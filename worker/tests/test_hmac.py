"""Test HMAC computation consistency."""
import sys
sys.path.insert(0, '../')

from app.utils.hmac import compute_hmac, verify_hmac


def test_hmac_consistency():
    """Test that HMAC computation is consistent."""
    payload = {"jobId": "test-123", "status": "COMPLETED"}
    secret = "test-secret"
    
    hmac1 = compute_hmac(payload, secret)
    hmac2 = compute_hmac(payload, secret)
    
    assert hmac1 == hmac2, "HMAC should be deterministic"
    assert len(hmac1) == 64, "SHA256 should produce 64 hex chars"
    assert verify_hmac(payload, hmac1, secret), "HMAC verification should succeed"
    
    print(f"✓ HMAC: {hmac1}")
    print("✓ All HMAC tests passed")


if __name__ == "__main__":
    test_hmac_consistency()
