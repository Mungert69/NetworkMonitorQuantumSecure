using NetworkMonitor.Api.Services;
using QuantumSecure.ViewModels;
namespace QuantumSecure;
public partial class NetworkMonitorPage : ContentPage
{
    public NetworkMonitorPage(NetworkMonitorViewModel networkMonitorViewModel)
    {
        InitializeComponent();
        BindingContext = networkMonitorViewModel;
    }
}