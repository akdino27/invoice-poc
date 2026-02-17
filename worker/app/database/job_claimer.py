import psycopg2
from psycopg2.extras import RealDictCursor
import json
import logging
from typing import Optional
from app.models.job import Job, JobPayload

logger = logging.getLogger(__name__)

class JobClaimer:
    """Handles atomic job claiming from PostgreSQL."""
    
    def __init__(self, connection_string: str):
        self.connection_string = connection_string
        self.connection = None
    
    def connect(self):
        """Establish database connection."""
        try:
            self.connection = psycopg2.connect(self.connection_string)
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
        """Atomically claim a pending job using PostgreSQL row locks."""
        if not self.connection:
            raise RuntimeError("Database not connected")
        
        cursor = self.connection.cursor(cursor_factory=RealDictCursor)
        
        try:
            cursor.execute("""
                SELECT 
                    "Id"::text, 
                    "JobType", 
                    "Status", 
                    "PayloadJson"::text, 
                    "RetryCount",
                    "CreatedAt",
                    "UpdatedAt"
                FROM "job_queues"
                WHERE "Status" = 'PENDING'
                  AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= NOW() AT TIME ZONE 'UTC')
                ORDER BY "CreatedAt" ASC
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            """)
            
            row = cursor.fetchone()
            if not row:
                return None
            
            job_id = row['Id']
            
            cursor.execute("""
                UPDATE "job_queues"
                SET "Status" = 'PROCESSING',
                    "LockedBy" = %s,
                    "LockedAt" = NOW() AT TIME ZONE 'UTC',
                    "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
                WHERE "Id" = %s::uuid
            """, (worker_id, job_id))
            
            self.connection.commit()
            logger.info(f"Claimed job {job_id}")
            
            payload_dict = json.loads(row['PayloadJson'])
            payload = JobPayload(**payload_dict)
            
            return Job(
                id=job_id,
                jobType=row['JobType'],
                status=row['Status'],
                payload=payload,
                retryCount=row['RetryCount'],
                createdAt=row['CreatedAt'],
                updatedAt=row['UpdatedAt']
            )
            
        except Exception as e:
            self.connection.rollback()
            logger.error(f"Error claiming job: {e}")
            raise
        finally:
            cursor.close()
    
    def release_job_lock(self, job_id: str) -> bool:
        """Release lock on a job."""
        if not self.connection:
            return False
        
        cursor = self.connection.cursor()
        try:
            cursor.execute("""
                UPDATE "job_queues"
                SET "LockedBy" = NULL, "LockedAt" = NULL
                WHERE "Id" = %s::uuid AND "Status" = 'PROCESSING'
            """, (job_id,))
            self.connection.commit()
            return cursor.rowcount > 0
        except Exception as e:
            self.connection.rollback()
            logger.error(f"Failed to release lock: {e}")
            return False
        finally:
            cursor.close()
    
    def release_all_locks(self, worker_id: str) -> int:
        """Release all locks held by worker."""
        if not self.connection:
            return 0
        
        cursor = self.connection.cursor()
        try:
            cursor.execute("""
                UPDATE "job_queues"
                SET "Status" = 'PENDING',
                    "LockedBy" = NULL,
                    "LockedAt" = NULL,
                    "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
                WHERE "LockedBy" = %s AND "Status" = 'PROCESSING'
            """, (worker_id,))
            self.connection.commit()
            return cursor.rowcount
        except Exception as e:
            self.connection.rollback()
            logger.error(f"Failed to release locks: {e}")
            return 0
        finally:
            cursor.close()
