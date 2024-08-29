using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
namespace QuantumSecure.Services
{
    public interface IBackgroundService
    {
        Task<ResultObj> Start();
        Task<ResultObj> Stop();
    }
    public class BackgroundService : IBackgroundService
    {
        // This is any integer value unique to the application.
        private ILogger _logger;
        private NetConnectConfig _netConfig;
        private ILoggerFactory _loggerFactory;
        private MonitorPingProcessor _monitorPingProcessor;
        private ICmdProcessor _nmapCmdProcessor;
         private ICmdProcessor _metaCmdProcessor;
        private IRabbitRepo _rabbitRepo;
        private IRabbitListener _rabbitListener;
        private IFileRepo _fileRepo;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        private ILocalCmdProcessorStates _nmapCmdProcessorStates;
        private ILocalCmdProcessorStates _metaCmdProcessorStates;
        public BackgroundService(ILogger logger, NetConnectConfig netConfig, ILoggerFactory loggerFactory, IRabbitRepo rabbitRepo, IFileRepo fileRepo, LocalProcessorStates processorStates, IMonitorPingInfoView monitorPingInfoView, ILocalCmdProcessorStates nmapCmdProcessorStates,ILocalCmdProcessorStates metaCmdProcessorStates )
        {
            _logger = logger;
            _netConfig = netConfig;
            _loggerFactory = loggerFactory;
            _rabbitRepo = rabbitRepo;
            _fileRepo = fileRepo;
            _monitorPingInfoView = monitorPingInfoView;
            _processorStates = processorStates;
            _nmapCmdProcessorStates = nmapCmdProcessorStates;
            _metaCmdProcessorStates=metaCmdProcessorStates;
        }
        public async Task<ResultObj> Start()
        {
            var result = new ResultObj();
            result.Message = " Background Service : Start : ";
            try
            {
                result = await _rabbitRepo.ConnectAndSetUp();
                if (!result.Success) return result;
                if (!_netConfig.OqsProviderPath.Contains(FileSystem.AppDataDirectory))
                {
                    string[] pathComponents = _netConfig.OqsProviderPath.Trim('/').Split('/');

                    string fullPath = FileSystem.AppDataDirectory;
                    foreach (var component in pathComponents)
                    {
                        fullPath = Path.Combine(fullPath, component);
                    }
                    _netConfig.OqsProviderPath = fullPath;
                    System.Diagnostics.Debug.WriteLine($"Setting OqsProviderPath : {_netConfig.OqsProviderPath}");
                }
                var _connectFactory = new NetworkMonitor.Connection.ConnectFactory(_loggerFactory.CreateLogger<ConnectFactory>(), isLoadAlogTable: true, oqsProviderPath: _netConfig.OqsProviderPath);
                _nmapCmdProcessorStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _nmapCmdProcessorStates.CmdName);
                _nmapCmdProcessorStates.UseDefaultEndpointType = _netConfig.UseDefaultEndpointType;
                _nmapCmdProcessorStates.DefaultEndpointType = _netConfig.DefaultEndpointType;
                _nmapCmdProcessorStates.EndpointTypes = _netConfig.EndpointTypes;
                _nmapCmdProcessor = new NmapCmdProcessor(_loggerFactory.CreateLogger<NmapCmdProcessor>(), _nmapCmdProcessorStates, _rabbitRepo, _netConfig);
                _metaCmdProcessorStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _metaCmdProcessorStates.CmdName);
                _metaCmdProcessorStates.UseDefaultEndpointType = _netConfig.UseDefaultEndpointType;
                _metaCmdProcessorStates.DefaultEndpointType = _netConfig.DefaultEndpointType;
                _metaCmdProcessorStates.EndpointTypes = _netConfig.EndpointTypes;
                _metaCmdProcessor = new MetaCmdProcessor(_loggerFactory.CreateLogger<NmapCmdProcessor>(), _metaCmdProcessorStates, _rabbitRepo, _netConfig);
              
                _monitorPingProcessor = new MonitorPingProcessor(_loggerFactory.CreateLogger<MonitorPingProcessor>(), _netConfig, _connectFactory, _fileRepo, _rabbitRepo, _processorStates, _monitorPingInfoView);
                _rabbitListener = new RabbitListener(_monitorPingProcessor, _loggerFactory.CreateLogger<RabbitListener>(), _netConfig, _processorStates, _nmapCmdProcessor, _metaCmdProcessor);
                var resultListener = await _rabbitListener.SetupListener();
                var resultProcessor = await _monitorPingProcessor.Init(new ProcessorInitObj());
                result.Message += resultListener.Message + resultProcessor.Message;
                result.Success = resultProcessor.Success && resultListener.Success;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message += $" Error : Enabled to start background service . Error was : {e.Message}";
                _logger.LogError(result.Message);
            }

            return result;

        }
        public async Task<ResultObj> Stop()
        {
            var result = new ResultObj();
            result.Message = " Background Service : Stop : ";
            result.Success = true;
            try
            {
                _logger.LogInformation("Shutting down RabbitListener.");
                await _rabbitListener.Shutdown();
                result.Message += " Success : Shutdown RabbitListener.";
            }
            catch (Exception e)
            {
                _logger.LogError("Error during shutting down RabbitListener: " + e.ToString());
                result.Success = false;
            }
            try
            {
                _logger.LogInformation("Shutting down MonitorPingProcessor.");
                if (_monitorPingProcessor != null)
                {
                    await _monitorPingProcessor.OnStoppingAsync();
                }
                result.Message += " Success : Shutdown MonitorPingProcessor.";
            }
            catch (Exception e)
            {
                _logger.LogError("Error during shutting down MonitorPingProcessor: " + e.ToString());
                result.Success = false;
            }
            try
            {
                _logger.LogInformation("Shutting down RabbitRepo.");
                await _rabbitRepo.ShutdownRepo();
                result.Message += " Success : Shutdown RabbitRepo.";
            }
            catch (Exception e)
            {
                _logger.LogError("Error during shutting down RabbitRepo: " + e.ToString());
                result.Success = false;
            }

            return result;
        }
    }
}
