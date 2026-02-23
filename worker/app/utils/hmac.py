import hmac
import hashlib
import json
from typing import Dict


def compute_hmac(payload: Dict, secret: str) -> str:
    """
    Compute HMAC-SHA256 signature for callback payload.
    Args:
        payload: Callback payload dictionary
        secret: Shared secret key
    Returns:
        64-character hex signature (lowercase)
    """
    # Serialize with NO whitespace - CRITICAL for signature matching
    json_str = json.dumps(payload, separators=(',', ':'), sort_keys=True)

    # Compute HMAC-SHA256
    signature = hmac.new(
        secret.encode('utf-8'),
        json_str.encode('utf-8'),
        hashlib.sha256
    ).hexdigest().lower()

    return signature


def verify_hmac(payload: Dict, signature: str, secret: str) -> bool:
    """
    Verify HMAC signature.
    Args:
        payload: Callback payload dictionary
        signature: Provided signature to verify
        secret: Shared secret key
    Returns:
        True if signature is valid
    """
    expected = compute_hmac(payload, secret)
    return hmac.compare_digest(expected, signature.lower())
