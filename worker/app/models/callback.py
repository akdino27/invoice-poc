from pydantic import BaseModel, Field
from datetime import datetime, timezone
from app.models.invoice import InvoiceData


class CallbackPayload(BaseModel):
    """Base callback payload."""
    jobId: str = Field(..., description="Job GUID")
    status: str = Field(..., description="COMPLETED, INVALID, or FAILED")
    workerId: str = Field(..., description="Worker that processed this job")
    processedAt: str = Field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat(),
        description="ISO timestamp"
    )


class CompletedCallback(CallbackPayload):
    """Callback for successfully processed invoice."""
    status: str = Field(default="COMPLETED", const=True)
    result: InvoiceData = Field(..., description="Extracted invoice data")


class InvalidCallback(CallbackPayload):
    """Callback for invalid/unprocessable file."""
    status: str = Field(default="INVALID", const=True)
    reason: str = Field(..., description="Why file cannot be processed")


class FailedCallback(CallbackPayload):
    """Callback for transient processing failure."""
    status: str = Field(default="FAILED", const=True)
    reason: str = Field(..., description="Error message")
