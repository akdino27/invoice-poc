import pdfplumber
import io
import logging

logger = logging.getLogger(__name__)


def extract_text_from_pdf(pdf_data: bytes) -> str:
    """
    Extract text from PDF using pdfplumber.
    Preserves layout and structure better than PyPDF2.
    Args:
        pdf_data: Raw PDF bytes
    Returns:
        Extracted text string
    Raises:
        Exception: If PDF extraction fails
    """
    try:
        logger.debug(f"Opening PDF ({len(pdf_data)} bytes)")
        
        text_parts = []
        
        with pdfplumber.open(io.BytesIO(pdf_data)) as pdf:
            logger.debug(f"PDF has {len(pdf.pages)} pages")
            
            for page_num, page in enumerate(pdf.pages, 1):
                # Extract text with layout preservation
                page_text = page.extract_text()
                
                if page_text:
                    text_parts.append(page_text)
                    logger.debug(f"Page {page_num}: {len(page_text)} characters")
                else:
                    logger.warning(f"Page {page_num}: No text extracted")
        
        full_text = "\n\n".join(text_parts)
        logger.debug(f"Total extracted: {len(full_text)} characters")
        
        return full_text
        
    except Exception as e:
        logger.error(f"PDF extraction failed: {e}", exc_info=True)
        raise Exception(f"PDF extraction failed: {str(e)}")


def extract_text_from_pdf_pymupdf(pdf_data: bytes) -> str:
    """
    Alternative: Extract text using PyMuPDF (faster for large PDFs).
    Args:
        pdf_data: Raw PDF bytes
    Returns:
        Extracted text string
    """
    import fitz  # PyMuPDF
    
    try:
        doc = fitz.open(stream=pdf_data, filetype="pdf")
        text_parts = []
        
        logger.debug(f"PDF has {len(doc)} pages (PyMuPDF)")
        
        for page_num, page in enumerate(doc, 1):
            page_text = page.get_text()
            if page_text:
                text_parts.append(page_text)
                logger.debug(f"Page {page_num}: {len(page_text)} characters")
        
        doc.close()
        
        full_text = "\n\n".join(text_parts)
        return full_text
        
    except Exception as e:
        logger.error(f"PDF extraction (PyMuPDF) failed: {e}")
        raise Exception(f"PDF extraction failed: {str(e)}")
