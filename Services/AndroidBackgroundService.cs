#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Configuration;
using AndroidX.Core.App;
using NetworkMonitor.Processor.Services;


namespace QuantumSecure.Services
{

    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AndroidBackgroundService : Service
    {
        private CancellationTokenSource _cts;
        // This is any integer value unique to the application.
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        private ILogger _logger;
        private NetConnectConfig _netConfig;
        private ILoggerFactory _loggerFactory;
        private IRabbitRepo _rabbitRepo;
       private IBackgroundService _backgroundService;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        private ICmdProcessorProvider _cmdProcessorProvider ;
        private IPlatformService _platformService;

        private IFileRepo _fileRepo;
public const string ServiceBroadcastAction = "com.networkmonitor.service.STATUS";
public const string ServiceStatusExtra = "ServiceStatus";
public const string ServiceMessageExtra = "ServiceMessage";

        public AndroidBackgroundService()
        {

        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override void OnCreate()
        {
            base.OnCreate();
             _cts = new CancellationTokenSource();

            _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<AndroidBackgroundService>>();
            _netConfig = MauiProgram.ServiceProvider.GetRequiredService<NetConnectConfig>();
            _loggerFactory = MauiProgram.ServiceProvider.GetRequiredService<ILoggerFactory>();
            _fileRepo = MauiProgram.ServiceProvider.GetRequiredService<IFileRepo>();
            _rabbitRepo = MauiProgram.ServiceProvider.GetRequiredService<IRabbitRepo>();
            _monitorPingInfoView = MauiProgram.ServiceProvider.GetRequiredService<IMonitorPingInfoView>();
            _processorStates=MauiProgram.ServiceProvider.GetRequiredService<LocalProcessorStates>();
            _cmdProcessorProvider=MauiProgram.ServiceProvider.GetRequiredService<ICmdProcessorProvider>();
            _platformService= MauiProgram.ServiceProvider.GetRequiredService<IPlatformService>();
            _backgroundService = new BackgroundService(_logger, _netConfig, _loggerFactory, _rabbitRepo, _fileRepo,_processorStates, _monitorPingInfoView, _cmdProcessorProvider );

        }
        private async Task StartAsync()
        {
            try
            {
                var result = await _backgroundService.Start();
               _platformService.OnUpdateServiceState(result, true);

            }
            catch (Exception ex)
            {
               _logger.LogError($"Error initializing background service: {ex.Message}");
            }
        }

        private async Task StopAsync()
        {
            try
            {
            var result = await _backgroundService.Stop();
            _platformService.OnUpdateServiceState(result,false);
            }
            catch (Exception ex)
            {
            _logger.LogError($"Error stopping background service: {ex.Message}");
            }
        }

        private PendingIntent GetViewAppPendingIntent()
        {
            var viewAppIntent = new Intent(this, typeof(MainActivity)); // Replace 'MainActivity' with your main activity class
            viewAppIntent.SetAction(Intent.ActionMain);
            viewAppIntent.AddCategory(Intent.CategoryLauncher);
            return PendingIntent.GetActivity(this, 0, viewAppIntent, 0);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
             if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            try { 
                if (intent.Action == "STOP_SERVICE") {

                Task.Run(async () =>
                    {
#pragma warning disable CA1422
                        StopForeground(true);
                    //StopBackgroundService(true);
#pragma warning restore CA1422
                        await StopAsync();
                    }, _cts.Token);
                return StartCommandResult.Sticky;
                
                }
               
            }
            catch (Exception e){
                var result=new ResultObj(){Message=$" Error : Failed to Stop service . Error was : {e.Message}",Success=false};
                _platformService.OnUpdateServiceState(result,false);
            }                          
            try
            {
                 Task.Run(async () =>
                {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
#pragma warning disable CA1416, CA1422
                    Notification notification;
                    NotificationChannel channel = new NotificationChannel("channel_id", "Quantum Secure Agent", NotificationImportance.Low);
                    NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                    notificationManager.CreateNotificationChannel(channel);
                    /*var stopAction = new Notification.Action.Builder(
                            QuantumSecure.Resource.Drawable.stop,
                            "Stop",
                            GetStopServicePendingIntent())
                             .Build();

                    var viewAction = new Notification.Action.Builder(
                         QuantumSecure.Resource.Drawable.view,
                         "View",
                         GetViewAppPendingIntent())
                        .Build();
                        */

                     notification = new Notification.Builder(this, "channel_id")
                        .SetAutoCancel(false)
                        .SetOngoing(true)
                        .SetContentTitle("Quantum Secure Agent")
                        .SetContentText("Monitoring network...")
                        .SetSmallIcon(QuantumSecure.Resource.Drawable.logo)
                        .Build();
                    if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                    {
                        StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
                    }
                    else
                    {
                        StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification,
                        Android.Content.PM.ForegroundService.TypeConnectedDevice);
                        
                    }    
#pragma warning restore CA1416, CA1422
                }
                else
                {
#pragma warning disable CS0618
                    // For API below 26
                    Notification notification = new NotificationCompat.Builder(this)
                                   .SetContentTitle("Quantum Secure Agent")
                                   .SetContentText("Monitoring network...")
                                   .SetSmallIcon(QuantumSecure.Resource.Drawable.logo)
                                   .SetOngoing(true)
                                   //.AddAction(QuantumSecure.Resource.Drawable.stop, "Stop", GetStopServicePendingIntent())
                                   .AddAction(QuantumSecure.Resource.Drawable.view, "Open", GetViewAppPendingIntent()) // Ensure you have an icon for 'View App'
                                   .Build();
                    StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
#pragma warning restore CS0618
                }
                await StartAsync();
                }, _cts.Token);
            }
           catch (Exception e){
             var result=new ResultObj(){Message=$" Error : Failed to Start service . Error was : {e.Message}",Success=false};
             _platformService.OnUpdateServiceState(result,true);
            }
            return StartCommandResult.Sticky;
        }

       public override void OnDestroy()
{
    try
    {
        Task.Run(async () =>
        {
            await StopAsync();
        }).Wait(TimeSpan.FromSeconds(10)); // Give it 5 seconds to complete
    }
    catch (Exception e)
    {
        _logger.LogError($" Error stopping service in OnDestroy: {e.Message}");
    }
    finally
    {
        base.OnDestroy();
    }
}
    }
}
#endif