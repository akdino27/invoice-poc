from googleapiclient.discovery import build
from googleapiclient.http import MediaIoBaseDownload
from google.oauth2 import service_account
import io
import logging

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
        
        Args:
            file_id: Google Drive file ID
            
        Returns:
            File contents as bytes
            
        Raises:
            Exception: If download fails
        """
        if not self.service:
            raise RuntimeError("Drive service not initialized")
        
        try:
            logger.info(f"Downloading file {file_id} from Google Drive")
            
            # Request file download
            request = self.service.files().get_media(fileId=file_id)
            
            # Download to buffer
            file_buffer = io.BytesIO()
            downloader = MediaIoBaseDownload(file_buffer, request)
            
            done = False
            while not done:
                status, done = downloader.next_chunk()
                if status:
                    progress = int(status.progress() * 100)
                    logger.debug(f"Download progress: {progress}%")
            
            # Get file data
            file_buffer.seek(0)
            file_data = file_buffer.read()
            
            logger.info(f"Downloaded {len(file_data)} bytes")
            
            return file_data
            
        except Exception as e:
            logger.error(f"Failed to download file {file_id}: {e}")
            raise Exception(f"Failed to download file {file_id}: {str(e)}")
