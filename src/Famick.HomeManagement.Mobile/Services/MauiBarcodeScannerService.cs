using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Mobile.Pages;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI implementation of barcode scanner using the device camera.
/// Uses ZXing.Net.MAUI for camera-based barcode detection.
/// </summary>
public class MauiBarcodeScannerService : IBarcodeScannerService
{
    /// <summary>
    /// Returns true on mobile platforms (camera is available).
    /// Note: Actual availability depends on camera permissions.
    /// </summary>
    public bool IsAvailable => true;

    /// <summary>
    /// Scans a barcode using the device camera.
    /// Opens a modal scanner page and returns when a barcode is detected or cancelled.
    /// </summary>
    public async Task<string?> ScanBarcodeAsync(CancellationToken ct = default)
    {
        // Check camera permission first
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                throw new InvalidOperationException("Camera permission denied");
            }
        }

        // Get the current page to navigate from
        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;

        // If wrapped in NavigationPage, get the navigation from it
        if (currentPage is NavigationPage navPage)
        {
            currentPage = navPage.CurrentPage ?? navPage;
        }

        if (currentPage == null)
        {
            throw new InvalidOperationException("Could not find current page for navigation");
        }

        // Create and show the scanner page
        var scannerPage = new BarcodeScannerPage();
        var scanTask = scannerPage.ScanAsync(ct);

        await currentPage.Navigation.PushModalAsync(scannerPage);

        // Wait for result
        var result = await scanTask;

        return result;
    }
}
