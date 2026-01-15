namespace Famick.HomeManagement.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Add global exception handlers for debugging
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex?.Message}\n{ex?.StackTrace}");
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var page = Current?.Windows?.FirstOrDefault()?.Page;
				if (page != null)
				{
					page.DisplayAlert(
						"Unhandled Exception",
						$"{ex?.GetType().Name}: {ex?.Message}",
						"OK");
				}
			});
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception?.Message}\n{e.Exception?.StackTrace}");
			e.SetObserved();
		};
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
