from pydantic_settings import BaseSettings
from pydantic import Field


class Config(BaseSettings):
    """Application configuration loaded from environment variables."""
    
    # Database Configuration
    db_host: str = Field(..., description="SQL Server host")
    db_port: int = Field(default=1433, description="SQL Server port")
    db_name: str = Field(..., description="Database name")
    db_user: str = Field(..., description="Database user")
    db_password: str = Field(..., description="Database password")
    
    # Backend Configuration
    backend_url: str = Field(..., description="ASP.NET backend base URL")
    callback_secret: str = Field(..., description="HMAC shared secret")
    
    # Google Drive Configuration
    google_service_account_key: str = Field(..., description="Path to service account JSON")
    
    # Groq LLM Configuration - UPDATED FOR LLAMA-4
    groq_api_key: str = Field(..., description="Groq API key")
    groq_model: str = Field(
        default="llama-4-70b-versatile",  # CHANGED TO LLAMA-4
        description="Groq model identifier"
    )
    
    # Worker Configuration
    worker_id: str = Field(default="worker-1", description="Unique worker identifier")
    poll_interval: int = Field(default=5, description="Job polling interval in seconds")
    max_retries: int = Field(default=3, description="Maximum retry attempts")
    
    @property
    def db_connection_string(self) -> str:
        """Generate SQL Server ODBC connection string."""
        return (
            f"DRIVER={{ODBC Driver 18 for SQL Server}};"
            f"SERVER={self.db_host},{self.db_port};"
            f"DATABASE={self.db_name};"
            f"UID={self.db_user};"
            f"PWD={self.db_password};"
            f"TrustServerCertificate=yes;"
        )
    
    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


def load_config() -> Config:
    """Load and validate configuration from environment."""
    return Config()
