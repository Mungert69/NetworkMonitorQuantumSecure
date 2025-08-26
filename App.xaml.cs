using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.Utils;
﻿using MetroLog.Maui;
namespace QuantumSecure;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        try
        {
            InitializeComponent();
            MainPage = serviceProvider.GetRequiredService<AppShell>();
            LogController.InitializeNavigation(page => MainPage!.Navigation.PushModalAsync(page),() => MainPage!.Navigation.PopModalAsync());

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing App: {ex.Message}");
        }
    }
   
}
