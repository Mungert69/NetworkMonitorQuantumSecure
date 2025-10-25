using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.Maui;

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

            if (Build.VERSION.SdkInt <= BuildVersionCodes.M)
            {
                try
                {
                    var nativeDir = ApplicationInfo?.NativeLibraryDir;
                    if (!string.IsNullOrEmpty(nativeDir))
                    {
                        var current = System.Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
                        var segments = current
                            .Split(':', StringSplitOptions.RemoveEmptyEntries)
                            .ToHashSet(StringComparer.Ordinal);

                        if (segments.Add(nativeDir))
                        {
                            var merged = segments.Count == 1
                                ? nativeDir
                                : string.Join(":", segments.Prepend(nativeDir));
                            System.Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", merged);
                            Log.Debug("QuantumSecure", $"Primed LD_LIBRARY_PATH with {nativeDir}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("QuantumSecure", $"Unable to prime LD_LIBRARY_PATH: {ex}");
                }
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
