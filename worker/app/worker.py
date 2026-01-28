import asyncio
import signal
import logging
import time
from datetime import datetime, timezone
from typing import Optional

from app.config import Config
from app.database.job_claimer import JobClaimer
from app.services.drive_service import DriveService
from app.services.mime_detector import (
    detect_mime_type, 
    validate_mime_type, 
    get_pipeline_for_mime,
    ProcessingPipeline
)
from app.services.callback_service import CallbackService
from app.extractors.image_extractor import extract_text_from_image
from app.extractors.pdf_extractor import extract_text_from_pdf
from app.extractors.llm_extractor import LLMExtractor
from app.utils.text_cleaner import preprocess_ocr_text
from app.utils.validator import validate_invoice_data
from app.models.invoice import InvoiceData

logger = logging.getLogger(__name__)


class InvoiceWorker:
    """Main worker that polls for jobs and processes invoices."""
    
    def __init__(self, config: Config):
        self.config = config
        self.running = False
        
        # Worker configuration
        self.worker_id = config.worker_id
        self.poll_interval = config.poll_interval
        self.is_running = False
        self.start_time = None
        self.logger = logger
        
        # Initialize and connect services
        self.logger.info("Initializing worker services...")
        
        # Database connection
        self.job_claimer = JobClaimer(config.db_connection_string)
        self.job_claimer.connect()
        self.logger.info("✓ Database connected")
        
        # Google Drive connection
        self.drive_service = DriveService(config.google_service_account_key)
        self.drive_service.connect()
        self.logger.info("✓ Google Drive connected")
        
        # LLM extractor (no connection needed)
        self.llm_extractor = LLMExtractor(config.groq_api_key, config.groq_model)
        self.logger.info("✓ LLM extractor initialized")
        
        # Callback service (no connection needed - uses httpx per request)
        self.callback_service = CallbackService(config.backend_url, config.callback_secret)
        self.logger.info("✓ Callback service initialized")
        
        # Statistics
        self.stats = {
            "jobs_processed": 0,
            "jobs_failed": 0,
            "jobs_invalid": 0,
            "start_time": datetime.now(timezone.utc)
        }
        
        self.logger.info("Worker initialization complete")
    
    async def start(self):
        """Start the worker polling loop."""
        self.logger.info(f"Worker {self.worker_id} starting...")
        
        # Register signal handlers only if in main thread
        try:
            signal.signal(signal.SIGINT, self._signal_handler)
            signal.signal(signal.SIGTERM, self._signal_handler)
            self.logger.debug("Signal handlers registered")
        except ValueError:
            # Signal handlers only work in main thread - that's okay
            self.logger.debug("Running in background thread - signal handlers skipped")
        
        self.is_running = True
        self.start_time = time.time()
        
        self.logger.info(
            f"Worker {self.worker_id} polling every {self.poll_interval} seconds"
        )
        
        while self.is_running:
            try:
                await self._poll_and_process()
                await asyncio.sleep(self.poll_interval)
            except asyncio.CancelledError:
                self.logger.info("Worker task cancelled")
                break
            except Exception as e:
                self.logger.error(f"Unexpected error in worker loop: {e}", exc_info=True)
                await asyncio.sleep(self.poll_interval)
        
        # Graceful shutdown - release all locks
        await self._shutdown_cleanup()
        
        self.logger.info(f"Worker {self.worker_id} stopped")
    
    async def _poll_and_process(self):
        """Poll for a job and process it."""
        # Claim a job from database
        job = self.job_claimer.claim_job(self.worker_id)
        
        if job is None:
            self.logger.debug("No pending jobs available")
            return
        
        job_id = job.id
        self.logger.info(f"[{job_id}] Claimed job for processing")
        
        # Process the job
        callback_data = await self._process_job(job)
        
        # ✅ Release lock BEFORE sending callback to avoid deadlock
        try:
            self.job_claimer.release_job_lock(job_id)
            self.logger.debug(f"[{job_id}] Released job lock before callback")
        except Exception as e:
            self.logger.warning(f"[{job_id}] Failed to release lock: {e}")
        
        # Send callback to backend
        try:
            success = await self.callback_service.send_callback(callback_data)
            
            if success:
                if callback_data["status"] == "COMPLETED":
                    self.stats["jobs_processed"] += 1
                    self.logger.info(f"[{job_id}] Job completed successfully")
                elif callback_data["status"] == "INVALID":
                    self.stats["jobs_invalid"] += 1
                    self.logger.warning(f"[{job_id}] Job marked as INVALID")
                elif callback_data["status"] == "FAILED":
                    self.stats["jobs_failed"] += 1
                    self.logger.error(f"[{job_id}] Job failed")
            else:
                self.logger.error(f"[{job_id}] Failed to send callback to backend")
        
        except Exception as e:
            self.logger.error(f"[{job_id}] Error sending callback: {e}", exc_info=True)
    
    async def _process_job(self, job) -> dict:
        """Complete job processing pipeline."""
        job_id = job.id
        payload = job.payload
        file_id = payload.fileId
        expected_mime = payload.mimeType
        
        try:
            # Step 1: Download file from Google Drive
            logger.info(f"[{job_id}] Downloading file {file_id}")
            file_data = self.drive_service.download_file(file_id)
            
            # Step 2: Detect MIME type
            detected_mime = detect_mime_type(file_data)
            logger.info(f"[{job_id}] MIME: detected={detected_mime}, expected={expected_mime}")
            
            # Step 3: Validate MIME type
            if not validate_mime_type(detected_mime, expected_mime):
                return self._create_invalid_callback(
                    job_id,
                    f"MIME type mismatch: expected {expected_mime}, got {detected_mime}"
                )
            
            # Step 4: Route to appropriate pipeline
            pipeline = get_pipeline_for_mime(detected_mime)
            
            if pipeline == ProcessingPipeline.UNSUPPORTED:
                return self._create_invalid_callback(
                    job_id,
                    f"Unsupported MIME type: {detected_mime}"
                )
            
            # Step 5: Extract text
            logger.info(f"[{job_id}] Extracting text using {pipeline.value} pipeline")
            
            if pipeline == ProcessingPipeline.IMAGE:
                raw_text = extract_text_from_image(file_data)
                raw_text = preprocess_ocr_text(raw_text)
            elif pipeline == ProcessingPipeline.PDF:
                raw_text = extract_text_from_pdf(file_data)
            
            # Step 6: Validate extracted text
            if not raw_text or len(raw_text) < 50:
                return self._create_invalid_callback(
                    job_id,
                    f"Insufficient text extracted ({len(raw_text)} chars)"
                )
            
            logger.info(f"[{job_id}] Extracted {len(raw_text)} characters")
            
            # Step 7: Extract invoice data using LLM
            logger.info(f"[{job_id}] Sending to Groq LLM")
            invoice_data = self.llm_extractor.extract_invoice(raw_text)
            
            logger.info(f"[{job_id}] Successfully extracted invoice {invoice_data.InvoiceNumber}")
            
            # Step 8: Validate invoice data
            is_valid, error_msg = validate_invoice_data(invoice_data)
            if not is_valid:
                logger.error(f"[{job_id}] Validation failed: {error_msg}")
                return self._create_failed_callback(job_id, f"Validation failed: {error_msg}")
            
            logger.info(f"[{job_id}] All validations passed")
            
            # Step 9: Create success callback
            return self._create_completed_callback(job_id, invoice_data)
            
        except Exception as e:
            logger.error(f"[{job_id}] Processing failed: {e}", exc_info=True)
            return self._create_failed_callback(job_id, str(e))
    
    def _create_completed_callback(self, job_id: str, result: InvoiceData) -> dict:
        """Create COMPLETED status callback."""
        return {
            "jobId": job_id,
            "status": "COMPLETED",
            "result": result.model_dump(),
            "workerId": self.config.worker_id,
            "processedAt": datetime.now(timezone.utc).isoformat()
        }
    
    def _create_invalid_callback(self, job_id: str, reason: str) -> dict:
        """Create INVALID status callback."""
        return {
            "jobId": job_id,
            "status": "INVALID",
            "reason": reason,
            "workerId": self.config.worker_id,
            "processedAt": datetime.now(timezone.utc).isoformat()
        }
    
    def _create_failed_callback(self, job_id: str, reason: str) -> dict:
        """Create FAILED status callback."""
        return {
            "jobId": job_id,
            "status": "FAILED",
            "reason": reason,
            "workerId": self.config.worker_id,
            "processedAt": datetime.now(timezone.utc).isoformat()
        }
    
    def _signal_handler(self, signum, frame):
        """Handle shutdown signals gracefully."""
        logger.info(f"Received signal {signum}, shutting down gracefully...")
        self.is_running = False
    
    async def _shutdown_cleanup(self):
        """Release all locks held by this worker on shutdown."""
        logger.info("Performing shutdown cleanup...")
        
        try:
            released = self.job_claimer.release_all_locks(self.worker_id)
            if released > 0:
                logger.info(f"Released {released} job locks held by worker {self.worker_id}")
            else:
                logger.info("No job locks to release")
        except Exception as e:
            logger.error(f"Error during shutdown cleanup: {e}")
        
        # Close database connection
        try:
            self.job_claimer.disconnect()
            logger.info("Database connection closed")
        except Exception as e:
            logger.error(f"Error closing database connection: {e}")
    
    def get_stats(self) -> dict:
        """Get worker statistics."""
        uptime = time.time() - self.start_time if self.start_time else 0
        total_jobs = self.stats["jobs_processed"] + self.stats["jobs_failed"] + self.stats["jobs_invalid"]
        
        return {
            "worker_id": self.worker_id,
            "is_running": self.is_running,
            "uptime_seconds": round(uptime, 2),
            "jobs_processed": self.stats["jobs_processed"],
            "jobs_failed": self.stats["jobs_failed"],
            "jobs_invalid": self.stats["jobs_invalid"],
            "total_jobs": total_jobs,
            "success_rate": round(self.stats["jobs_processed"] / total_jobs * 100, 2) if total_jobs > 0 else 0
        }
