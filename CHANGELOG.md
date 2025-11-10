# Changelog

## v1.0 - STABLE (November 10, 2024)

### 🎉 Production Release

This release marks the first stable production version of the Aristocratic Artwork Sale application.

### ✅ Fixed Issues
- **Admin Edit Form**: Fixed Status, Currency, and Price fields not saving correctly
  - Added explicit `name` attributes to all form fields for proper model binding
  - Status dropdown now correctly shows current value (Available/Sold)
  - Currency dropdown now correctly shows current value (EUR/USD)
  - Price field no longer clears when not modified during edit
  - Category dropdown maintains correct selection

### 🔧 Technical Improvements
- Implemented debug logging in CollectionsController for troubleshooting
- Cleaned up repository structure, removed old scripts and documentation
- Consolidated documentation into single comprehensive README.md
- Retained only production Docker configuration (docker-compose.prod.yml)

### 📦 Repository Structure
Clean, production-ready structure with:
- Single README.md with complete deployment guide
- Production Docker Compose configuration only
- No legacy scripts or duplicate files
- Clear separation of concerns

### 🚀 Deployment
- Fully containerized with Docker
- PostgreSQL database
- Nginx reverse proxy with SSL support
- Automated image processing
- Multi-language support (10 languages)

### 📝 Documentation
- Comprehensive README.md with deployment instructions
- Troubleshooting guide
- Security best practices
- Backup and maintenance procedures

---

## Pre-releases

All previous development work has been consolidated into this stable v1.0 release.
