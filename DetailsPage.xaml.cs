using NetworkMonitor.Objects;
using QuantumSecure.ViewModels;
using NetworkMonitor.DTOs;
namespace QuantumSecure;
public partial class DetailsPage : ContentPage
{
    public DetailsPage(IMonitorPingInfoView monitorPingInfoView)
    {
        InitializeComponent();
        BindingContext = monitorPingInfoView;
    }

 private async  void OnBackButton_Clicked(object sender, EventArgs e)
    {
        // Navigate back to the previous page
             await Shell.Current.Navigation.PopAsync();
    }
          
}
