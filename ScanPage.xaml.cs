using MetroLog.Maui;
using NetworkMonitor.Processor.Services;
using QuantumSecure.Services;
using QuantumSecure.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace QuantumSecure;

public partial class ScanPage : ContentPage
{

    private ILogger _logger;

    private ScanProcessorStatesViewModel _scanProcessorStatesViewModel;

    public ScanPage(ILogger logger, ScanProcessorStatesViewModel scanProcessorStatesViewModel)
    {
        InitializeComponent();
        _scanProcessorStatesViewModel=scanProcessorStatesViewModel;
        _logger = logger;

        CustomPopupView.BindingContext = scanProcessorStatesViewModel;
        BindingContext = scanProcessorStatesViewModel;
        EndpointTypePicker.SelectedIndexChanged += OnEndpointTypePickerSelectedIndexChanged;

        WeakReferenceMessenger.Default.Register<ShowLoadingMessage>(this, (recipient, message) =>
        {
            ShowLoadingNoCancel(message.Show);
        });

    }

 private void OnEndpointTypePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (EndpointTypePicker.SelectedItem is string selectedEndpointType)
        {
            _scanProcessorStatesViewModel.DefaultEndpointType = selectedEndpointType;
        }
    }

    private async void OpenLoginWebsite()
    {

        try
        {//await Launcher.OpenAsync("https://freenetworkmonitor.click/dashboard");
            await Browser.Default.OpenAsync("https://freenetworkmonitor.click/dashboard", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not open browser . Errro was : {ex.Message}", "OK");
            _logger.LogError($"Could not open browser. Errro was : {ex.ToString()}");

        }

    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        try
        {
            await Task.Run(async () =>
            {
                await _scanProcessorStatesViewModel.Scan();
            });
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not scan local hosts . Error was : {ex.Message}", "OK");
            _logger.LogError($"Could not scan local hosts. Error was : {ex.ToString()}");

        }
    }
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            await _scanProcessorStatesViewModel.Cancel();
            CancelButton.IsVisible = false; // Hide the Cancel button
            ShowLoading(false); // Reset the content to the original state
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not complete Cancel click . Error was : {ex.Message}", "OK");
            _logger.LogError($"Could not complete Cancel click. Error was : {ex.ToString()}");

        }


    }

    private void ShowLoading(bool show)
    {
        ProgressIndicator.IsVisible = show;
        ProgressIndicator.IsRunning = show;
        CancelButton.IsVisible = show;
    }
    private void ShowLoadingNoCancel(bool show)
    {
        ProgressIndicator.IsVisible = show;
        ProgressIndicator.IsRunning = show;
        CancelButton.IsVisible = false;
    }



}

