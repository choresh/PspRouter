# 🚀 PSP Router Setup Guide

## Prerequisites

### 1. PostgreSQL with pgvector
```bash
# Install PostgreSQL (Ubuntu/Debian)
sudo apt update
sudo apt install postgresql postgresql-contrib

# Install pgvector extension
sudo apt install postgresql-16-pgvector  # Adjust version as needed

# Or using Docker
docker run --name psp-router-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=psp_router -p 5432:5432 -d pgvector/pgvector:pg16
```

### 2. .NET 8.0 SDK
```bash
# Install .NET 8.0 SDK
# Visit: https://dotnet.microsoft.com/download/dotnet/8.0
```

### 3. OpenAI API Key
- Get your API key from: https://platform.openai.com/api-keys
- Set the environment variable: `OPENAI_API_KEY=sk-your-key-here`

## Database Setup

### 1. Create Database
```sql
-- Connect to PostgreSQL as superuser
sudo -u postgres psql

-- Create database
CREATE DATABASE psp_router;

-- Create user (optional)
CREATE USER psp_router_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE psp_router TO psp_router_user;
```

### 2. Run Setup Script
```bash
# Run the setup script
psql -U postgres -d psp_router -f setup-database.sql
```

### 3. Verify Setup
```sql
-- Connect to the database
psql -U postgres -d psp_router

-- Check if vector extension is installed
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Check if tables are created
\dt

-- Check sample data
SELECT key, content FROM psp_lessons LIMIT 5;
```

## Environment Configuration

### 1. Set Environment Variables
```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY="sk-your-openai-key"
$env:PGVECTOR_CONNSTR="Host=localhost;Username=postgres;Password=postgres;Database=psp_router"

# Linux/Mac
export OPENAI_API_KEY="sk-your-openai-key"
export PGVECTOR_CONNSTR="Host=localhost;Username=postgres;Password=postgres;Database=psp_router"
```

### 2. Connection String Format
```
Host=localhost;Username=postgres;Password=postgres;Database=psp_router;Port=5432
```

## Running the Application

### 1. Build the Project
```bash
dotnet build
```

### 2. Run the Application
```bash
dotnet run
```

### 3. Expected Output
```
=== Configuration ===
✓ Database schema ensured
✓ Components initialized

--- Transaction 1 ---
Merchant: M123, Amount: 120.00 USD, Method: Card
Decision: Adyen
Reasoning: LLM routing - Auth: 89.00%, Fee: 200bps + $0.30
Method: LLM routing
Outcome: ✓ Authorized - Fee: $2.70 - Time: 450ms
✓ Lesson added to memory

Top memory results:
score=0.950 key=sample_1 meta_candidate=Adyen
USD Visa transactions work well with Adyen for low-risk merchants
---

=== PSP Router Ready ===
The enhanced PSP Router is now running with:
• LLM-based intelligent routing
• Multi-armed bandit learning
• Vector memory for lessons
• Comprehensive logging and monitoring
• Real PostgreSQL database integration
```

## Production Deployment

### 1. Environment Variables for Production
```bash
# Production OpenAI API key
export OPENAI_API_KEY="sk-prod-your-key"

# Production database connection
export PGVECTOR_CONNSTR="Host=prod-db-host;Username=psp_router;Password=secure-password;Database=psp_router;Port=5432;SSL Mode=Require"
```

### 2. Database Security
```sql
-- Create production user with limited privileges
CREATE USER psp_router_prod WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE psp_router TO psp_router_prod;
GRANT USAGE ON SCHEMA public TO psp_router_prod;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO psp_router_prod;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO psp_router_prod;
```

### 3. Monitoring Setup
```bash
# Install monitoring tools
# - Application Insights (Azure)
# - Prometheus + Grafana
# - ELK Stack for logging
```

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed
```
Error: 3D000: database "psp_router" does not exist
```
**Solution**: Create the database using the setup script

#### 2. Vector Extension Not Found
```
Error: extension "vector" does not exist
```
**Solution**: Install pgvector extension
```bash
sudo apt install postgresql-16-pgvector
```

#### 3. OpenAI API Key Invalid
```
Error: Invalid API key
```
**Solution**: Verify your API key and ensure it has sufficient credits

#### 4. Permission Denied
```
Error: permission denied for table psp_lessons
```
**Solution**: Grant proper permissions to your database user

### Debug Mode
```bash
# Enable debug logging
export DOTNET_ENVIRONMENT=Development
dotnet run
```

## Performance Tuning

### 1. Database Optimization
```sql
-- Optimize vector search performance
CREATE INDEX CONCURRENTLY psp_lessons_embedding_idx 
ON psp_lessons USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);

-- Analyze tables for better query planning
ANALYZE psp_lessons;
ANALYZE transaction_outcomes;
```

### 2. Connection Pooling
```csharp
// In your connection string, add pooling parameters
"Host=localhost;Username=postgres;Password=postgres;Database=psp_router;Pooling=true;MinPoolSize=5;MaxPoolSize=20"
```

### 3. Caching Strategy
- Cache PSP health status (30 seconds)
- Cache fee tables (5 minutes)
- Cache vector search results (1 minute)

## Security Considerations

### 1. API Key Security
- Never commit API keys to version control
- Use environment variables or secure key management
- Rotate keys regularly

### 2. Database Security
- Use strong passwords
- Enable SSL connections in production
- Restrict database access by IP
- Regular security updates

### 3. Network Security
- Use VPN or private networks for database access
- Implement rate limiting
- Monitor for suspicious activity

## Backup and Recovery

### 1. Database Backup
```bash
# Create backup
pg_dump -U postgres -h localhost psp_router > psp_router_backup.sql

# Restore backup
psql -U postgres -h localhost psp_router < psp_router_backup.sql
```

### 2. Automated Backups
```bash
# Add to crontab for daily backups
0 2 * * * pg_dump -U postgres psp_router | gzip > /backups/psp_router_$(date +\%Y\%m\%d).sql.gz
```

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review the logs for error details
3. Create an issue in the repository
4. Check the ENHANCED-README.md for detailed documentation

---

**Ready to route payments intelligently! 🎯**
