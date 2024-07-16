using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
namespace QuantumSecure.ViewModels
{
    public class ScanProcessorStatesViewModel : BasePopupViewModel
    {
        private LocalScanProcessorStates _scanProcessorStates;
        // public ICommand ShowPopupCommand { get; private set; }
        private ILogger _logger;
        private string _popupMessageType = "";
        public ObservableCollection<string> EndpointTypes { get; set; }


        public ScanProcessorStatesViewModel(ILogger logger, LocalScanProcessorStates scanProcessorStates)
        {
            try
            {
                _logger = logger; _scanProcessorStates = scanProcessorStates;
                _scanProcessorStates.PropertyChanged += OnScanProcessorStatesChanged;
                EndpointTypes = new ObservableCollection<string>(_scanProcessorStates.EndpointTypes);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing ScanProcessorStatesViewModel: {ex}");
            }


        }

        public bool IsRunning => _scanProcessorStates.IsRunning;
        public bool IsSuccess => _scanProcessorStates.IsSuccess;
        public string CompletedMessage => _scanProcessorStates.CompletedMessage;
        public string RunningMessage => _scanProcessorStates.RunningMessage;

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
        private void OnScanProcessorStatesChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);

            if (IsPopupVisible)
            {
                // Update PopupMessage based on the changed property
                UpdatePopupMessage(e.PropertyName);
            }
        }

        private void UpdatePopupMessage(string propertyName)
        {
            PopupMessage = $"{RunningMessage}\n{CompletedMessage}";
            // Logic to update PopupMessage based on propertyName
            /* switch (propertyName)
             {
                 case nameof(RunningMessage):
                     if (IsRunning) PopupMessage = $"Scan Running Message: {RunningMessage}";
                     break;
                 case nameof(CompletedMessage):
                     if (!IsRunning) PopupMessage = $"Scan Completed Message: {RunningMessage}\n{CompletedMessage}";

                     break;
             }*/
        }

    }

}