using NetworkMonitor.Api.Services;
using NetworkMonitor.Maui.ViewModels;
namespace QuantumSecure;
public partial class NetworkMonitorPage : ContentPage
{
    public NetworkMonitorPage(IApiService apiService)
    {
        InitializeComponent();
        BindingContext = new NetworkMonitorViewModel(apiService);


    }
}