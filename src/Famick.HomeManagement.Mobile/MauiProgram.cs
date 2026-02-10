using CommunityToolkit.Maui;
using Famick.HomeManagement.Mobile.Pages;
using Famick.HomeManagement.Mobile.Pages.Onboarding;
using Famick.HomeManagement.Mobile.Pages.Wizard;
using Famick.HomeManagement.Mobile.Services;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace Famick.HomeManagement.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if IOS
        // Disable iOS Password AutoFill floating button
        Platforms.iOS.DisableAutoFillHandler.Register();
#endif

        // API Settings (singleton - configures server URL)
        var apiSettings = new ApiSettings();
        builder.Services.AddSingleton(apiSettings);

        // Configure HttpClient with dynamic base URL and automatic token refresh
        builder.Services.AddScoped(sp =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            var tokenStorage = sp.GetRequiredService<TokenStorage>();
            var innerHandler = new DynamicApiHttpHandler(settings);
            var authHandler = new AuthenticatingHttpHandler(tokenStorage, settings)
            {
                InnerHandler = innerHandler
            };
            return new HttpClient(authHandler)
            {
                BaseAddress = new Uri(settings.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        });

        // Core Services
        builder.Services.AddSingleton<TokenStorage>();
        builder.Services.AddSingleton<TenantStorage>();
        builder.Services.AddSingleton<OnboardingService>();
        builder.Services.AddScoped<ShoppingApiClient>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<OfflineStorageService>();
        builder.Services.AddSingleton<BleScannerService>();
        builder.Services.AddScoped<ImageCacheService>();

        // ConnectivityService needs ShoppingApiClient, register as scoped to match ShoppingApiClient's lifetime
        builder.Services.AddScoped<ConnectivityService>();

        // OAuth Service for social login
        builder.Services.AddScoped<OAuthService>();

        // Platform-specific Apple Sign in service
#if IOS
        builder.Services.AddSingleton<IAppleSignInService, Platforms.iOS.AppleSignInService>();
#elif ANDROID
        builder.Services.AddSingleton<IAppleSignInService, Platforms.Android.AppleSignInService>();
#endif

        // Platform-specific Google Sign in service
#if IOS
        builder.Services.AddSingleton<IGoogleSignInService, Platforms.iOS.GoogleSignInService>();
#elif ANDROID
        builder.Services.AddSingleton<IGoogleSignInService, Platforms.Android.GoogleSignInService>();
#endif

        // Onboarding Pages (only those that can be resolved by DI)
        // Note: EmailVerificationPage and CreatePasswordPage have runtime parameters
        // and are created manually during navigation
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<QrScannerPage>();

        // Main App Pages (registered for DI navigation)
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ForceChangePasswordPage>();
        builder.Services.AddTransient<ServerConfigPage>();
        builder.Services.AddTransient<ListSelectionPage>();
        builder.Services.AddTransient<ShoppingSessionPage>();
        builder.Services.AddTransient<AddItemPage>();
        builder.Services.AddTransient<BarcodeScannerPage>();
        builder.Services.AddTransient<AisleOrderPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QuickConsumePage>();
        builder.Services.AddTransient<ChildProductSelectionPage>();
        builder.Services.AddTransient<InventorySessionPage>();
        builder.Services.AddTransient<StockOverviewPage>();

        // Wizard Pages
        builder.Services.AddTransient<WizardHouseholdInfoPage>();
        builder.Services.AddTransient<WizardMembersPage>();
        builder.Services.AddTransient<WizardHomeStatsPage>();
        builder.Services.AddTransient<WizardMaintenancePage>();
        builder.Services.AddTransient<WizardVehiclesPage>();
        // Note: WizardVehicleEditPage has runtime parameters and is created manually

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
