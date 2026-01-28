"""Test all connectivity before starting worker."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from app.config import load_config
import pyodbc


def test_database():
    """Test SQL Server connection."""
    print("\n" + "="*60)
    print("TEST 1: SQL Server Database Connection")
    print("="*60)
    
    try:
        config = load_config()
        print(f"Connecting to: {config.db_host}/{config.db_name}")
        
        conn = pyodbc.connect(config.db_connection_string, timeout=10)
        cursor = conn.cursor()
        
        cursor.execute("SELECT @@VERSION")
        version = cursor.fetchone()[0]
        print(f"✅ Database connected!")
        print(f"   SQL Server: {version.split(chr(10))[0][:60]}...")
        
        cursor.execute("SELECT COUNT(*) FROM JobQueues")
        count = cursor.fetchone()[0]
        print(f"✅ JobQueues table accessible")
        print(f"   Current jobs: {count}")
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Database connection failed!")
        print(f"   Error: {e}")
        return False


def test_backend():
    """Test backend API connection."""
    print("\n" + "="*60)
    print("TEST 2: C# Backend API Connection")
    print("="*60)
    
    try:
        import httpx
        config = load_config()
        
        print(f"Connecting to: {config.backend_url}/health")
        
        response = httpx.get(f"{config.backend_url}/health", timeout=5)
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Backend connected!")
            print(f"   Status: {data.get('Status')}")
            print(f"   Version: {data.get('Version')}")
            return True
        else:
            print(f"❌ Backend returned HTTP {response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ Backend connection failed: {e}")
        return False


def test_groq():
    """Test Groq API key."""
    print("\n" + "="*60)
    print("TEST 3: Groq LLM API Connection")
    print("="*60)
    
    try:
        from groq import Groq
        config = load_config()
        
        print(f"Testing API key: {config.groq_api_key[:20]}...")
        
        client = Groq(api_key=config.groq_api_key)
        
        response = client.chat.completions.create(
            messages=[{"role": "user", "content": "Reply with: OK"}],
            model=config.groq_model,
            max_tokens=10,
            temperature=0
        )
        
        result = response.choices[0].message.content
        print(f"✅ Groq API connected!")
        print(f"   Response: {result}")
        return True
        
    except Exception as e:
        print(f"❌ Groq API failed: {e}")
        return False


def test_google_drive():
    """Test Google Drive service account."""
    print("\n" + "="*60)
    print("TEST 4: Google Drive Service Account")
    print("="*60)
    
    try:
        from google.oauth2 import service_account
        from googleapiclient.discovery import build
        config = load_config()
        
        print(f"Loading: {config.google_service_account_key}")
        
        credentials = service_account.Credentials.from_service_account_file(
            config.google_service_account_key,
            scopes=['https://www.googleapis.com/auth/drive.readonly']
        )
        
        service = build('drive', 'v3', credentials=credentials)
        results = service.files().list(pageSize=1, fields="files(id, name)").execute()
        
        print(f"✅ Google Drive connected!")
        print(f"   Service account: {credentials.service_account_email}")
        return True
        
    except Exception as e:
        print(f"❌ Google Drive test failed: {e}")
        return False


if __name__ == "__main__":
    print("\n" + "█"*60)
    print("  INVOICE WORKER - CONNECTIVITY TESTS")
    print("█"*60)
    
    results = {
        "Database": test_database(),
        "Backend": test_backend(),
        "Groq": test_groq(),
        "Google Drive": test_google_drive()
    }
    
    print("\n" + "="*60)
    print("TEST SUMMARY")
    print("="*60)
    
    for test, passed in results.items():
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"{test:20s} {status}")
    
    print("="*60)
    
    if all(results.values()):
        print("✅ ALL TESTS PASSED - Ready to start worker!")
    else:
        print("❌ SOME TESTS FAILED - Fix issues before starting worker")
    
    print("="*60 + "\n")
