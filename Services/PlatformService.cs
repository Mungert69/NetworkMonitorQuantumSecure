#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
#endif
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using NetworkMonitor.Objects;
using NetworkMonitor.Service.Services.OpenAI;

namespace QuantumSecure.Services
{
    public interface IPlatformService
    {
        bool RequestPermissionsAsync();
        Task StartBackgroundService();
        Task StopBackgroundService();
        bool IsServiceStarted { get; set; }
        string ServiceMessage { get; set; }
        Task ChangeServiceState(bool state);
        //void OnServiceStateChanged();
        event EventHandler ServiceStateChanged;
        bool DisableAgentOnServiceShutdown { get; set; }
        void OnUpdateServiceState(ResultObj result, bool state);
    }

    public class PlatformService
    {
        protected ILogger _logger;
        protected IDialogService _dialogService;
        protected bool _isServiceStarted;
        protected string _serviceMessage;
        protected bool _disableAgentOnServiceShutdown = false;
        public event EventHandler ServiceStateChanged;
        //public event EventHandler CloseAgentChanged;
        public bool IsServiceStarted
        {
            get => _isServiceStarted;
            set
            {
                if (_isServiceStarted != value)
                {

                    OnServiceStateChanged();
                    _isServiceStarted = value;

                }
            }
        }
        protected void OnServiceStateChanged()
        {
            ServiceStateChanged?.Invoke(this, EventArgs.Empty);
        }


        public PlatformService(IDialogService dialogService, ILogger logger)
        {
            _dialogService = dialogService;

            _logger = logger;
        }

        public async Task ChangeServiceState(bool state)
        {
            var result = new ResultObj();
            result.Message = " PlatformService : ChangeServiceState : ";
            try
            {
                if (_isServiceStarted && !state)
                {
                    await StopBackgroundService();
                    result.Success = true;
                    result.Message += " Success : Sent toggle service request. ";
                }
                if (!_isServiceStarted && state)
                {
                    await StartBackgroundService();
                    result.Success = true;
                    result.Message += " Success : Sent toggle service request. ";
                }


            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = $" Error : Unable to toggle service state . Error was : {e.Message}";
                _logger.LogError(result.Message);
            }
            _logger.LogInformation($" Running Toggle service.. Result was : {result.Message}");

        }
        public virtual Task StartBackgroundService()
        {
            return Task.CompletedTask;
        }
        public virtual Task StopBackgroundService()
        {
            return Task.CompletedTask;
        }
        public string ServiceMessage { get => _serviceMessage; set => _serviceMessage = value; }
        public bool DisableAgentOnServiceShutdown { get => _disableAgentOnServiceShutdown; set => _disableAgentOnServiceShutdown = value; }
    }
#if ANDROID
    public class AndroidPlatformService : PlatformService, IPlatformService
    {
        private BroadcastReceiver _serviceStatusReceiver;
        private TaskCompletionSource<bool> _serviceOperationCompletionSource;


        public AndroidPlatformService(IDialogService dialogService, ILogger<AndroidPlatformService> logger) : base(dialogService, logger)
        {
            try
            {
                _disableAgentOnServiceShutdown = false;
                InitializeReceiver();
            }
            catch (Exception e)
            {
                logger.LogError($" Error : failed to initialise AndroidPlatformService . Error was : {e.Message}");
            }

        }

        private void InitializeReceiver()
        {
            // Corrected context access
            _serviceStatusReceiver = new ServiceStatusReceiver(this, _logger);
            IntentFilter filter = new IntentFilter(AndroidBackgroundService.ServiceBroadcastAction);
            Android.App.Application.Context.RegisterReceiver(_serviceStatusReceiver, filter);
        }

        public bool RequestPermissionsAsync()
        {
            try
            {
                /*var hasPermissions = await Utils.RequestStoragePermissionsAsync();

                if (!hasPermissions)
                {
                    await _dialogService.DisplayAlert("Permission Request", "This app requires storage and battery optimization permissions for network monitoring. Please grant the permissions for optimal functionality.", "OK");
                    bool openSettings = await _dialogService.DisplayAlert("Permission Denied", "Without the necessary permissions, the app might not work. Would you like to open settings and grant the permissions?", "Yes", "No");
                    if (openSettings)
                    {
                        var intentSettings = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                        intentSettings.AddCategory(Android.Content.Intent.CategoryDefault);
                        intentSettings.SetData(Android.Net.Uri.Parse("package:" + Platform.CurrentActivity.PackageName));
                        Platform.CurrentActivity.StartActivity(intentSettings);
                    }
                }*/
                //await _dialogService.DisplayAlert("Permission Request", "This app requires battery optimization permissions for network monitoring. Please grant the permissions for optimal functionality.", "OK");
                  
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
#pragma warning disable CA1416
                    var powerManager = (PowerManager)Platform.CurrentActivity.GetSystemService(Context.PowerService);
                    if (!powerManager.IsIgnoringBatteryOptimizations(Platform.CurrentActivity.PackageName))
                    {
                        var intentBattery = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                        intentBattery.SetData(Android.Net.Uri.Parse("package:" + Platform.CurrentActivity.PackageName));
                        Platform.CurrentActivity.StartActivity(intentBattery);
                    }
#pragma warning restore CA1416

                }

                //return hasPermissions;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting permissions in AndroidPlatformService");
                return false;
            }
        }

        public override Task StartBackgroundService()
        {
            _serviceOperationCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                 Android.Content.Intent intent = new Android.Content.Intent(Android.App.Application.Context,typeof(AndroidBackgroundService));
   
                 if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                Android.App.Application.Context.StartForegroundService(intent);
                }
                else{
  
                Android.App.Application.Context.StartService(intent);
                }
               
                //_serviceMessage = " Android Service started successfully.";
                //_isServiceStarted=true;

                return _serviceOperationCompletionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting background service in AndroidPlatformService");
                _serviceOperationCompletionSource.SetException(ex);
                return Task.FromException(ex);
            }
        }
        public override Task StopBackgroundService()
        {
            _serviceOperationCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                var intent = new Intent(Platform.CurrentActivity, typeof(AndroidBackgroundService));
                Platform.CurrentActivity.StopService(intent);
                //_serviceMessage = " Android Service stopped successfully.";
                //_isServiceStarted=false;

                return _serviceOperationCompletionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping background service in AndroidPlatformService");
                _serviceOperationCompletionSource.SetException(ex);
                return Task.FromException(ex);
            }
        }

        public  void OnUpdateServiceState(ResultObj result, bool state)
        {
                 try
            {
                
                    
                    // Update PlatformService properties
                    if (result.Success)
                    {
                        IsServiceStarted = state;
                        if (IsServiceStarted) ServiceMessage = " Agent enabled. Complete tasks below to start monitoring...";
                        else
                        {
                            ServiceMessage = " Agent disabled. You can now close the App.";
                        }
                        _logger.LogInformation(result.Message+ServiceMessage);
                        _serviceOperationCompletionSource?.SetResult(true);

                    }
                    else
                    {
                        var stateStr = "start";
                        if (!IsServiceStarted) stateStr = "stop";

                        ServiceMessage = $" Agent service failed to {stateStr}. Service message was : {result.Message}";
                         _logger.LogError(result.Message+ServiceMessage);
                       
                        _serviceOperationCompletionSource?.SetResult(false);

                    }

                    OnServiceStateChanged();

                    // Optionally, notify the UI or log the status
                
            }
            catch (Exception e)
            {
                _logger.LogError($" Error : failed to run OnUpdateServiceState  . Error was : {e.Message}");
            }
    
        }

        private class ServiceStatusReceiver : Android.Content.BroadcastReceiver
        {
            private AndroidPlatformService _platformService;
            private ILogger _logger;

            public ServiceStatusReceiver(AndroidPlatformService platformService, ILogger logger)
            {
                _platformService = platformService;
                _logger = logger;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                try
                {
                    if (intent.Action == AndroidBackgroundService.ServiceBroadcastAction)
                    {
                        bool serviceChangeSuccess = intent.GetBooleanExtra(AndroidBackgroundService.ServiceStatusExtra, false);
                        string message = intent.GetStringExtra(AndroidBackgroundService.ServiceMessageExtra);

                        // Update PlatformService properties
                        if (serviceChangeSuccess)
                        {
                            _platformService.IsServiceStarted = !_platformService.IsServiceStarted;
                            if (_platformService.IsServiceStarted) _platformService.ServiceMessage = " Agent enabled. Complete tasks below to start monitoring...";
                            else
                            {
                                _platformService.ServiceMessage = " Agent disabled. You can now close the App.";
                            }
                            _platformService._serviceOperationCompletionSource?.SetResult(true);

                        }
                        else
                        {
                            var stateStr = "start";
                            if (!_platformService.IsServiceStarted) stateStr = "stop";

                            _platformService.ServiceMessage = $" Agent service failed to {stateStr}. Service message was : {message}";
                            _platformService._serviceOperationCompletionSource?.SetResult(false);

                        }

                        _platformService.OnServiceStateChanged();

                        // Optionally, notify the UI or log the status
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($" Error : could not receive broadcast in background service . Error was : {e.Message}");
                }

            }
        }

    }
#endif
    public class WindowsPlatformService : PlatformService, IPlatformService
    {
        private IBackgroundService _backgroundService;

        public WindowsPlatformService(IBackgroundService backgroundService, IDialogService dialogService, ILogger<WindowsPlatformService> logger) : base(dialogService, logger)
        {
            _backgroundService = backgroundService;
        }

        public bool RequestPermissionsAsync()
        {
            try
            {
                // Windows-specific permission logic here
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting permissions in WindowsPlatformService");
                return false;
            }
        }

        public void OnUpdateServiceState(ResultObj result, bool state)
        {
            try
            {
                // Windows-specific permission logic here

            }
            catch (Exception ex)
            {
                _logger.LogError($" Error :  OnUpdateServiceState : {ex.Message}");

            }
        }

        public override async Task StartBackgroundService()
        {
            try
            {
                var result = await _backgroundService.Start();
                if (result.Success) _serviceMessage = " Agent enabled. Complete tasks below to start monitoring...";
                else _serviceMessage = $" Agent service failed to start. Service message was : {result.Message}";
                if (result.Success) _isServiceStarted = true;
                OnServiceStateChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting background service in WindowsPlatformService");
            }
        }

        public override async Task StopBackgroundService()
        {
            try
            {
                var result = await _backgroundService.Stop();
                if (result.Success) _serviceMessage = " Agent disabled. You can now close the App.";
                else _serviceMessage = $" Agent service failed to stop. Service message was : {result.Message}";
                if (result.Success) _isServiceStarted = false;
                OnServiceStateChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping background service in WindowsPlatformService");
            }
        }
    }
}
