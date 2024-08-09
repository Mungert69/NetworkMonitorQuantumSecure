using MetroLog.Maui;
using NetworkMonitor.Processor.Services;
using QuantumSecure.Services;
using QuantumSecure.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;

namespace QuantumSecure;


public partial class MainPage : ContentPage
{
    private IAuthService _authService;
    private NetConnectConfig _netConfig;
    private ILogger _logger;
    private CancellationTokenSource _cancellationTokenSource;
    private IPlatformService _platformService;


    public MainPage(IAuthService authService, NetConnectConfig netConfig, ILogger logger, IPlatformService platformService, ProcessorStatesViewModel processorStatesViewModel)
    {
        InitializeComponent();
        _authService = authService;
        _netConfig = netConfig;
        _logger = logger;
        _platformService = platformService;

        //Application.Current.UserAppTheme = AppTheme.Dark;
        var mainPageViewModel = new MainPageViewModel(_netConfig, Authorize, OpenLoginWebsite, ScanHosts, _platformService, _logger);
        mainPageViewModel.ShowLoadingMessage += (sender, show) => ShowLoadingNoCancel(show);
        BindingContext= mainPageViewModel;

        CustomPopupView.BindingContext = processorStatesViewModel;
        ProcessorStatesView.BindingContext = processorStatesViewModel;
       /* WeakReferenceMessenger.Default.Register<ShowLoadingMessage>(this, (recipient, message) =>
        {
            ShowLoadingNoCancel(message.Show);
        });*/

    }


    private async void Authorize()
    {
        string authUrl = "not set";
        try
        {

            var resultInit = await _authService.InitializeAsync();
            if (!resultInit.Success)
            {
                await DisplayAlert("Error", resultInit.Message, "OK");
                ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
                return;
            }
            var resultSend = await _authService.SendAuthRequestAsync();

            if (!resultSend.Success)
            {
                await DisplayAlert("Error", resultSend.Message, "OK");
                ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
                return;
            }


            authUrl = _netConfig.ClientAuthUrl;
            _logger.LogInformation($" authUrl is {authUrl}");
            if (!string.IsNullOrWhiteSpace(authUrl))
            {

                PollForTokenInBackground();
                //await Launcher.OpenAsync(authUrl);

                await Browser.Default.OpenAsync(authUrl, BrowserLaunchMode.SystemPreferred);


            }
            else
            {
                // Handle the case where the URL is not available
                await DisplayAlert("Error", "Authorization URL is not available.", "OK");
                _logger.LogError(" Error : Authorization URL is not available.");
                ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not authorize with requested authUrl {authUrl} . Errro was : {ex.Message}", "OK");
            _logger.LogError($" Error : Could not authorize with requested authUrl {authUrl} . Errro was : {ex.ToString()}");
            ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
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

    private async void ScanHosts()
    {
        try
        {
            await Shell.Current.GoToAsync("//Scan");
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not navigate to scan page . Error was : {ex.Message}", "OK");
            _logger.LogError($"Could not navigate to scan page. Error was : {ex.ToString()}");

        }
    }
    private void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
            _cancellationTokenSource?.Cancel(); // Cancel the polling operation
            CancelButton.IsVisible = false; // Hide the Cancel button
            ShowLoading(false); // Reset the content to the original state
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            DisplayAlert("Error", $"Could not complete Cancel click . Errro was : {ex.Message}", "OK");
            _logger.LogError($"Could not complete Cancel click. Errro was : {ex.ToString()}");

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

    private async void PollForTokenInBackground()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            ShowLoading(true); // Show progress indicator and cancel button
            var result = await Task.Run(async () =>
                await _authService.PollForTokenAsync(_cancellationTokenSource.Token)); // Pass the token

            // Using Dispatcher.Dispatch instead of Device.BeginInvokeOnMainThread
            Dispatcher.Dispatch(async () =>
            {
                ShowLoading(false);
                ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag

                if (result.Success)
                {
                    //(BindingContext as MainPageViewModel)?.UpdateTaskCompletion("Authorize Agent", true);

                    await DisplayAlert("Success", $"Authorization successful! Now login to Quantum Secure and add hosts to monitor from your device. choose '{_netConfig.MonitorLocation}' as the monitor location . ", "OK");

                }
                else
                {
                    await DisplayAlert("Fail", result.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($" Error : Unable to complete Polling in background. Error was : {ex.Message}");
            ((MainPageViewModel)BindingContext).IsPolling = false; // Reset the flag
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            
        }

    }



}

