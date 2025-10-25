#if WINDOWS
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using QuantumSecure;
using QuantumSecure.Views;
using Xunit;

namespace NetworkMonitorQuantumSecure.Tests;

public class XamlSmokeTests
{
#if DEBUG
    [Fact]
    public void StatusDetailsPopup_Loads()
    {
        MauiTestHarness.EnsureInitialized();

        var popup = new StatusDetailsPopup();

        Assert.NotNull(popup);
        Assert.IsType<Grid>(popup.Content);
    }

    [Fact]
    public void DataViewPage_Loads()
    {
        MauiTestHarness.EnsureInitialized();

        var viewModel = new MonitorPingInfoView
        {
            MonitorPingInfos = new List<MonitorPingInfo>
            {
                new()
                {
                    MonitorIPID = 1,
                    Address = "localhost",
                    EndPointType = "Loopback",
                    Port = 80,
                    PacketsLostPercentage = 0,
                    RoundTripTimeAverage = 10
                }
            }
        };

        var page = new DataViewPage(viewModel);

        Assert.NotNull(page);
        Assert.Equal(viewModel, page.BindingContext);
    }
#else
    [Fact(Skip = "XAML smoke tests execute only in DEBUG builds.")]
    public void DebugOnly() { }
#endif
}
#endif
