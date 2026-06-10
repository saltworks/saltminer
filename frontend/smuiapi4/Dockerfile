FROM python:3.12-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt gunicorn

# Copy application code
COPY app/ app/
COPY run.py .

# Create directories for file storage (will be overridden by volume mounts)
RUN mkdir -p /opt/saltworks/saltminer/report-templates \
             /opt/saltworks/saltminer/custom-jobs \
             /opt/saltworks/saltminer/core

EXPOSE 5001

# Run with gunicorn for production (4 workers)
CMD ["gunicorn", "--bind", "0.0.0.0:5001", "--workers", "4", "run:app"]
