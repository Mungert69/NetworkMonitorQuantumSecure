
using NetworkMonitor.DTOs;
using Microsoft.Extensions.Logging;
namespace QuantumSecure;
public partial class DetailsPage : ContentPage
{


    private readonly ILogger _logger;
    public DetailsPage(IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();

            BindingContext = monitorPingInfoView;

        }
        catch (Exception ex)
        {
              }
    }

    private async void OnBackButton_Clicked(object sender, EventArgs e)
    {
        try {
        // Navigate back to the previous page
        await Shell.Current.Navigation.PopAsync();}
         catch (Exception ex)
        {
        }
    }

}
