using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.Maui;
using QuantumSecure.Platforms.Android;

namespace QuantumSecure
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            var nativeDir = ApplicationInfo?.NativeLibraryDir;
            LegacyNativeLibraryManager.Initialize(nativeDir);

            var preferredDir = LegacyNativeLibraryManager.GetPreferredDirectory(nativeDir);
            if (string.IsNullOrWhiteSpace(preferredDir))
            {
                return;
            }

            try
            {
                var current = System.Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
                var segments = current
                    .Split(':', StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet(StringComparer.Ordinal);

                if (segments.Add(preferredDir))
                {
                    var merged = segments.Count == 1
                        ? preferredDir
                        : string.Join(":", segments.Prepend(preferredDir));
                    System.Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", merged);
                    Log.Debug("QuantumSecure", $"Primed LD_LIBRARY_PATH with {preferredDir}");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("QuantumSecure", $"Unable to prime LD_LIBRARY_PATH: {ex}");
            }

            try
            {
                System.Environment.SetEnvironmentVariable("OPENSSL_MODULES", preferredDir);
            }
            catch (Exception ex)
            {
                Log.Warn("QuantumSecure", $"Unable to set OPENSSL_MODULES: {ex}");
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
