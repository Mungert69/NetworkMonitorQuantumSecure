﻿using MetroLog.Maui;
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
    private MainPageViewModel _mainPageViewModel;


    public MainPage(IAuthService authService, NetConnectConfig netConfig, ILogger logger, MainPageViewModel mainPageViewModel, ProcessorStatesViewModel processorStatesViewModel)
    {
        try
        {
            InitializeComponent();
            _authService = authService;
            _netConfig = netConfig;
            _logger = logger;
            //Application.Current.UserAppTheme = AppTheme.Dark;
            _mainPageViewModel = mainPageViewModel;
            _mainPageViewModel.ShowLoadingMessage += (sender, show) => ShowLoadingNoCancel(show);
            _mainPageViewModel.AuthorizeAction += Authorize;
            _mainPageViewModel.LoginAction += OpenLoginWebsite;
            _mainPageViewModel.AddHostsAction += ScanHosts;
            _mainPageViewModel.SetupTasks();
            BindingContext = _mainPageViewModel;

            CustomPopupView.BindingContext = processorStatesViewModel;
            ProcessorStatesView.BindingContext = processorStatesViewModel;
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load MainPage. Error was: {ex.Message}");
        }

    }


    private async void Authorize()
    {
        string authUrl = "not set";
        try
        {

            var resultInit = await Task.Run(async () => await  _authService.InitializeAsync());

            if (!resultInit.Success)
            {
                await DisplayAlert("Error", resultInit.Message, "OK");
                _mainPageViewModel.IsPolling = false; // Reset the flag
                return;
            }
            var resultSend = await Task.Run(async () => await _authService.SendAuthRequestAsync());

            if (!resultSend.Success)
            {
                await DisplayAlert("Error", resultSend.Message, "OK");
                _mainPageViewModel.IsPolling = false; // Reset the flag
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
                _mainPageViewModel.IsPolling = false; // Reset the flag
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not authorize with requested authUrl {authUrl} . Errro was : {ex.Message}", "OK");
            _logger.LogError($" Error : Could not authorize with requested authUrl {authUrl} . Errro was : {ex.ToString()}");
            _mainPageViewModel.IsPolling = false; // Reset the flag
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
            _mainPageViewModel.IsPolling = false; // Reset the flag
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
            var result= await Task.Run(async () => await _authService.PollForTokenAsync(_cancellationTokenSource.Token)); 
            ShowLoading(false);
            _mainPageViewModel.IsPolling = false; // Reset the flag

            if (result.Success)
            {
                 await DisplayAlert("Success", $"Authorization successful! Now login to Free Network Monitor and add hosts to monitor from your device. choose '{_netConfig.MonitorLocation}' as the monitor location . ", "OK");

            }
            else
            {
                await DisplayAlert("Fail", result.Message, "OK");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError($" Error : Unable to complete Polling in background. Error was : {ex.Message}");
            _mainPageViewModel.IsPolling = false; // Reset the flag
        }
        finally
        {
            _cancellationTokenSource?.Dispose();

        }

    }



}

