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
        private IRabbitRepo _rabbitRepo;
        private IRabbitListener _rabbitListener;
        private IFileRepo _fileRepo;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        private ICmdProcessorProvider _cmdProcessorProvider;
        public BackgroundService(ILogger logger, NetConnectConfig netConfig, ILoggerFactory loggerFactory, IRabbitRepo rabbitRepo, IFileRepo fileRepo, LocalProcessorStates processorStates, IMonitorPingInfoView monitorPingInfoView, ICmdProcessorProvider cmdProcessorProvider)
        {
            _logger = logger;
            _netConfig = netConfig;
            _loggerFactory = loggerFactory;
            _rabbitRepo = rabbitRepo;
            _fileRepo = fileRepo;
            _monitorPingInfoView = monitorPingInfoView;
            _processorStates = processorStates;
            _cmdProcessorProvider = cmdProcessorProvider;

        }
        public async Task<ResultObj> Start()
        {
            var result = new ResultObj();
            result.Message = " Background Service : Start : ";
            try
            {
                result = await _rabbitRepo.ConnectAndSetUp();
                if (!result.Success) return result;
                
                var _connectFactory = new NetworkMonitor.Connection.ConnectFactory(_loggerFactory.CreateLogger<ConnectFactory>(), netConfig: _netConfig);
                _ = _connectFactory.SetupChromium(_netConfig);
                _cmdProcessorProvider.NmapStates.UseDefaultEndpointType = _netConfig.UseDefaultEndpointType;
                _cmdProcessorProvider.NmapStates.DefaultEndpointType = _netConfig.DefaultEndpointType;
                _cmdProcessorProvider.NmapStates.EndpointTypes = _netConfig.EndpointTypes;
                _cmdProcessorProvider.NmapStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.NmapStates.CmdName);
                if (_cmdProcessorProvider.NmapStates.IsCmdAvailable) _logger.LogInformation(" Success : Nmap command is available.");

                _cmdProcessorProvider.MetasploitStates.UseDefaultEndpointType = _netConfig.UseDefaultEndpointType;
                _cmdProcessorProvider.MetasploitStates.DefaultEndpointType = _netConfig.DefaultEndpointType;
                _cmdProcessorProvider.MetasploitStates.EndpointTypes = _netConfig.EndpointTypes;
                _cmdProcessorProvider.MetasploitStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.MetasploitStates.CmdName);
                if (_cmdProcessorProvider.MetasploitStates.IsCmdAvailable) _logger.LogInformation(" Success : Metasploit command is available.");

                _cmdProcessorProvider.OpensslStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.OpensslStates.CmdName);
                if (_cmdProcessorProvider.OpensslStates.IsCmdAvailable) _logger.LogInformation(" Success : Openssl command is available.");
                
                _cmdProcessorProvider.BusyboxStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.BusyboxStates.CmdName);
                if (_cmdProcessorProvider.BusyboxStates.IsCmdAvailable) _logger.LogInformation(" Success : Busybox command is available.");
                
                _cmdProcessorProvider.SearchWebStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.SearchWebStates.CmdName);
                if (_cmdProcessorProvider.SearchWebStates.IsCmdAvailable) _logger.LogInformation(" Success : SearchWeb command is available.");
                
                _cmdProcessorProvider.CrawlPageStates.IsCmdAvailable = !_netConfig.DisabledCommands.Any(a => a == _cmdProcessorProvider.CrawlPageStates.CmdName);
                if (_cmdProcessorProvider.CrawlPageStates.IsCmdAvailable) _logger.LogInformation(" Success : CrawlPage command is available.");
                

                _monitorPingProcessor = new MonitorPingProcessor(_loggerFactory.CreateLogger<MonitorPingProcessor>(), _netConfig, _connectFactory, _fileRepo, _rabbitRepo, _processorStates, _monitorPingInfoView);
                _rabbitListener = new RabbitListener(_monitorPingProcessor, _loggerFactory.CreateLogger<RabbitListener>(), _netConfig, _processorStates, _cmdProcessorProvider);
                var resultListener = await _rabbitListener.SetupListener();
                var resultProcessor = await _monitorPingProcessor.Init(new ProcessorInitObj());
                result.Message += resultListener.Message + resultProcessor.Message;
                result.Success = resultProcessor.Success && resultListener.Success;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message += $" Error : failed to start background service . Error was : {e.Message}";
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
                _rabbitRepo.ShutdownRepo();
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
