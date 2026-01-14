using Famick.HomeManagement.ShoppingMode.Pages;

namespace Famick.HomeManagement.ShoppingMode;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation (LoginPage is shown modally, not via Shell routing)
        Routing.RegisterRoute(nameof(ServerConfigPage), typeof(ServerConfigPage));
        Routing.RegisterRoute(nameof(ShoppingSessionPage), typeof(ShoppingSessionPage));
        Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
        Routing.RegisterRoute(nameof(BarcodeScannerPage), typeof(BarcodeScannerPage));
    }
}
