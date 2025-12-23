# Famick Home Management (Self-Hosted)

🏠 **Open-source household management system** for inventory tracking, recipe management, chores, and task organization.

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL%203.0-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)](https://www.postgresql.org/)

## ✨ Features

- 📦 **Stock Management** - Track inventory with barcode scanning
- 🛒 **Shopping Lists** - Plan purchases and share lists
- 🍳 **Recipes** - Store recipes with meal planning
- 🧹 **Chores** - Schedule and track household tasks
- ✅ **Tasks** - Personal task management
- 👥 **Multi-User** - Support for household members with permissions

## 🚀 Quick Start with Docker

The easiest way to get started is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/famick/homemanagement.git
cd homemanagement

# Create environment file
cp .env.example .env
# Edit .env and set secure passwords

# Start the application
docker compose -f docker/docker-compose.yml up -d

# Access at http://localhost:5000
```

### First-Time Setup

After starting the containers:

1. Navigate to `http://localhost:5000`
2. Register the first admin user
3. Configure your household settings

## 📋 Requirements

### Docker Deployment (Recommended)
- Docker 20.10+
- Docker Compose 2.0+
- 2GB RAM minimum
- 10GB disk space

### Manual Deployment
- .NET 8.0 SDK
- PostgreSQL 14+
- 4GB RAM recommended

## 🔧 Configuration

Key environment variables in `.env`:

```bash
# Database
DB_PASSWORD=your_secure_password_here

# JWT Authentication  
JWT_SECRET_KEY=your-secret-key-min-32-characters

# Application
ASPNETCORE_ENVIRONMENT=Production
```

See [Configuration Guide](docs/configuration.md) for all options.

## 📚 Documentation

- [Installation Guide](docs/installation.md)
- [Configuration](docs/configuration.md)
- [Backup & Restore](docs/backup-restore.md)
- [Upgrading](docs/upgrading.md)
- [API Documentation](docs/api.md)
- [Troubleshooting](docs/troubleshooting.md)

## 🔒 Security

- **Authentication**: JWT-based with secure password hashing
- **HTTPS**: Configured by default (self-signed cert in dev)
- **Updates**: Regular security patches - watch releases

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

### Development Setup

```bash
# Clone and restore
git clone https://github.com/famick/homemanagement.git
cd homemanagement
dotnet restore

# Run with hot reload
dotnet watch run --project src/Famick.HomeManagement.Web

# Run tests
dotnet test
```

## 📄 License

This project is licensed under **AGPL-3.0** - see [LICENSE](LICENSE) for details.

This means:
- ✅ Free to use, modify, and distribute
- ✅ Commercial use allowed
- ⚠️ Must disclose source code of modifications
- ⚠️ Network use triggers distribution requirements

## 🙏 Credits

Inspired by [Grocy](https://github.com/grocy/grocy) - the original PHP household management system.

## 💬 Support

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/famick/homemanagement/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/famick/homemanagement/discussions)  
- 📧 **Email**: support@famick.com

## ☁️ Cloud Version

Looking for a managed SaaS solution? Check out [Famick Cloud](https://famick.com) with:
- Multi-tenant architecture
- Automatic backups
- Mobile apps
- Premium support
- Store integrations
