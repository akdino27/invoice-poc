import json
import logging
from groq import Groq
from app.models.invoice import InvoiceData

logger = logging.getLogger(__name__)

class LLMExtractor:
    """Groq Llama-3 based invoice data extractor."""

    def __init__(self, api_key: str, model: str = "llama-3.3-70b-versatile"):
        self.client = Groq(api_key=api_key)
        self.model = model
        logger.info(f"Initialized LLM extractor with model: {model}")

        self.system_prompt = """You are an expert invoice data extraction system.

Extract structured invoice data from the provided text and return ONLY valid JSON.

CRITICAL FIELDS (must extract accurately):
1. InvoiceNumber: The invoice/receipt number (REQUIRED)
2. VendorName: The SELLER/COMPANY issuing the invoice (e.g., SuperStore, Amazon, Walmart) - REQUIRED
3. BillTo.Name: The CUSTOMER/BUYER name (who is being billed)
4. TotalAmount: The final total (REQUIRED)

Required JSON structure:
{
  "InvoiceNumber": "string (REQUIRED)",
  "InvoiceDate": "string (any format, REQUIRED)",
  "OrderId": "string or null",
  "VendorName": "string (REQUIRED - company/seller name)",
  "BillTo": {
    "Name": "string (REQUIRED - customer name)"
  },
  "ShipTo": {
    "City": "string or null",
    "State": "string or null",
    "Country": "string or null"
  },
  "ShipMode": "string or null",
  "LineItems": [
    {
      "ProductName": "string (REQUIRED)",
      "Category": "string or null",
      "ProductId": "string (REQUIRED)",
      "Quantity": number (REQUIRED),
      "UnitRate": number (REQUIRED),
      "Amount": number (REQUIRED)"
    }
  ],
  "Subtotal": number or null,
  "Discount": {
    "Percentage": number or null,
    "Amount": number or null
  } or null,
  "ShippingCost": number or null,
  "TotalAmount": number (REQUIRED),
  "BalanceDue": number or null,
  "Currency": "string (default: USD)",
  "Notes": "string or null",
  "Terms": "string or null"
}

Rules:
1. Return ONLY the JSON object, no markdown code blocks, no explanations
2. VendorName is the SELLER (company issuing invoice), NOT the customer
3. BillTo.Name is the CUSTOMER (who is being billed)
4. All monetary values must be numbers (not strings)
5. All quantities must be numbers
6. If a field is not found, use null
7. InvoiceNumber, VendorName, and TotalAmount are REQUIRED
8. LineItems array must have at least one item
9. Each LineItem must have ProductName, ProductId, Quantity, UnitRate, and Amount
10. Currency defaults to "USD" if not specified"""

    def extract_invoice(self, raw_text: str) -> InvoiceData:
        """
        Extract structured invoice data from raw text using Groq Llama.
        Args:
            raw_text: Extracted text from OCR or PDF
        Returns:
            Validated InvoiceData object
        Raises:
            Exception: If LLM fails or returns invalid data
        """
        user_prompt = f"""Extract invoice data from this text:

{raw_text}

Return only valid JSON matching the required structure.
IMPORTANT: VendorName is the SELLER/COMPANY issuing the invoice (like "SuperStore", "Amazon", etc.)"""

        try:
            logger.info(f"Calling Groq Llama API with {len(raw_text)} characters")

            # Call Groq API
            chat_completion = self.client.chat.completions.create(
                messages=[
                    {"role": "system", "content": self.system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                model=self.model,
                temperature=0.1,  # Low temperature for consistent extraction
                max_tokens=4096,
                response_format={"type": "json_object"}  # Force JSON response
            )

            # Extract response
            response_text = chat_completion.choices[0].message.content
            logger.debug(f"Llama response: {len(response_text)} characters")

            # Parse JSON
            invoice_dict = json.loads(response_text)

            # Validate with Pydantic
            invoice_data = InvoiceData(**invoice_dict)

            logger.info(f"Successfully extracted invoice {invoice_data.InvoiceNumber}")

            return invoice_data

        except json.JSONDecodeError as e:
            logger.error(f"Llama returned invalid JSON: {e}")
            raise Exception(f"LLM returned invalid JSON: {str(e)}")
        except Exception as e:
            logger.error(f"Llama extraction failed: {e}", exc_info=True)
            raise Exception(f"LLM extraction failed: {str(e)}")
