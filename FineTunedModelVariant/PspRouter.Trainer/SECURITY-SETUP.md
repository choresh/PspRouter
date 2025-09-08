# Database Security Setup Guide

## 🔐 Secure Ways to Handle Database Credentials

### **Option 1: .env File (Easiest for Development)**

Create a `.env` file in the project root:
```bash
# Copy the example file
cp env.example .env

# Edit .env with your actual values
PSPROUTER_DB_CONNECTION=Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=true;
```

**Important:** Add `.env` to your `.gitignore` file to avoid committing credentials!

### **Option 2: Environment Variables (Recommended for Production)**

#### Windows (PowerShell):
```powershell
# Set environment variable
$env:PSPROUTER_DB_CONNECTION = "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;"

# Or set permanently
[Environment]::SetEnvironmentVariable("PSPROUTER_DB_CONNECTION", "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;", "User")
```

#### Windows (Command Prompt):
```cmd
set PSPROUTER_DB_CONNECTION=Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;
```

#### Linux/Mac:
```bash
export PSPROUTER_DB_CONNECTION="Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
```

### **Option 2: User Secrets (Development Only)**

```bash
# Initialize user secrets
dotnet user-secrets init

# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
```

### **Option 3: Azure Key Vault (Enterprise)**

Add to `Program.cs`:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

### **Option 4: Docker Environment Variables**

```dockerfile
# In Dockerfile
ENV PSPROUTER_DB_CONNECTION="Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
```

Or in `docker-compose.yml`:
```yaml
services:
  psprouter-trainer:
    environment:
      - PSPROUTER_DB_CONNECTION=Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;
```

## 🚀 **Quick Start (Recommended)**

### **For Development (.env file):**
```bash
# Copy example file
cp env.example .env

# Edit .env with your connection string
# PSPROUTER_DB_CONNECTION=Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;

# Run the application
dotnet run
```

### **For Development (Environment Variable):**
```bash
# Set environment variable
$env:PSPROUTER_DB_CONNECTION = "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;"

# Run the application
dotnet run
```

### **For Production:**
Set the environment variable on your production server and run the application.

## 🔒 **Security Best Practices**

1. **Never commit credentials** to source control
2. **Use environment variables** for production
3. **Use user secrets** for development
4. **Rotate credentials** regularly
5. **Use least privilege** database accounts
6. **Enable SSL/TLS** for database connections
7. **Use Azure Key Vault** for enterprise environments

## 📝 **Connection String Examples**

### **SQL Server with Windows Authentication:**
```
Server=your-server;Database=your-db;Trusted_Connection=true;TrustServerCertificate=true;
```

### **SQL Server with SQL Authentication:**
```
Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;
```

### **Azure SQL Database:**
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-db;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
