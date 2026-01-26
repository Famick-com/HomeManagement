using Famick.HomeManagement.Mobile.Services;
using ZXing.Net.Maui;

namespace Famick.HomeManagement.Mobile.Pages.Onboarding;

public partial class QrScannerPage : ContentPage
{
    private readonly ApiSettings _apiSettings;
    private readonly ShoppingApiClient _apiClient;
    private bool _isProcessing;

    public BarcodeReaderOptions BarcodeOptions { get; } = new()
    {
        Formats = BarcodeFormat.QrCode,
        AutoRotate = true,
        Multiple = false
    };

    public bool ShowOverlay => true;

    public QrScannerPage(ApiSettings apiSettings, ShoppingApiClient apiClient)
    {
        InitializeComponent();
        _apiSettings = apiSettings;
        _apiClient = apiClient;
        BindingContext = this;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;

        var barcode = e.Results.FirstOrDefault();
        if (barcode == null) return;

        _isProcessing = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ProcessQrCodeAsync(barcode.Value);
        });
    }

    private async Task ProcessQrCodeAsync(string qrValue)
    {
        ShowStatus("Processing QR code...", true);

        try
        {
            // Expected format: famick://setup?url=https://server.com&name=HouseholdName
            // Or: famick://setup?url=https://server.com
            var parsed = ParseQrCode(qrValue);

            if (parsed == null)
            {
                ShowStatus("Invalid QR code format. Please scan a Famick setup QR code.", false);
                await Task.Delay(2000);
                _isProcessing = false;
                HideStatus();
                return;
            }

            ShowStatus($"Connecting to {parsed.Value.tenantName ?? "server"}...", true);

            // Test connection to the server
            _apiSettings.Mode = ServerMode.SelfHosted;
            _apiSettings.SelfHostedUrl = parsed.Value.url;

            var isHealthy = await _apiClient.CheckHealthAsync();

            if (isHealthy)
            {
                // Configure server from QR code
                _apiSettings.ConfigureFromQrCode(parsed.Value.url, parsed.Value.tenantName);

                ShowStatus("Server configured! Redirecting to login...", false);
                await Task.Delay(1000);

                // Navigate to login page
                await Navigation.PushAsync(
                    Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<LoginPage>());
            }
            else
            {
                ShowStatus("Could not connect to the server. Please try again.", false);
                await Task.Delay(2000);
                _isProcessing = false;
                HideStatus();
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", false);
            await Task.Delay(2000);
            _isProcessing = false;
            HideStatus();
        }
    }

    private static (string url, string? tenantName)? ParseQrCode(string qrValue)
    {
        try
        {
            // Handle famick:// scheme
            if (qrValue.StartsWith("famick://setup", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(qrValue);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                var url = query["url"];
                var name = query["name"];

                if (!string.IsNullOrEmpty(url))
                {
                    return (url, name);
                }
            }

            // Handle direct URL (for backwards compatibility)
            if (qrValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                qrValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return (qrValue, null);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ShowStatus(string message, bool showLoading)
    {
        StatusFrame.IsVisible = true;
        StatusLabel.Text = message;
        LoadingIndicator.IsRunning = showLoading;
        LoadingIndicator.IsVisible = showLoading;
    }

    private void HideStatus()
    {
        StatusFrame.IsVisible = false;
    }
}
