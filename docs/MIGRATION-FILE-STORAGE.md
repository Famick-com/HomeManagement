# File Storage Migration Guide

This guide describes how to migrate uploaded files after updating to a version with secure file storage.

## Background

Previously, uploaded files (product images and equipment documents) were stored in `wwwroot/uploads/` and served as static files. This allowed anyone with the URL to access files without authentication.

The new version stores files outside the web-accessible directory (`/app/uploads/` in Docker, or `uploads/` in the application root locally) and serves them through authenticated API endpoints. This ensures:

- All file access requires authentication
- Tenant isolation is enforced (users can only access their tenant's files)
- Direct URL access to files is blocked

## Migration Steps

### For Docker Deployments (Bind Mounts)

If you're using bind mounts (recommended), migration is simple - just update the container path:

**Before:**
```yaml
volumes:
  - ./uploads:/app/wwwroot/uploads
```

**After:**
```yaml
volumes:
  - ./uploads:/app/uploads
```

The host directory (`./uploads`) stays the same. No files need to be moved on the host.

1. **Stop the application**
   ```bash
   docker compose down
   ```

2. **Update docker-compose.yml** - change the volume mount path as shown above

3. **Start the application**
   ```bash
   docker compose up -d
   ```

4. **Verify file access**
   - Log into the application
   - Navigate to a product with images and verify they display
   - Navigate to equipment with documents and verify they download

### For Local Development

If running locally (not in Docker):

1. **Move files**
   ```bash
   # From the homemanagement/src/Famick.HomeManagement.Web directory
   mkdir -p uploads/products uploads/equipment

   # Move product images
   mv wwwroot/uploads/products/* uploads/products/ 2>/dev/null || true

   # Move equipment documents
   mv wwwroot/uploads/equipment/* uploads/equipment/ 2>/dev/null || true
   ```

2. **Clean up old directories**
   ```bash
   rm -rf wwwroot/uploads/products wwwroot/uploads/equipment
   ```

## Verification

After migration, verify that:

1. **Files are accessible when authenticated**
   - Product images display correctly on product detail pages
   - Equipment documents can be downloaded

2. **Files are NOT accessible without authentication**
   - Open a browser incognito window
   - Try to access a file URL directly (e.g., `/api/v1/products/{productId}/images/{imageId}/download`)
   - You should receive a 401 Unauthorized response

3. **Old static file URLs no longer work**
   - Try accessing the old URL pattern: `/uploads/products/{productId}/{filename}`
   - You should receive a 404 Not Found response

## Troubleshooting

### Files not displaying after migration

1. Check that files exist:
   ```bash
   docker compose exec web ls -la /app/uploads/products/
   docker compose exec web ls -la /app/uploads/equipment/
   ```

2. Check file permissions:
   ```bash
   docker compose exec web ls -la /app/uploads/
   ```

3. Check application logs for errors:
   ```bash
   docker compose logs web | grep -i "file\|image\|document"
   ```

## New File URL Format

The new secure API endpoints are:

- **Product images**: `GET /api/v1/products/{productId}/images/{imageId}/download`
- **Equipment documents**: `GET /api/v1/equipment/documents/{documentId}/download`

These endpoints:
- Require authentication (JWT token in Authorization header)
- Return the file with proper Content-Type and Content-Disposition headers
- Validate that the file belongs to the user's tenant
