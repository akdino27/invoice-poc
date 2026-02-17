from pydantic import BaseModel, Field
from typing import List, Optional


class BillTo(BaseModel):
    """Billing address information."""
    Name: str = Field(..., description="Customer name")


class ShipTo(BaseModel):
    """Shipping address information."""
    City: Optional[str] = None
    State: Optional[str] = None
    Country: Optional[str] = None


class LineItem(BaseModel):
    """Individual invoice line item."""
    ProductName: str = Field(..., description="Product name or description")
    Category: Optional[str] = Field(None, description="Product category")
    ProductId: str = Field(..., description="Product ID or SKU")
    Quantity: float = Field(..., gt=0, description="Quantity ordered")
    UnitRate: float = Field(..., gt=0, description="Price per unit")
    Amount: float = Field(..., description="Total line amount")


class DiscountInfo(BaseModel):  # ← RENAMED from "Discount" to "DiscountInfo"
    """Discount information."""
    Percentage: Optional[float] = Field(None, ge=0, le=100)
    Amount: Optional[float] = Field(None, ge=0)


class InvoiceData(BaseModel):
    """Complete invoice data structure returned by LLM."""
    InvoiceNumber: str = Field(..., description="Invoice number (required)")
    InvoiceDate: str = Field(..., description="Invoice date in any format")
    OrderId: Optional[str] = None
    VendorName: Optional[str] = None
    BillTo: BillTo
    ShipTo: ShipTo
    ShipMode: Optional[str] = None
    LineItems: List[LineItem] = Field(..., min_length=1)
    Subtotal: Optional[float] = Field(None, ge=0)
    Discount: Optional[DiscountInfo] = None  # ← Uses DiscountInfo class
    ShippingCost: Optional[float] = Field(None, ge=0)
    TotalAmount: float = Field(..., description="Total invoice amount (required)")
    BalanceDue: Optional[float] = Field(None, ge=0)
    Currency: str = Field(default="USD", description="Currency code")
    Notes: Optional[str] = None
    Terms: Optional[str] = None
