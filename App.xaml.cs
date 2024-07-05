using MetroLog.Maui;
using QuantumSecure.Services;
using QuantumSecure.ViewModels;
using Microsoft.Extensions.Logging;
namespace QuantumSecure;

public partial class App : Application
{

   // public static ProcessorStatesViewModel ProcessorStatesVM { get; private set; }
    private ILogger? _logger;

    public App(IServiceProvider serviceProvider)
    {
       
        try
        {
          
            InitializeComponent();
            _logger = serviceProvider.GetRequiredService<ILogger<App>>();
            MainPage = serviceProvider.GetRequiredService<AppShell>(); ;

            //ProcessorStatesVM = new ProcessorStatesViewModel();
            LogController.InitializeNavigation(
           page => MainPage!.Navigation.PushModalAsync(page),
           () => MainPage!.Navigation.PopModalAsync());
        }
        catch (Exception ex)
        {
            _logger?.LogError($" Error initializing App {ex.Message} ");
        }



    }
}
