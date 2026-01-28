# Invoice Processing System

AI-powered automated invoice processing system using a .NET backend and a Python AI worker for LLM-based extraction.

## Architecture

```
    ┌───────────────┐    ┌──────────────────┐    ┌───────────────┐
    │ Google Drive  │ ──▶│ .NET Backend     │◀───│ Python Worker │
    │ (Invoices)    │    │ (API + Monitor)  │    │ (AI Extractor)│
    └───────────────┘    └──────────────────┘    └───────────────┘
            │                     │                    │
            ▼                     ▼                    ▼
    ┌───────────────┐     ┌────────────────┐     ┌──────────────┐
    │ SQL Server    │     │ Groq LLM       │     │ Other Stores │
    │ (Database)    │     │ (Llama 3.3)    │     │ (e.g., Blob) │
    └───────────────┘     └────────────────┘     └──────────────┘
```

## Features

* Automatic invoice detection — Monitors a Google Drive folder for new invoices and creates processing jobs.
* AI-powered extraction — Uses Groq (Llama 3.3) for intelligent field extraction from PDFs (OCR + LLM).
* Structured data storage — Persists invoices, line items, and product metadata to SQL Server.
* Secure callbacks — HMAC-SHA256 signed communication between worker and backend for job callbacks.
* Docker support — Full containerization with `docker-compose`.
* Automatic retry — Failed jobs are retried according to configurable policies.
* Product analytics — Tracks sales, revenue, and trends per product.

---

## Quick Start (Docker)

### Prerequisites

* Docker Desktop installed and running
* Git installed
* Google Drive service account JSON key
* Groq API key

### Step 1 — Clone repository

```bash
git clone https://github.com/akdino27/invoice-poc.git
cd invoice-poc
```

### Step 2 — Setup secrets

Create `.env` from the template:

```bash
cp .env.template .env
```

Edit `.env` and set required values, for example:

```
DB_PASSWORD=YourStrong@Passw0rd
GOOGLE_FOLDER_ID=your-folder-id-from-google-drive
CALLBACK_SECRET=your-base64-secret
GROQ_API_KEY=gsk_your-groq-api-key
```

Add Google service account key to the `secrets` folder (this folder should be git-ignored):

```bash
# Copy your service account key to the secrets folder
cp /path/to/your/service-account-key.json secrets/service-account-key.json
```

### Step 3 — Run with Docker Compose

```bash
# Build and start all services (foreground)
docker-compose up --build

# Or run in detached mode
docker-compose up -d --build
```

### Step 4 — Verify services

* Backend Swagger UI: `http://localhost:5247/swagger`
* SQL Server: `localhost:1433` (SA / password from `.env`)

Check logs:

```bash
docker logs invoice-backend -f
docker logs invoice-worker -f
```

### Step 5 — Test the system

* Upload an invoice PDF to your configured Google Drive folder.
* Watch worker logs for processing (`docker logs invoice-worker -f`).
* Check the backend Swagger UI and query the database to confirm extracted data.

### Step 6 — Stop services

```bash
# Stop containers
docker-compose down

# Stop and remove volumes (deletes database)
docker-compose down -v
```

---

## Development Setup (Without Docker)

### Backend (.NET 8)

```bash
cd backend

# Restore NuGet packages
dotnet restore

# Create appsettings.json from template
cp appsettings.template.json appsettings.json

# Edit appsettings.json with your configuration

# Run database migrations
dotnet ef database update

# Run backend
dotnet run
```

Backend will start at `http://localhost:5247`.

### Worker (Python 3.11)

```bash
cd worker

# Create virtual environment
python -m venv venv

# Activate virtual environment
# Windows:
venv\\Scripts\\activate
# Linux/Mac:
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Create .env from template
cp .env.template .env

# Edit .env with your configuration

# Run worker
python -m app.main
```

---

## Project Structure

```
invoice-poc/
├── backend/                 # .NET 8 Backend API
│   ├── src/
│   │   ├── Api/             # Controllers, Middleware
│   │   ├── Application/     # Services, DTOs, Background Services
│   │   ├── Domain/          # Entities, Enums
│   │   └── Infrastructure/  # Data, Repositories
│   ├── Migrations/          # EF Core migrations
│   ├── Properties/
│   ├── Dockerfile
│   ├── appsettings.template.json
│   ├── invoice-v1.csproj
│   └── Program.cs
│
├── worker/                  # Python 3.11 AI Worker
│   ├── app/
│   │   ├── database/        # Job claimer (SQL Server)
│   │   ├── extractors/      # OCR, PDF, LLM extraction
│   │   ├── services/        # Callback, Drive, MIME detection
│   │   ├── models/          # Pydantic models
│   │   └── utils/           # HMAC, text cleaning
│   ├── tests/
│   ├── Dockerfile
│   ├── requirements.txt
│   └── .env.template
│
├── secrets/                 # Git-ignored secrets folder
│   ├── .gitignore
│   ├── service-account-key.json  (ignored)
│   └── service-account-key.json.template
│
├── .github/
│   └── workflows/
│       └── ci.yml           # CI/CD pipeline
│
├── docker-compose.yml       # Docker orchestration
├── .env                     # Environment variables (ignored)
├── .env.template            # Template for .env
├── .gitignore               # Git ignore rules
├── invoice-v1.slnx          # Visual Studio solution
├── README.md                # This file
└── SETUP.md                 # Team setup instructions
```

---

## Security

* Never commit `.env` files or `service-account-key.json`.
* Store secrets only in `.env` and `secrets/` (git-ignored).
* Use HMAC-SHA256 authentication for worker-to-backend callbacks.
* Use environment variables for configuration in production.
* Rotate secrets regularly and enforce least-privilege for service accounts.

---

## Testing

### Backend tests

```bash
cd backend
dotnet test
```

### Worker tests

```bash
cd worker
pytest tests/ -v
```

---

## Database Migrations

```bash
cd backend

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to previous migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove
```

---

## Troubleshooting

### Worker can't connect to database

**Symptom:** `pyodbc.OperationalError: Unable to connect`

**Actions:**

* Wait ~30 seconds after `docker-compose up` for SQL Server to initialize.
* Check SQL Server health: `docker logs invoice-sqlserver`.
* Verify `DB_PASSWORD` in `.env` matches `docker-compose.yml` configuration.

### Backend can't read service account key

**Symptom:** `FileNotFoundException: service-account-key.json`

**Actions:**

```bash
# Verify file exists inside repo container mount
ls secrets/service-account-key.json

# Check docker-compose volume mount
docker-compose config
```

### LLM extraction fails (rate limit)

**Symptom:** `groq.RateLimitError: Rate limit exceeded`

**Actions:**

* Verify Groq API quota at the Groq console.
* Verify `GROQ_API_KEY` in `.env` is valid.
* Implement exponential backoff and retry in the worker.

### HMAC validation failed

**Symptom:** Backend rejects worker callback with HMAC validation error.

**Actions:**

* Ensure `CALLBACK_SECRET` is identical in:

  * Backend `appsettings.json` under `Security:CallbackSecret`
  * Worker `.env` as `CALLBACK_SECRET`
* Both must be the same base64 string.

---

## Environment Variables

### Backend (`appsettings.json`)

| Variable                              |                                 Description | Example                                                                  |
| ------------------------------------- | ------------------------------------------: | ------------------------------------------------------------------------ |
| `ConnectionStrings:DefaultConnection` |                SQL Server connection string | `Server=sqlserver;Database=InvoiceProcessingV2;User Id=sa;Password=...;` |
| `GoogleDrive:ServiceAccountKeyPath`   | Path to Google credentials inside container | `/app/secrets/service-account-key.json`                                  |
| `GoogleDrive:SharedFolderId`          |           Google Drive folder ID to monitor | `1-PenAeLnXGUiZeNrmxJo...`                                               |
| `Security:CallbackSecret`             |               HMAC callback secret (base64) | `YN0Dr3K5DrXzy06IpCQP...`                                                |
| `JobCreation:IntervalSeconds`         |              Job polling interval (seconds) | `30`                                                                     |

### Worker (`.env`)

| Variable                     |                      Description | Example                                 |
| ---------------------------- | -------------------------------: | --------------------------------------- |
| `DB_HOST`                    |              SQL Server hostname | `sqlserver` or `localhost\\SQLEXPRESS`  |
| `DB_NAME`                    |                    Database name | `InvoiceProcessingV2`                   |
| `DB_PASSWORD`                |                      SA password | `YourStrong@Passw0rd`                   |
| `BACKEND_URL`                |                  Backend API URL | `http://backend:80`                     |
| `CALLBACK_SECRET`            | HMAC secret (must match backend) | `YN0Dr3K5DrXzy06IpCQP...`               |
| `GROQ_API_KEY`               |                 Groq LLM API key | `gsk_...`                               |
| `GOOGLE_SERVICE_ACCOUNT_KEY` |       Path to Google credentials | `/app/secrets/service-account-key.json` |
| `WORKER_ID`                  |         Unique worker identifier | `worker-1`                              |
| `POLL_INTERVAL`              |   Job polling interval (seconds) | `5`                                     |

---
