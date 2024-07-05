using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace QuantumSecure.ViewModels
{
    public class BasePopupViewModel: INotifyPropertyChanged
    {
        public ICommand ClosePopupCommand { get; protected set; }
        private bool _isPopupVisible;
        private string _popupMessage="";

        public BasePopupViewModel()
        {
            ClosePopupCommand = new Command(() => IsPopupVisible = false);
        }

         public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set
            {
                _isPopupVisible = value;
                OnPropertyChanged();
            }
        }
         public string PopupMessage
        {
            get => _popupMessage;
            set
            {
                _popupMessage = value;
                OnPropertyChanged();
            }
        }

         public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
