# Session Checkpoint - 2025-12-28

## Completed Features

### Product Management (Complete)
- Product CRUD operations with barcode scanning
- Product lookup via plugin pipeline (USDA, OpenFoodFacts)
- Product images with thumbnail support
- Nutrition facts panel
- Serving size information
- Mobile-responsive UI for products list and detail pages

### Mobile App Deployment (Complete)
- iOS deployment with proper code signing
- Android deployment with emulator support
- Localization working on both platforms (MauiLocalizationService)
- HTTP connections allowed for local development
- Dynamic base URL switching for self-hosted servers

### Authentication Enhancements
- Remember Me functionality implemented
  - Unchecked: 7-day refresh token expiration
  - Checked: 30-day refresh token expiration (configurable)

## Key Decisions

### Architecture
1. **Plugin Pipeline Pattern**: Plugins execute sequentially, each can add new results or enrich existing ones with images/data
2. **MAUI Localization**: Use `FileSystem.OpenAppPackageFileAsync` instead of HttpClient for reading locale files on mobile
3. **Dynamic URL Handling**: `DynamicApiHttpHandler` rewrites all request URLs using current `ApiSettings.BaseUrl`

### Mobile Configuration
1. **iOS**: `NSAllowsArbitraryLoads=true` in Info.plist for development HTTP
2. **Android**: `network_security_config.xml` with `cleartextTrafficPermitted=true`
3. **Android Build**: `EmbedAssembliesIntoApk=true` required for emulator/manual APK installs
4. **Java SDK**: OpenJDK 17 required, path set in csproj for macOS

### Configuration Settings
- `JwtSettings:RefreshTokenExpirationDays`: 7 (default session)
- `JwtSettings:RefreshTokenExtendedExpirationDays`: 30 (Remember Me)

## Repository State

### homemanagement (main)
- Latest commit: `b8fe569` - chore(config): add RefreshTokenExtendedExpirationDays setting
- Pushed to origin

### homemanagement-shared (main)
- Latest commit: `1836658` - fix(auth): implement Remember Me functionality for login
- Pushed to origin

## Next Steps (Potential)
- Stock management features
- Recipe management
- Chore tracking
- Cloud deployment configuration
- Production security hardening (disable HTTP allowances)
