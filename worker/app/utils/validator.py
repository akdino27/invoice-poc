from typing import Tuple
from app.models.invoice import InvoiceData

def validate_invoice_data(invoice: InvoiceData) -> Tuple[bool, str]:
    """
    Validate extracted invoice data with RELAXED tolerance for rounding errors.
    Args:
        invoice: InvoiceData object to validate
    Returns:
        Tuple of (is_valid, error_message)
    """
    
    # 1. Check required fields
    if not invoice.InvoiceNumber:
        return False, "Missing InvoiceNumber"
    
    if not invoice.InvoiceDate:
        return False, "Missing InvoiceDate"
    
    if not invoice.LineItems or len(invoice.LineItems) == 0:
        return False, "No line items found"
    
    # 2. Check TotalAmount is present and positive
    if invoice.TotalAmount is None:
        return False, "Missing TotalAmount"
    
    if invoice.TotalAmount <= 0:
        return False, f"Invalid TotalAmount: {invoice.TotalAmount}"
    
    # 3. Validate each line item with RELAXED tolerance (1% or $1)
    for idx, item in enumerate(invoice.LineItems):
        # Check required fields
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
        
        # Calculate expected amount
        expected_amount = item.Quantity * item.UnitRate
        
        # RELAXED tolerance: 1% of amount OR $1, whichever is larger
        tolerance = max(abs(item.Amount * 0.01), 1.0)
        difference = abs(item.Amount - expected_amount)
        
        if difference > tolerance:
            # Beyond tolerance - reject
            return False, (
                f"LineItem[{idx}] amount mismatch beyond tolerance: "
                f"{item.Amount} != {expected_amount:.2f} "
                f"(difference: {difference:.2f}, tolerance: {tolerance:.2f})"
            )
    
    # 4. Validate subtotal vs line items with RELAXED tolerance
    calculated_subtotal = sum(item.Amount for item in invoice.LineItems)
    
    if invoice.Subtotal is not None:
        # RELAXED tolerance: 1% of subtotal OR $1, whichever is larger
        tolerance = max(abs(invoice.Subtotal * 0.01), 1.0)
        difference = abs(calculated_subtotal - invoice.Subtotal)
        
        if difference > tolerance:
            return False, (
                f"Subtotal mismatch beyond tolerance: "
                f"calculated={calculated_subtotal:.2f}, "
                f"expected={invoice.Subtotal:.2f}, "
                f"difference={difference:.2f}, "
                f"tolerance={tolerance:.2f}"
            )
    
    # 5. Validate TotalAmount calculation with RELAXED tolerance
    # TotalAmount = Subtotal - Discount.Amount + ShippingCost
    expected_total = calculated_subtotal
    
    # Handle Discount (nested object with Amount field)
    if invoice.Discount is not None and invoice.Discount.Amount is not None:
        expected_total -= invoice.Discount.Amount
    
    # Handle ShippingCost
    if invoice.ShippingCost is not None:
        expected_total += invoice.ShippingCost
    
    # RELAXED tolerance: 1% of total OR $1, whichever is larger
    tolerance = max(abs(invoice.TotalAmount * 0.01), 1.0)
    difference = abs(invoice.TotalAmount - expected_total)
    
    if difference > tolerance:
        return False, (
            f"TotalAmount mismatch beyond tolerance: "
            f"calculated={expected_total:.2f}, "
            f"expected={invoice.TotalAmount:.2f}, "
            f"difference={difference:.2f}, "
            f"tolerance={tolerance:.2f}"
        )
    
    # All validations passed
    return True, ""


def validate_line_item_amount(quantity: float, unit_rate: float, amount: float) -> Tuple[bool, str]:
    """
    Validate that line item amount matches quantity * unit_rate with tolerance.
    Args:
        quantity: Item quantity
        unit_rate: Unit price
        amount: Total amount
    Returns:
        Tuple of (is_valid, error_message)
    """
    expected = quantity * unit_rate
    
    # RELAXED tolerance: 1% of amount OR $1, whichever is larger
    tolerance = max(abs(amount * 0.01), 1.0)
    difference = abs(amount - expected)
    
    if difference > tolerance:
        return False, (
            f"Amount mismatch: {amount} != {expected:.2f} "
            f"(difference: {difference:.2f}, tolerance: {tolerance:.2f})"
        )
    
    return True, ""
