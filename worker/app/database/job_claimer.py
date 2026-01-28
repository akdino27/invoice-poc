import pyodbc
import json
import logging
from typing import Optional
from app.models.job import Job, JobPayload

logger = logging.getLogger(__name__)

class JobClaimer:
    """Handles atomic job claiming from SQL Server."""
    
    def __init__(self, connection_string: str):
        self.connection_string = connection_string
        self.connection = None
    
    def connect(self):
        """Establish database connection."""
        try:
            self.connection = pyodbc.connect(self.connection_string)
            logger.info("Database connection established")
        except Exception as e:
            logger.error(f"Failed to connect to database: {e}")
            raise
    
    def disconnect(self):
        """Close database connection."""
        if self.connection:
            self.connection.close()
            logger.info("Database connection closed")
    
    def claim_job(self, worker_id: str) -> Optional[Job]:
        """
        Atomically claim a pending job using SQL Server row locks.
        Args:
            worker_id: Unique identifier for this worker
        Returns:
            Job object if claimed, None if no jobs available
        """
        if not self.connection:
            raise RuntimeError("Database not connected")
        
        cursor = self.connection.cursor()
        
        try:
            # Start transaction
            cursor.execute("BEGIN TRANSACTION")
            
            # Find and lock a pending job
            # UPDLOCK: Exclusive lock for update
            # READPAST: Skip rows locked by other workers
            cursor.execute("""
                SELECT TOP 1 
                    Id, 
                    JobType, 
                    Status, 
                    PayloadJson, 
                    RetryCount,
                    CreatedAt,
                    UpdatedAt
                FROM JobQueues WITH (UPDLOCK, READPAST)
                WHERE Status = 'PENDING'
                  AND (NextRetryAt IS NULL OR NextRetryAt <= GETUTCDATE())
                ORDER BY CreatedAt ASC
            """)
            
            row = cursor.fetchone()
            
            if not row:
                cursor.execute("ROLLBACK")
                return None
            
            # Extract row data
            job_id = str(row[0])
            job_type = row[1]
            status = row[2]
            payload_json = row[3]
            retry_count = row[4]
            created_at = row[5]
            updated_at = row[6]
            
            # Immediately claim the job
            cursor.execute("""
                UPDATE JobQueues
                SET Status = 'PROCESSING',
                    LockedBy = ?,
                    LockedAt = GETUTCDATE(),
                    UpdatedAt = GETUTCDATE()
                WHERE Id = ?
            """, worker_id, job_id)
            
            # Commit transaction
            cursor.execute("COMMIT")
            
            logger.info(f"Claimed job {job_id}")
            
            # Parse payload
            try:
                payload_dict = json.loads(payload_json)
                payload = JobPayload(**payload_dict)
            except Exception as e:
                logger.error(f"Failed to parse job payload: {e}")
                raise
            
            # Build Job object
            return Job(
                id=job_id,
                jobType=job_type,
                status=status,
                payload=payload,
                retryCount=retry_count,
                createdAt=created_at,
                updatedAt=updated_at
            )
            
        except Exception as e:
            cursor.execute("ROLLBACK")
            logger.error(f"Error claiming job: {e}")
            raise
        finally:
            cursor.close()
    
    def release_job_lock(self, job_id: str) -> bool:
        """
        Release the lock on a job before sending callback.
        This prevents database deadlock when backend tries to update the same job.
        
        Args:
            job_id: Job ID to release lock for
            
        Returns:
            True if lock released, False otherwise
        """
        if not self.connection:
            logger.warning("Database not connected - cannot release lock")
            return False
        
        cursor = self.connection.cursor()
        
        try:
            cursor.execute("""
                UPDATE JobQueues
                SET LockedBy = NULL,
                    LockedAt = NULL
                WHERE Id = ?
                AND Status = 'PROCESSING'
            """, job_id)
            
            self.connection.commit()
            
            if cursor.rowcount > 0:
                logger.debug(f"Released lock for job {job_id}")
                return True
            else:
                logger.warning(f"Job {job_id} not found or not PROCESSING")
                return False
                
        except Exception as e:
            logger.error(f"Failed to release lock for job {job_id}: {e}")
            self.connection.rollback()
            return False
        finally:
            cursor.close()
    
    def release_all_locks(self, worker_id: str) -> int:
        """
        Release all locks held by this worker.
        Called during graceful shutdown to reset PROCESSING jobs back to PENDING.
        
        Args:
            worker_id: Worker ID to release locks for
            
        Returns:
            Number of locks released
        """
        if not self.connection:
            logger.warning("Database not connected - cannot release locks")
            return 0
        
        cursor = self.connection.cursor()
        
        try:
            cursor.execute("""
                UPDATE JobQueues
                SET Status = 'PENDING',
                    LockedBy = NULL,
                    LockedAt = NULL,
                    UpdatedAt = GETUTCDATE()
                WHERE LockedBy = ?
                AND Status = 'PROCESSING'
            """, worker_id)
            
            self.connection.commit()
            
            released_count = cursor.rowcount
            
            if released_count > 0:
                logger.info(f"Released {released_count} job locks for worker {worker_id}")
            
            return released_count
            
        except Exception as e:
            logger.error(f"Failed to release locks for worker {worker_id}: {e}")
            self.connection.rollback()
            return 0
        finally:
            cursor.close()
