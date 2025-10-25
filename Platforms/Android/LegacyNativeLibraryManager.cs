using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.OS;
using Android.Util;
using Microsoft.Maui.Storage;
using QuantumSecure.Platforms.Android;

namespace QuantumSecure
{
    internal static class LegacyNativeLibraryManager
    {
        private static readonly string[] RequiredLibraries =
        {
            "libprocwrapper.so",
            "libopenssl_exec.so",
            "libnmap_exec.so",
            "libssl.so",
            "libcrypto.so",
            "libc++_shared.so",
            "liboqsprovider.so",
            "libopenssl_lua.so"
        };

        private static string? _overrideDirectory;

        public static void Initialize(string? sourceDirectory)
        {
            _overrideDirectory = null;

            if (Build.VERSION.SdkInt > BuildVersionCodes.M)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                Log.Warn("LegacyNativeLibMgr", $"Source directory invalid: {sourceDirectory ?? "(null)"}");
                return;
            }

            try
            {
                string destinationDirectory = Path.Combine(FileSystem.AppDataDirectory, "openssl", "bin");
                Directory.CreateDirectory(destinationDirectory);

                foreach (var fileName in RequiredLibraries.Distinct(StringComparer.Ordinal))
                {
                    string sourcePath = Path.Combine(sourceDirectory, fileName);
                    if (!File.Exists(sourcePath))
                    {
                        Log.Warn("LegacyNativeLibMgr", $"Missing source library: {sourcePath}");
                        continue;
                    }

                    string destPath = Path.Combine(destinationDirectory, fileName);
                    if (!NeedsCopy(sourcePath, destPath))
                    {
                        continue;
                    }

                    File.Copy(sourcePath, destPath, overwrite: true);
                    PermissionsHelper.MakeFileExecutable(destPath);
                    Log.Debug("LegacyNativeLibMgr", $"Copied {fileName} to {destPath}");
                }

                _overrideDirectory = destinationDirectory;
            }
            catch (Exception ex)
            {
                Log.Warn("LegacyNativeLibMgr", $"Failed to copy native libraries: {ex}");
            }
        }

        public static string GetPreferredDirectory(string? fallbackDirectory)
        {
            if (!string.IsNullOrWhiteSpace(_overrideDirectory))
            {
                return _overrideDirectory;
            }

            return fallbackDirectory ?? string.Empty;
        }

        private static bool NeedsCopy(string sourcePath, string destPath)
        {
            if (!File.Exists(destPath))
            {
                return true;
            }

            try
            {
                var sourceInfo = new FileInfo(sourcePath);
                var destInfo = new FileInfo(destPath);

                return sourceInfo.Length != destInfo.Length ||
                       sourceInfo.LastWriteTimeUtc > destInfo.LastWriteTimeUtc;
            }
            catch
            {
                return true;
            }
        }
    }
}
