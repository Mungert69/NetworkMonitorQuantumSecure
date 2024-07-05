using MetroLog.Maui;
using NetworkMonitor.Processor.Services;
using QuantumSecure.Services;
using QuantumSecure.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Microsoft.Maui.Graphics;  // For Rectangle
using Microsoft.Maui.Layouts;  // For AbsoluteLayoutFlags
namespace QuantumSecure;
public partial class LogsPage : ContentPage
{
    private ILogger _logger;
    private LogController _logController;
    public LogsPage()
    {
        InitializeComponent();
        _logController = new LogController();
        BindingContext = _logController;
        _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<ConfigPage>>();
    }
    private async void ViewLogsButton_Clicked(object sender, EventArgs e)
    {
        try
        {

            _logController?.GoToLogsPageCommand.Execute(null);
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Unable to open logs page . Error was : {ex.Message}", "OK");
            _logger.LogError($" Error : Unable to open logs page . Error was : {ex.ToString()}");
        }
    }
}
