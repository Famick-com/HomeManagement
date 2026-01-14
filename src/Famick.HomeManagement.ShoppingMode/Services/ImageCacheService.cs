using Famick.HomeManagement.ShoppingMode.Models;

namespace Famick.HomeManagement.ShoppingMode.Services;

/// <summary>
/// Service for caching product images locally.
/// </summary>
public class ImageCacheService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public ImageCacheService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "images");

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <summary>
    /// Caches all product images for a shopping session.
    /// </summary>
    public async Task CacheImagesForSessionAsync(ShoppingSession session)
    {
        var tasks = new List<Task>();

        foreach (var item in session.Items)
        {
            if (!string.IsNullOrEmpty(item.LocalImagePath))
            {
                // Already cached
                continue;
            }

            // Get image URL from the API (would need to be added to item DTO)
            // For now, skip if no image URL available
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Downloads and caches an image, returning the local path.
    /// </summary>
    public async Task<string?> CacheImageAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        try
        {
            // Generate a unique filename based on URL hash
            var hash = imageUrl.GetHashCode().ToString("X");
            var extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";

            var localPath = Path.Combine(_cacheDirectory, $"{hash}{extension}");

            // Check if already cached
            if (File.Exists(localPath))
            {
                return localPath;
            }

            // Download and save
            var imageData = await _httpClient.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(localPath, imageData);

            return localPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the local path for a cached image if it exists.
    /// </summary>
    public string? GetCachedImagePath(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        try
        {
            var hash = imageUrl.GetHashCode().ToString("X");
            var extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";

            var localPath = Path.Combine(_cacheDirectory, $"{hash}{extension}");

            return File.Exists(localPath) ? localPath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, recursive: true);
                Directory.CreateDirectory(_cacheDirectory);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Gets the total size of cached images in bytes.
    /// </summary>
    public long GetCacheSizeBytes()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return 0;

            return new DirectoryInfo(_cacheDirectory)
                .GetFiles()
                .Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }
}
