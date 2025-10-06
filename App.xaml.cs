using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.Utils;
using MetroLog.Maui;
namespace QuantumSecure;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        try
        {
            InitializeComponent();
            MainPage = serviceProvider.GetRequiredService<AppShell>();
            LogController.InitializeNavigation(page => MainPage!.Navigation.PushModalAsync(page), () => MainPage!.Navigation.PopModalAsync());

        }
        catch (Exception ex)
        {
            // Show a blocking error page to the user if app fails to start
            MainPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "Startup Error",
                            TextColor = Colors.Red,
                            FontSize = 22,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = ex.Message,
                            TextColor = Colors.Black,
                            FontSize = 16,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    }
                }
            };
            // Log and send the error as well
            NetworkMonitor.Utils.Helpers.ExceptionHelper.HandleGlobalException(ex, "App Startup Exception");
        }
    }

}
