import httpx
import json
import logging
import hmac
import hashlib
from typing import Dict

logger = logging.getLogger(__name__)


class CallbackService:
    """Handles HMAC-signed callbacks to ASP.NET backend."""
    
    def __init__(self, backend_url: str, callback_secret: str):
        self.backend_url = backend_url
        self.callback_secret = callback_secret
        self.logger = logger
    
    def _generate_hmac(self, body: bytes) -> str:
        """
        Generate HMAC-SHA256 signature for request body.
        
        Args:
            body: Request body as bytes
            
        Returns:
            Hex-encoded HMAC signature (lowercase)
        """
        signature = hmac.new(
            self.callback_secret.encode('utf-8'),
            body,
            hashlib.sha256
        ).hexdigest()
        
        return signature.lower()
    
    async def send_callback(self, callback_data: dict) -> bool:
        """
        Send callback to backend API with HMAC authentication.
        
        Args:
            callback_data: Dictionary containing job result data
            
        Returns:
            True if callback was accepted (HTTP 200)
            
        Raises:
            Exception: If callback fails or times out
        """
        url = f"{self.backend_url}/api/ai/callback"
        
        # Serialize callback data
        body = json.dumps(callback_data).encode('utf-8')
        
        # Generate HMAC signature
        hmac_signature = self._generate_hmac(body)
        
        headers = {
            "Content-Type": "application/json",
            "X-Callback-HMAC": hmac_signature
        }
        
        self.logger.info(f"Sending callback for job {callback_data['jobId']} to {url}")
        
        try:
            #Increase timeout to 180 seconds (3 minutes)
            async with httpx.AsyncClient(timeout=180.0) as client:
                response = await client.post(url, headers=headers, content=body)
                
                if response.status_code == 200:
                    self.logger.info(f"Callback accepted for job {callback_data['jobId']}")
                    return True
                else:
                    self.logger.error(f"Callback failed: HTTP {response.status_code}")
                    self.logger.error(f"Response: {response.text}")
                    raise Exception(f"Backend returned {response.status_code}")
        
        except httpx.TimeoutException:
            self.logger.error("Callback request timed out")
            raise Exception("Callback request timed out after 180s")
        except httpx.ReadTimeout:
            self.logger.error("Callback request timed out")
            raise Exception("Callback request timed out after 180s")
        except Exception as e:
            self.logger.error(f"Callback request failed: {e}")
            raise
