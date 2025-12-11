using CommunityToolkit.Maui.Views;
using System;

namespace QuantumSecure.Views
{
    // Keep Popup<bool> so ShowPopupAsync<bool> returns a typed result
    public partial class StatusDetailsPopup : Popup<bool>
    {
        public StatusDetailsPopup()
        {
            InitializeComponent();
        }

        public async void OnDetailsButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                await CloseAsync(true);
            }
            catch { }
        }

        public async void OnCloseButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                await CloseAsync(false);
            }
            catch { }
        }

        // New: allow tapping the popup card itself to act like "More details"
        public async void OnFrameTapped(object? sender, EventArgs e)
        {
            try
            {
                await CloseAsync(true);
            }
            catch { }
        }
    }
}