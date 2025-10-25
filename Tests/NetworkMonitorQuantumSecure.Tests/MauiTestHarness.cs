#if WINDOWS
using System;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using NetworkMonitor.Maui.Utils;

namespace NetworkMonitorQuantumSecure.Tests;

internal static class MauiTestHarness
{
#if DEBUG
    private static bool _initialized;
    private static readonly object _syncRoot = new();

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_initialized)
            {
                return;
            }

            if (Application.Current is null)
            {
                _ = new TestApplication();
            }

            _initialized = true;
        }
    }

    private sealed class TestApplication : Application
    {
        public TestApplication()
        {
            Resources.MergedDictionaries.Add(LoadDictionary("Resources/Styles/Colors.xaml"));
            Resources.MergedDictionaries.Add(LoadDictionary("Resources/Styles/Styles.xaml"));
            Resources.Add("BoundsConverter", new BoundsConverter());
            Resources.Add("BoolToColorConverter", new BoolToColorConverter());
            Resources.Add("ToggledEventArgsConverter", new ToggledEventArgsConverter());
            Resources.Add("MinTapSizeConverter", new MinTapSizeConverter());
        }

        private static ResourceDictionary LoadDictionary(string relativePath)
        {
            var dictionary = new ResourceDictionary();
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Missing resource dictionary for tests: {fullPath}");
            }

            using var reader = File.OpenText(fullPath);
            var xaml = reader.ReadToEnd();
            XamlLoader.Load(dictionary, xaml);
            return dictionary;
        }
    }
#else
    public static void EnsureInitialized()
    {
        // No-op outside DEBUG builds.
    }
#endif
}
#endif
