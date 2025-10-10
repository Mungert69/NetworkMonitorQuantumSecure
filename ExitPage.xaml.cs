using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui.Services;
using System.Threading.Tasks;
using System;

#if ANDROID
using Microsoft.Maui.ApplicationModel;
using Android.App;
#endif

namespace QuantumSecure;

public partial class ExitPage : ContentPage
{
    private readonly ExitPageViewModel _viewModel;
    private readonly IPlatformService _platformService;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger _logger;

    public ExitPage(ExitPageViewModel viewModel, IPlatformService platformService, IUiDispatcher? dispatcher = null, ILogger<ExitPage>? logger = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _platformService = platformService;
        _dispatcher = dispatcher ?? ServiceInitializer.Dispatcher;
        _logger = logger;

        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ConfigureForPlatform();
    }

    private void ConfigureForPlatform()
    {
        try
        {
#if ANDROID
            // On Android we usually want: Keep agent running (minimize) OR Stop agent + Exit
            InfoLabel.Text = "Close App";
            DetailLabel.Text = "You can keep the agent running and minimize the app, or stop the agent and exit.";

            PrimaryButton.Text = "Keep Agent Running and Minimize";
            SecondaryButton.Text = "Stop Agent and Exit";
#else
            // Default (Windows / other): stop service and exit OR cancel
            InfoLabel.Text = "Exit App";
            DetailLabel.Text = "Stopping the agent on exit is recommended on desktop platforms.";

            PrimaryButton.Text = "Stop Agent and Exit";
            SecondaryButton.IsVisible = false;
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error configuring ExitPage UI for platform");
        }
    }

    private async void OnPrimaryClicked(object sender, EventArgs e)
    {
        try
        {
#if ANDROID
            // Minimize the app but keep the service running
            try
            {
                var activity = Platform.CurrentActivity;
                activity?.MoveTaskToBack(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Could not minimize on Android");
            }
#else
            // On desktop stop service then exit
            await StopAgentAndExitAsync();
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnPrimaryClicked");
        }
    }

    private async void OnSecondaryClicked(object sender, EventArgs e)
    {
        try
        {
#if ANDROID
            // Stop agent then exit
            await StopAgentAndExitAsync();
#else
            // For platforms where secondary is hidden this shouldn't happen
            await StopAgentAndExitAsync();
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnSecondaryClicked");
        }
    }

    private async Task StopAgentAndExitAsync()
    {
        try
        {
            // Attempt to stop the platform service gracefully if available
            try
            {
                // IPlatformService.ChangeServiceState(bool) is used elsewhere — use it if present
                await _platformService.ChangeServiceState(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "PlatformService could not stop agent gracefully");
            }

            // Give the service a moment to shutdown
            await Task.Delay(300);

            // Exit app
            System.Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while stopping agent and exiting");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            // simply navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating back from ExitPage");
        }
    }
}
