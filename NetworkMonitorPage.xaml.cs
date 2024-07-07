using NetworkMonitor.Api.Services;
using QuantumSecure.ViewModels;
namespace QuantumSecure;
public partial class NetworkMonitorPage : ContentPage
{
    public NetworkMonitorPage(IApiService apiService)
    {
        InitializeComponent();
        BindingContext = new NetworkMonitorViewModel(apiService);


    }
}