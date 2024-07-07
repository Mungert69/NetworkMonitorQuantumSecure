using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Objects;

namespace QuantumSecure.ViewModels
{
    public class NetworkMonitorViewModel : INotifyPropertyChanged
    {
        private readonly IApiService _apiService;
        private string _selectedEndpointType;
        private string _address;
        private int _port;
        private bool _isBusy;
        private bool _hasResult;
        private string _resultMessage;
        private string _responseTime;
        private string _resultStatus;

        public ObservableCollection<string> EndpointTypes { get; } = new ObservableCollection<string> { "Http", "Smtp", "Dns", "Icmp", "Quantum" };

        public string SelectedEndpointType
        {
            get => _selectedEndpointType;
            set { _selectedEndpointType = value; OnPropertyChanged(); }
        }

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasResult
        {
            get => _hasResult;
            set { _hasResult = value; OnPropertyChanged(); }
        }

        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        public string ResponseTime
        {
            get => _responseTime;
            set { _responseTime = value; OnPropertyChanged(); }
        }

        public string ResultStatus
        {
            get => _resultStatus;
            set { _resultStatus = value; OnPropertyChanged(); }
        }

        public ICommand TestConnectionCommand { get; }

        public NetworkMonitorViewModel(IApiService apiService)
        {
            _apiService = apiService;
            TestConnectionCommand = new Command(async () => await TestConnection());
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(SelectedEndpointType))
            {
                ResultMessage = "Please enter an address and select an endpoint type.";
                HasResult = true;
                return;
            }

            IsBusy = true;
            HasResult = false;

            try
            {
                TResultObj<DataObj> result;
                var hostObject = new HostObject { Address = Address, Port = Port };

                switch (SelectedEndpointType.ToLower())
                {
                    case "http":
                        result = await _apiService.CheckHttp(hostObject);
                        break;
                    case "smtp":
                        result = await _apiService.CheckSmtp(hostObject);
                        break;
                    case "dns":
                        result = await _apiService.CheckDns(hostObject);
                        break;
                    case "icmp":
                        result = await _apiService.CheckIcmp(hostObject);
                        break;
                    case "quantum":
                        var quantumResult = await _apiService.CheckQuantum(new UrlObject { Url = Address, Port = Port });
                        result = new TResultObj<DataObj>
                        {
                            Success = quantumResult.Success,
                            Message = quantumResult.Message,
                            Data = new DataObj
                            {
                                ResponseTime = quantumResult.Data.ResponseTime,
                                ResultStatus = quantumResult.Data.ResultStatus
                            }
                        };
                        break;
                    default:
                        ResultMessage = "Invalid endpoint type selected.";
                        HasResult = true;
                        return;
                }

                ResultMessage = result.Success ? "Connection successful" : "Connection failed";
                ResponseTime = result.Data.ResponseTime.ToString();
                ResultStatus = result.Data.ResultStatus;
                HasResult = true;
            }
            catch (Exception ex)
            {
                ResultMessage = $"An error occurred: {ex.Message}";
                HasResult = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}