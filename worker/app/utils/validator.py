from typing import Tuple
from app.models.invoice import InvoiceData


def validate_invoice_data(invoice: InvoiceData) -> Tuple[bool, str]:
    """
    Validate extracted invoice data for presence of primary/important fields.
    Only checks that required fields exist and have valid values.
    Does NOT perform mathematical validation (amount calculations, subtotal checks, etc.).

    Args:
        invoice: InvoiceData object to validate
    Returns:
        Tuple of (is_valid, error_message)
    """

    # 1. Check required header fields
    if not invoice.InvoiceNumber:
        return False, "Missing InvoiceNumber"

    if not invoice.InvoiceDate:
        return False, "Missing InvoiceDate"

    if not invoice.VendorName:
        return False, "Missing VendorName"

    # 2. Check TotalAmount is present and positive
    if invoice.TotalAmount is None:
        return False, "Missing TotalAmount"

    if invoice.TotalAmount <= 0:
        return False, f"Invalid TotalAmount: {invoice.TotalAmount}"

    # 3. Check LineItems exist
    if not invoice.LineItems or len(invoice.LineItems) == 0:
        return False, "No line items found"

    # 4. Check each line item has required fields
    for idx, item in enumerate(invoice.LineItems):
        if not item.ProductName:
            return False, f"LineItem[{idx}] missing ProductName"

        if not item.ProductId:
            return False, f"LineItem[{idx}] missing ProductId"

        if item.Quantity is None or item.Quantity <= 0:
            return False, f"LineItem[{idx}] invalid Quantity: {item.Quantity}"

        if item.UnitRate is None or item.UnitRate <= 0:
            return False, f"LineItem[{idx}] invalid UnitRate: {item.UnitRate}"

        if item.Amount is None or item.Amount <= 0:
            return False, f"LineItem[{idx}] invalid Amount: {item.Amount}"

    # All field-presence validations passed
    return True, ""
