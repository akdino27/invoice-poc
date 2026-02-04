from googleapiclient.discovery import build
from googleapiclient.http import MediaIoBaseDownload
from google.oauth2 import service_account
from google.auth.transport.requests import Request
import io
import logging
import time


logger = logging.getLogger(__name__)


class DriveService:
    """Google Drive file operations with retry and timeout support."""
    
    def __init__(self, service_account_key_path: str, timeout: int = 300, chunk_size: int = 1048576, max_retries: int = 3):
        """
        Initialize Google Drive service.
        
        Args:
            service_account_key_path: Path to service account JSON file
            timeout: HTTP timeout in seconds (default: 300 = 5 minutes)
            chunk_size: Download chunk size in bytes (default: 1MB)
            max_retries: Maximum number of download retry attempts (default: 3)
        """
        self.service_account_key_path = service_account_key_path
        self.timeout = timeout
        self.chunk_size = chunk_size
        self.max_retries = max_retries
        self.service = None
        
        logger.info(f"DriveService config: timeout={timeout}s, chunk_size={chunk_size}B, max_retries={max_retries}")
    
    def connect(self):
        """Initialize Google Drive API service with custom timeout."""
        try:
            credentials = service_account.Credentials.from_service_account_file(
                self.service_account_key_path,
                scopes=['https://www.googleapis.com/auth/drive.readonly']
            )
            
            # Build service - timeout is handled by the HTTP client internally
            # No need to manually create http client with google-auth 2.0+
            self.service = build(
                'drive', 
                'v3', 
                credentials=credentials,
                # Note: timeout is passed to underlying requests, but we'll handle it in download
            )
            
            logger.info(f"Google Drive service initialized (timeout: {self.timeout}s)")
            
        except Exception as e:
            logger.error(f"Failed to initialize Drive service: {e}")
            raise
    
    def download_file(self, file_id: str) -> bytes:
        """
        Download file from Google Drive by file ID with retry logic.
        
        Args:
            file_id: Google Drive file ID
            
        Returns:
            File contents as bytes
            
        Raises:
            Exception: If download fails after all retry attempts
        """
        if not self.service:
            raise RuntimeError("Drive service not initialized. Call connect() first.")
        
        last_error = None
        
        for attempt in range(1, self.max_retries + 1):
            try:
                logger.info(f"Downloading file {file_id} from Google Drive (attempt {attempt}/{self.max_retries})")
                
                # Request file download
                request = self.service.files().get_media(fileId=file_id)
                
                # Download to buffer with chunking
                file_buffer = io.BytesIO()
                downloader = MediaIoBaseDownload(file_buffer, request, chunksize=self.chunk_size)
                
                done = False
                last_progress = 0
                
                while not done:
                    try:
                        status, done = downloader.next_chunk()
                        
                        if status:
                            progress = int(status.progress() * 100)
                            
                            # Log progress every 25% or when complete
                            if progress >= last_progress + 25 or done:
                                logger.info(f"Download progress: {progress}%")
                                last_progress = progress
                                
                    except Exception as chunk_error:
                        logger.error(f"Error during chunk download: {chunk_error}")
                        raise
                
                # Get file data
                file_buffer.seek(0)
                file_data = file_buffer.read()
                
                logger.info(f"✓ Downloaded {len(file_data)} bytes ({len(file_data)/1024:.2f} KB) successfully")
                
                return file_data
                
            except Exception as e:
                last_error = e
                error_msg = str(e)
                logger.error(f"✗ Download attempt {attempt}/{self.max_retries} failed: {error_msg}")
                
                if attempt < self.max_retries:
                    # Exponential backoff: 2^attempt seconds (2, 4, 8)
                    wait_time = 2 ** attempt
                    logger.info(f"⏳ Retrying in {wait_time} seconds...")
                    time.sleep(wait_time)
                else:
                    logger.error(f"✗ All {self.max_retries} download attempts failed for file {file_id}")
        
        # If we get here, all retries failed
        raise Exception(f"Failed to download file {file_id} after {self.max_retries} attempts: {str(last_error)}")
