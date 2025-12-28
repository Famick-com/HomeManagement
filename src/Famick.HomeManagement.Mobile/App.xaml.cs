namespace Famick.HomeManagement.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Wrap MainPage in NavigationPage to enable modal navigation (required for barcode scanner)
		var mainPage = new MainPage();
		NavigationPage.SetHasNavigationBar(mainPage, false);
		var navPage = new NavigationPage(mainPage);

		return new Window(navPage)
		{
			Title = "Famick.HomeManagement.Mobile"
		};
	}
}
