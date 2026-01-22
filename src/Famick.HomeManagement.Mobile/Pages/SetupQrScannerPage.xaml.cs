using Famick.HomeManagement.Mobile.Services;
using ZXing.Net.Maui;

namespace Famick.HomeManagement.Mobile.Pages;

/// <summary>
/// Page for scanning QR codes containing server setup deep links.
/// Scans for URLs like: famick://setup?url=https://home.example.com&name=Home%20Server
/// </summary>
public partial class SetupQrScannerPage : ContentPage
{
    private readonly DeepLinkHandler _deepLinkHandler;
    private TaskCompletionSource<SetupQrResult?>? _scanCompletionSource;
    private bool _isProcessing;

    public BarcodeReaderOptions BarcodeOptions { get; } = new()
    {
        Formats = BarcodeFormats.TwoDimensional, // QR codes only
        AutoRotate = true,
        Multiple = false,
        TryHarder = true
    };

    public SetupQrScannerPage(DeepLinkHandler deepLinkHandler)
    {
        InitializeComponent();
        _deepLinkHandler = deepLinkHandler;
        BindingContext = this;
    }

    /// <summary>
    /// Start scanning and return the result when a valid setup QR code is detected or cancelled.
    /// </summary>
    public Task<SetupQrResult?> ScanAsync(CancellationToken ct = default)
    {
        _scanCompletionSource = new TaskCompletionSource<SetupQrResult?>();

        // Register cancellation
        ct.Register(() =>
        {
            _scanCompletionSource?.TrySetResult(null);
        });

        return _scanCompletionSource.Task;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Prevent multiple detections
        if (_isProcessing) return;
        _isProcessing = true;

        var barcode = e.Results?.FirstOrDefault();
        if (barcode != null && !string.IsNullOrEmpty(barcode.Value))
        {
            System.Diagnostics.Debug.WriteLine($"QR Code detected: {barcode.Value}");

            // Try to parse as a setup deep link
            if (_deepLinkHandler.HandleUri(barcode.Value))
            {
                // Valid setup link found
                QrCodeReader.IsDetecting = false;

                // Vibrate for feedback
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                }
                catch
                {
                    // Vibration may not be available
                }

                var result = new SetupQrResult(
                    _deepLinkHandler.PendingServerUrl!,
                    _deepLinkHandler.PendingServerName);

                // Return result on main thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _scanCompletionSource?.TrySetResult(result);
                    await Navigation.PopModalAsync();
                });
            }
            else
            {
                // Not a valid setup link, update instruction and continue scanning
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    InstructionLabel.Text = "Not a valid setup QR code. Please scan the QR code from your server's Mobile App Setup page.";
                    InstructionLabel.TextColor = Colors.Orange;
                    _isProcessing = false;
                });
            }
        }
        else
        {
            _isProcessing = false;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        QrCodeReader.IsDetecting = false;
        _scanCompletionSource?.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        QrCodeReader.IsDetecting = false;
        _scanCompletionSource?.TrySetResult(null);
    }
}

/// <summary>
/// Result from scanning a setup QR code.
/// </summary>
public class SetupQrResult
{
    public string ServerUrl { get; }
    public string? ServerName { get; }

    public SetupQrResult(string serverUrl, string? serverName)
    {
        ServerUrl = serverUrl;
        ServerName = serverName;
    }
}
