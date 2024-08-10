using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using NetworkMonitor.Utils;
namespace QuantumSecure.ViewModels
{
    public class ScanProcessorStatesViewModel : BasePopupViewModel
    {
        private LocalScanProcessorStates _scanProcessorStates;
        // public ICommand ShowPopupCommand { get; private set; }
        private ILogger _logger;
        public ObservableCollection<string> EndpointTypes { get; set; }
        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces =>
           new ObservableCollection<NetworkInterfaceInfo>(_scanProcessorStates.AvailableNetworkInterfaces);

        public NetworkInterfaceInfo SelectedNetworkInterface
        {
            get => _scanProcessorStates.SelectedNetworkInterface;
            set
            {
                _scanProcessorStates.SelectedNetworkInterface = value;
                OnPropertyChanged();
            }
        }


        public ScanProcessorStatesViewModel(ILogger logger, LocalScanProcessorStates scanProcessorStates)
        {
            try
            {
                _logger = logger; _scanProcessorStates = scanProcessorStates;
                _scanProcessorStates.UpdateMessage+= UpdatePopupMessage;
                EndpointTypes = new ObservableCollection<string>(_scanProcessorStates.EndpointTypes);
                LoadNetworkInterfaces();

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing ScanProcessorStatesViewModel: {ex}");
            }


        }
       
        public List<MonitorIP> SelectedDevices => _scanProcessorStates.SelectedDevices.ToList();
        public void LoadNetworkInterfaces()
        {
            _scanProcessorStates.AvailableNetworkInterfaces = NetworkUtils.GetSuitableNetworkInterfaces(_logger,_scanProcessorStates);
            _scanProcessorStates.SelectedNetworkInterface = _scanProcessorStates.AvailableNetworkInterfaces.FirstOrDefault();
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

        private void UpdatePopupMessage()
        {
            PopupMessage = $"{_scanProcessorStates.RunningMessage}\n{_scanProcessorStates.CompletedMessage}";

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

        public async Task AddSelectedHosts(List<MonitorIP> selectedServices)
        {
            _scanProcessorStates.SelectedDevices.Clear();
            foreach (var service in selectedServices)
            {
                _scanProcessorStates.SelectedDevices.Add(service);
            }
            //IsPopupVisible = false;
        }
    }

}