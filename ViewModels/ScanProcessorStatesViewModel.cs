using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using NetworkMonitor.Utils;
using NetworkMonitor.Api.Services;
namespace QuantumSecure.ViewModels
{
    public class ScanProcessorStatesViewModel : BasePopupViewModel
    {
        private LocalCmdProcessorStates _scanProcessorStates;
        private ILogger _logger;
        private readonly IApiService _apiService;
        public ObservableCollection<string> EndpointTypes { get; set; }
        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces =>
           new ObservableCollection<NetworkInterfaceInfo>(_scanProcessorStates.AvailableNetworkInterfaces);
        public string RunningMessage => _scanProcessorStates.RunningMessage;
        public string CompletedMessage => _scanProcessorStates.CompletedMessage;
        public NetworkInterfaceInfo SelectedNetworkInterface
        {
            get => _scanProcessorStates.SelectedNetworkInterface;
            set
            {
                _scanProcessorStates.SelectedNetworkInterface = value;
                OnPropertyChanged();
            }
        }


        public ScanProcessorStatesViewModel(ILogger logger, LocalCmdProcessorStates cmdProcessorStates, IApiService apiService)
        {
            try
            {
                _logger = logger; _scanProcessorStates = cmdProcessorStates;
                _apiService = apiService;
                _scanProcessorStates.PropertyChanged += OnProcessorStatesChanged;
                EndpointTypes = new ObservableCollection<string>(_scanProcessorStates.EndpointTypes);
                LoadNetworkInterfaces();

            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing ScanProcessorStatesViewModel: {ex}");
            }


        }

        public List<MonitorIP> SelectedDevices => _scanProcessorStates.SelectedDevices.ToList();
        public void LoadNetworkInterfaces()
        {
            _scanProcessorStates.AvailableNetworkInterfaces = NetworkUtils.GetSuitableNetworkInterfaces(_logger, _scanProcessorStates);
            if (_scanProcessorStates.AvailableNetworkInterfaces!=null && _scanProcessorStates.AvailableNetworkInterfaces.Count>0) _scanProcessorStates.SelectedNetworkInterface = _scanProcessorStates.AvailableNetworkInterfaces.FirstOrDefault();
            
        }
        public async Task Scan()
        {
            IsPopupVisible = true;
            await _scanProcessorStates.Scan();
        }
        public async Task Cancel()
        {
            await _scanProcessorStates.Cancel();
        }

        public string DefaultEndpointType
        {
            get => _scanProcessorStates.DefaultEndpointType;
            set
            {
                _scanProcessorStates.DefaultEndpointType = value;
                OnPropertyChanged();
            }
        }

        public bool UseDefaultEndpointType
        {
            get => _scanProcessorStates.UseDefaultEndpointType;
            set
            {
                _scanProcessorStates.UseDefaultEndpointType = value;
                OnPropertyChanged();
            }
        }
          public bool UseFastScan
        {
            get => _scanProcessorStates.UseFastScan;
            set
            {
                _scanProcessorStates.UseFastScan = value;
                OnPropertyChanged();
            }
        }
         public bool LimitPorts
        {
            get => _scanProcessorStates.LimitPorts;
            set
            {
                _scanProcessorStates.LimitPorts = value;
                OnPropertyChanged();
            }
        }

        private void OnProcessorStatesChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);

            if (IsPopupVisible)
            {
                UpdatePopupMessage(e.PropertyName);
            }
        }

        private void UpdatePopupMessage(string? propertyName)
        {
            switch (propertyName)
            {
                case nameof(RunningMessage):
                    PopupMessage = $"{RunningMessage}\n{CompletedMessage}";

                    break;
                case nameof(CompletedMessage):
                    PopupMessage = $"{RunningMessage}\n{CompletedMessage}";

                    break;
            }
        }
        public async Task<List<MonitorIP>> ScanForHosts()
        {
            // Reset any previous state
            IsPopupVisible = true;
            await _scanProcessorStates.Scan().ConfigureAwait(false);

            // Return the detected hosts
            return _scanProcessorStates.ActiveDevices.ToList();
        }

        public async Task AddServices()
        {
            await _scanProcessorStates.AddServices().ConfigureAwait(false);
        }

        public void AddSelectedHosts(List<MonitorIP> selectedServices)
        {
            _scanProcessorStates.SelectedDevices.Clear();
            foreach (var service in selectedServices)
            {
                _scanProcessorStates.SelectedDevices.Add(service);
            }
            //IsPopupVisible = false;
        }
        public async Task CheckServices()
        {
            // Convert the selected devices to a list of IConnectionObject (HostObject)
            var connectionObjects = new List<IConnectionObject>();

            foreach (var device in _scanProcessorStates.SelectedDevices)
            {
                IConnectionObject hostObject;
                if (device.EndPointType == "quantum")
                {
                    hostObject = new QuantumHostObject
                    {
                        Address = device.Address ?? "NoHostFound", // Assuming MonitorIP has a property IPAddress
                        Port = device.Port,         // Assuming MonitorIP has a property Port
                        Timeout = 10000             // Default timeout, can be customized
                    };
                }
                else
                {
                    hostObject = new HostObject
                    {
                        Address = device.Address ?? "NoHostFound", // Assuming MonitorIP has a property IPAddress
                        Port = device.Port,         // Assuming MonitorIP has a property Port
                        Timeout = 10000,             // Default timeout, can be customized
                        EndPointType = device.EndPointType ?? "icmp"
                    };
                }


                connectionObjects.Add(hostObject);
            }

            // Use the ApiService to check the connections
            var results = await _apiService.CheckConnections(connectionObjects).ConfigureAwait(false);
            _scanProcessorStates.CompletedMessage += "\n\nChecking status of selected services...\n\n";
            // Handle the results (e.g., display them or log them)
            foreach (var result in results)
            {
                string message = "No Data in results";
                if (result.Data != null)
                {
                    if (result.Success)
                    {
                        message = $"Performed a successful {result.Data.CheckPerformed} check for {result.Data.TestedAddress} on port {result.Data.TestedPort} with status {result.Data.ResultStatus}\n";
                        _logger.LogInformation(message);
                    }
                    else
                    {
                        message = $"{result.Data.CheckPerformed} check failed for {result.Data.TestedAddress} on port {result.Data.TestedPort} with status {result.Data.ResultStatus}\n";
                        _logger.LogWarning(message);
                    }
                }
                _scanProcessorStates.CompletedMessage += message;
            }

            // Update the UI or state based on results, if necessary
        }
    }

}