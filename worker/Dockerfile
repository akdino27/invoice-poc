FROM python:3.11-slim

# Install system dependencies including libmagic for MIME detection
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-eng \
    curl \
    wget \
    gnupg \
    unixodbc \
    unixodbc-dev \
    ca-certificates \
    gcc \
    g++ \
    make \
    libmagic1 \
    file \
    && rm -rf /var/lib/apt/lists/*

# Download and install Microsoft ODBC Driver 18 directly
RUN wget -nv https://packages.microsoft.com/debian/12/prod/pool/main/m/msodbcsql18/msodbcsql18_18.3.3.1-1_amd64.deb -O /tmp/msodbcsql18.deb \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y /tmp/msodbcsql18.deb \
    && rm -f /tmp/msodbcsql18.deb \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy requirements
COPY requirements.txt .

# Upgrade pip and install requirements
RUN pip install --no-cache-dir --upgrade pip setuptools wheel \
    && pip install --no-cache-dir -r requirements.txt

# Copy application
COPY app/ ./app/

EXPOSE 8000

CMD ["python", "-m", "app.main"]
