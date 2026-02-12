from pydantic import BaseModel, Field
from typing import Optional
from datetime import datetime
from enum import Enum


class JobStatus(str, Enum):
    """Job processing status."""
    PENDING = "PENDING"
    PROCESSING = "PROCESSING"
    COMPLETED = "COMPLETED"
    FAILED = "FAILED"
    INVALID = "INVALID"


class JobPayload(BaseModel):
    """Structure of JobQueue PayloadJson field."""
    fileId: str = Field(..., description="Google Drive file ID")
    originalName: str = Field(..., description="Original filename")
    mimeType: str = Field(..., description="Expected MIME type")
    fileSize: int = Field(..., description="File size in bytes")
    uploader: Optional[str] = Field(None, description="User who uploaded file")
    schemaVersion: str = Field(default="1.0")
    idempotencyKey: str = Field(..., description="Unique key for deduplication")
    detectedAt: str = Field(..., description="ISO timestamp of detection")


class Job(BaseModel):
    """Complete job record from database."""
    id: str = Field(..., description="Job GUID")
    jobType: str = Field(..., description="Always INVOICE_EXTRACTION")
    status: JobStatus
    payload: JobPayload
    retryCount: int = Field(default=0)
    lockedBy: Optional[str] = None
    lockedAt: Optional[datetime] = None
    nextRetryAt: Optional[datetime] = None
    errorMessage: Optional[str] = None
    createdAt: datetime
    updatedAt: datetime
