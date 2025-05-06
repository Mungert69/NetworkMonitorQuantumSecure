
using NetworkMonitor.Objects;
namespace QuantumSecure
{
    public partial class SetupGuidePage : ContentPage
    {
        public SetupGuidePage()
        {
            InitializeComponent();
        }

        private async void OnDownloadLinkClicked(object sender, EventArgs e)
        {
            await Browser.Default.OpenAsync($"http://{AppConstants.AppDomain}/download");
        }
    }
}
