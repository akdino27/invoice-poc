from googleapiclient.discovery import build
from googleapiclient.http import MediaIoBaseDownload
from google.oauth2 import service_account
from googleapiclient.errors import HttpError
import io
import logging
import time
import socket

logger = logging.getLogger(__name__)

class DriveService:
    """Google Drive file operations."""
    
    def __init__(self, service_account_key_path: str):
        self.service_account_key_path = service_account_key_path
        self.service = None
    
    def connect(self):
        """Initialize Google Drive API service."""
        try:
            credentials = service_account.Credentials.from_service_account_file(
                self.service_account_key_path,
                scopes=['https://www.googleapis.com/auth/drive.readonly']
            )
            
            self.service = build('drive', 'v3', credentials=credentials)
            logger.info("Google Drive service initialized")
            
        except Exception as e:
            logger.error(f"Failed to initialize Drive service: {e}")
            raise
    
    def download_file(self, file_id: str) -> bytes:
        """
        Download file from Google Drive by file ID.
        Includes retry logic for transient network errors (WinError 10053).
        """
        if not self.service:
            # Auto-connect if not connected
            try:
                self.connect()
            except Exception:
                raise RuntimeError("Drive service not initialized and failed to connect")
        
        # RETRY LOGIC for Transient Errors
        max_retries = 3
        
        for attempt in range(max_retries):
            try:
                logger.info(f"Downloading file {file_id} (Attempt {attempt + 1}/{max_retries})")
                
                # Request file download
                request = self.service.files().get_media(fileId=file_id)
                
                # Download to buffer
                file_buffer = io.BytesIO()
                downloader = MediaIoBaseDownload(file_buffer, request)
                
                done = False
                while not done:
                    try:
                        status, done = downloader.next_chunk()
                        if status:
                            progress = int(status.progress() * 100)
                            # logger.debug(f"Download progress: {progress}%")
                    except (socket.error, ConnectionError, OSError) as chunk_error:
                        # Catch WinError 10053 specifically during chunk download
                        logger.warning(f"Connection broken during chunk download: {chunk_error}")
                        raise # Re-raise to trigger the outer loop retry
            
                # If we get here, download completed successfully
                file_buffer.seek(0)
                file_data = file_buffer.read()
                
                logger.info(f"Downloaded {len(file_data)} bytes successfully")
                return file_data
            
            except (HttpError, socket.error, ConnectionError, OSError) as e:
                logger.warning(f"Download failed for {file_id} (Attempt {attempt + 1}): {e}")
                
                if attempt < max_retries - 1:
                    # Exponential backoff for internal retries: 2s, 4s
                    sleep_time = 2 * (attempt + 1)
                    time.sleep(sleep_time)
                else:
                    # Final attempt failed, raise exception to fail the job
                    logger.error(f"Failed to download file {file_id} after {max_retries} attempts.")
                    raise Exception(f"Failed to download file {file_id}: {str(e)}")
            
            except Exception as e:
                # Non-transient errors (like 404 Not Found or Auth errors) should fail immediately
                logger.error(f"Non-retriable error for {file_id}: {e}")
                raise Exception(f"Failed to download file {file_id}: {str(e)}")
