

using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;

using Microsoft.Extensions.Logging;
using QuantumSecure.Views;
using CommunityToolkit.Maui.Views;

namespace QuantumSecure;
public partial class DataViewPage : ContentPage
{

    private readonly ILogger _logger;
    private readonly IMonitorPingInfoView _monitorPingInfoView;
    public DataViewPage(IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();
            _monitorPingInfoView = monitorPingInfoView;
            BindingContext = _monitorPingInfoView;
        }
        catch (Exception ex)
        {
             _logger?.LogError($" Error : Unable to load DataViewPage. Error was: {ex.Message}");
        }

    }

    private async void OnStatusIndicatorTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is View view && view.BindingContext is MPIndicator mpIndicator)
            {
                _monitorPingInfoView?.SelectMonitorPingInfo(mpIndicator.MonitorIPID);
                var monitorPingInfo = _monitorPingInfoView?.SelectedMonitorPingInfo;
                if (monitorPingInfo != null)
                {
                    await ShowDetailsPopup(monitorPingInfo);
                }
            }
        }
        catch (Exception ex)
        {
             _logger?.LogError($" Error : in OnStatusIndicatorTapped on DataViewPage. Error was: {ex.Message}");
        }
    }

    private async Task ShowDetailsPopup(MonitorPingInfo info)
    {
        try
        {
            var popup = new StatusDetailsPopup
            {
                BindingContext = info
            };

            var result = await this.ShowPopupAsync(popup, CancellationToken.None);

            if (result is bool boolResult)
            {
                if (boolResult)
                {

                    var detailsPage = new DetailsPage(_logger,_monitorPingInfoView);
                    await Shell.Current.Navigation.PushAsync(detailsPage);

                }

            }
        }
        catch (Exception e)
        {
             _logger?.LogError($" Error: Could not navigate to page {nameof(DetailsPage)}. Error was: {e.ToString()}");
        }// Yes was tapped
    }
}




