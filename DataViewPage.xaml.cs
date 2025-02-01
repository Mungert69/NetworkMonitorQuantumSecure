
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Views;
using QuantumSecure.Views;


namespace QuantumSecure;
public partial class DataViewPage : ContentPage
{

    private ILogger _logger;
    private IMonitorPingInfoView _monitorPingInfoView;
    public DataViewPage(ILogger logger, IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();
            _logger = logger;
            _monitorPingInfoView = monitorPingInfoView;
            BindingContext = monitorPingInfoView;
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load DataViewPage. Error was: {ex.Message}");
        }

    }

    private async void OnStatusIndicatorTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is View view && view.BindingContext is MPIndicator mpIndicator)
            {
                var monitorPingInfoView = BindingContext as IMonitorPingInfoView;
                monitorPingInfoView?.SelectMonitorPingInfo(mpIndicator.MonitorIPID);

                var monitorPingInfo = monitorPingInfoView?.SelectedMonitorPingInfo;
                if (monitorPingInfo != null)
                {
                    await ShowDetailsPopup(monitorPingInfo);
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnStatusIndicatorTapped on DataViewPage. Error was: {ex.Message}");
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

                    var detailsPage = new DetailsPage(_logger, _monitorPingInfoView);
                    await Shell.Current.Navigation.PushAsync(detailsPage);

                }

            }
        }
        catch (Exception e)
        {
            _logger.LogError($" Error: Could not navigate to page {nameof(DetailsPage)}. Error was: {e.ToString()}");
        }// Yes was tapped
    }
}




