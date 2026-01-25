using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Models;

/// <summary>
/// Request model for uploading a photo to a storage bin.
/// </summary>
public class UploadStorageBinPhotoRequest
{
    /// <summary>
    /// The photo file to upload
    /// </summary>
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}
