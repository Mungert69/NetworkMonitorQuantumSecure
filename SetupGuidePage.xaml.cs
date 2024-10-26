using System;
using Microsoft.Maui.Controls;

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
            await Browser.OpenAsync("http://freenetworkmonitor.click/download");
        }
    }
}
