import pytesseract
from PIL import Image
import io
import logging
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'
logger = logging.getLogger(__name__)


def extract_text_from_image(image_data: bytes) -> str:
    """
    Extract text from image using Tesseract OCR.
    Args:
        image_data: Raw image bytes (JPEG, PNG)
    Returns:
        Extracted text string
    Raises:
        Exception: If OCR fails
    """
    try:
        logger.debug(f"Opening image ({len(image_data)} bytes)")
        
        # Open image from bytes
        image = Image.open(io.BytesIO(image_data))
        
        # Convert to RGB if necessary (some formats need this)
        if image.mode not in ('RGB', 'L'):
            logger.debug(f"Converting image from {image.mode} to RGB")
            image = image.convert('RGB')
        
        logger.debug(f"Image size: {image.size}, mode: {image.mode}")
        
        # Perform OCR with English language
        text = pytesseract.image_to_string(image, lang='eng')
        
        logger.debug(f"OCR extracted {len(text)} characters")
        
        return text.strip()
        
    except Exception as e:
        logger.error(f"OCR failed: {e}", exc_info=True)
        raise Exception(f"OCR failed: {str(e)}")
