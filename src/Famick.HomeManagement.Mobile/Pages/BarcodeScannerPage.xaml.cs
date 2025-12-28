using ZXing.Net.Maui;

namespace Famick.HomeManagement.Mobile.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private TaskCompletionSource<string?>? _scanCompletionSource;
    private bool _isProcessing;

    public BarcodeReaderOptions BarcodeOptions { get; } = new()
    {
        Formats = BarcodeFormats.OneDimensional,
        AutoRotate = true,
        Multiple = false,
        TryHarder = true
    };

    public BarcodeScannerPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    /// <summary>
    /// Start scanning and return the result when a barcode is detected or cancelled.
    /// </summary>
    public Task<string?> ScanAsync(CancellationToken ct = default)
    {
        _scanCompletionSource = new TaskCompletionSource<string?>();

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
            // Stop detecting
            BarcodeReader.IsDetecting = false;

            // Vibrate for feedback
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
            }
            catch
            {
                // Vibration may not be available
            }

            // Return result on main thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _scanCompletionSource?.TrySetResult(barcode.Value);
                await Navigation.PopModalAsync();
            });
        }
        else
        {
            _isProcessing = false;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        BarcodeReader.IsDetecting = false;
        _scanCompletionSource?.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
        _scanCompletionSource?.TrySetResult(null);
    }
}
