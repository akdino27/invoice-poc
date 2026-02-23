import re


def preprocess_ocr_text(text: str) -> str:
    """
    Clean and normalize OCR output.
    - Remove excessive whitespace
    - Fix common OCR errors
    - Preserve structure
    Args:
        text: Raw OCR text
    Returns:
        Cleaned text
    """
    if not text:
        return ""

    # Remove multiple spaces (keep single spaces)
    text = re.sub(r' +', ' ', text)

    # Remove multiple newlines (keep max 2)
    text = re.sub(r'\n{3,}', '\n\n', text)

    # Remove leading/trailing whitespace from each line
    lines = [line.strip() for line in text.split('\n')]
    text = '\n'.join(lines)

    # Common OCR corrections (context-aware)
    # These can be expanded based on observed errors

    return text.strip()


def truncate_text(text: str, max_length: int = 10000) -> str:
    """
    Truncate text to maximum length for LLM processing.
    Args:
        text: Input text
        max_length: Maximum characters
    Returns:
        Truncated text with indicator if truncated
    """
    if len(text) <= max_length:
        return text

    return text[:max_length] + "\n\n[Text truncated due to length...]"
