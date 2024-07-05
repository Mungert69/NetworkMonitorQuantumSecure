using MetroLog.Maui;
using NetworkMonitor.Processor.Services;
using QuantumSecure.Services;
using QuantumSecure.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Microsoft.Maui.Graphics;  // For Rectangle
using Microsoft.Maui.Layouts;  // For AbsoluteLayoutFlags

namespace QuantumSecure;
public partial class ConfigPage : ContentPage
{
    public ConfigPage(ConfigPageViewModel configPageViewModel)
    {
        InitializeComponent();
        BindingContext = configPageViewModel;
    }
}

