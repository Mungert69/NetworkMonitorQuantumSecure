using MetroLog.MicrosoftExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Processor.Services;
using QuantumSecure.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using QuantumSecure.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Maui.Hosting;
using System.Diagnostics;


namespace QuantumSecure
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {

            var os = "linux";
#if ANDROID
			os="android";
#endif

#if WINDOWS
            os="windows";
#endif

            var builder = MauiApp.CreateBuilder();

            // Add Maui Loggers
            try
            {
                builder.Logging.AddTraceLogger(options =>
                {
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
                });
                builder.Logging.AddInMemoryLogger(options =>
                {
                    options.MaxLines = 1024;
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
                });
                builder.Logging.AddStreamingFileLogger(options =>
                {
                    options.RetainDays = 2;
                    options.FolderPath = Path.Combine(
                        FileSystem.CacheDirectory,
                        "MetroLogs");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error : could not setup logging . Error was : {ex}");
            }
            // Define the paths for the local and packaged appsettings.json
            try
            {
                string localAppSettingsPath = Path.Combine(FileSystem.AppDataDirectory, $"appsettings.json");
                //string packagedAppSettingsPath = "QuantumSecure.appsettings.json";
                IConfigurationRoot config;
                // Check if a local copy of appsettings.json exists
                if (File.Exists(localAppSettingsPath))
                {
                    // Use the local copy
                    config = new ConfigurationBuilder()
                        .AddJsonFile(localAppSettingsPath, optional: false, reloadOnChange: false)
                        .Build();
                }
                else
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync($"appsettings.json").Result;
                    config = new ConfigurationBuilder().AddJsonStream(stream).Build();
                }
                builder.Configuration.AddConfiguration(config);
                Task.Run(() => CopyAssetsToLocalStorage($"{config["OpensslVersion"]}-{os}")).Wait();

                ListCopiedFiles();


            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error could not load appsetting.json . Error was : {ex.Message}");
            }
            builder.Services.AddSingleton(provider =>
           {
               return new LocalProcessorStates();
           });
            builder.Services.AddSingleton(provider =>
           {
               return new LocalScanProcessorStates();
           });
            builder.Services.AddSingleton<IApiService,>(provider =>
          {
              var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
              var configuration = provider.GetRequiredService<IConfiguration>();
              return new ApiService(loggerFactory, configuration);
          });
           builder.Services.AddSingleton(provider =>
            {
                 var apiService = provider.GetRequiredService<IApiService>();
                return new NetworkMonitorViewModel(apiService);
            });


            builder.Services.AddSingleton<IMonitorPingInfoView>(provider =>
            {
                return new MonitorPingInfoView();
            });

            builder.Services.AddSingleton<IFileRepo>(provider =>
            {

                try
                {

                    bool isRunningOnMauiAndroid = true;
                    string prefixPath = FileSystem.AppDataDirectory;
                    var fileRepo = new FileRepo(isRunningOnMauiAndroid, prefixPath);
                    return fileRepo;

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error : initializing FileRepo . Error was : {ex}");
                    return new FileRepo();

                }

            });
            builder.Services.AddSingleton<NetConnectConfig>(provider =>
            {
                // Assuming Configuration is properly set up
                var configuration = provider.GetRequiredService<IConfiguration>();
                return new NetConnectConfig(configuration);
            });
            builder.Services.AddSingleton<IRabbitRepo>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RabbitRepo>>();
                var netConfig = provider.GetRequiredService<NetConnectConfig>();
                // Choose the appropriate constructor
                return new RabbitRepo(logger, netConfig);
            });
            builder.Services.AddSingleton<IAuthService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<AuthService>>();
                var netConfig = provider.GetRequiredService<NetConnectConfig>();
                var rabbitRepo = provider.GetRequiredService<IRabbitRepo>();
                var processorStates = provider.GetRequiredService<LocalProcessorStates>();
                return new AuthService(logger, netConfig, rabbitRepo, processorStates);
            });
            builder.Services.AddSingleton<IDialogService>(provider => { return new DialogService(); });
            builder.Services.AddSingleton<IPlatformService>(provider =>
            {
#if ANDROID
				   var logger = provider.GetRequiredService<ILogger<AndroidPlatformService>>();
				   var dialogService = provider.GetRequiredService<IDialogService>();
				   return new AndroidPlatformService(dialogService, logger);
#endif

#if WINDOWS
                var logger = provider.GetRequiredService<ILogger<WindowsPlatformService>>();
                var dialogService = provider.GetRequiredService<IDialogService>();
                var backgroundService = provider.GetRequiredService<IBackgroundService>();
                return new WindowsPlatformService(backgroundService, dialogService, logger);
#endif
                // throw new NotImplementedException("Unsupported platform");
            });
#if WINDOWS
            builder.Services.AddSingleton<IBackgroundService>
                (provider =>
                {

                    var logger = provider.GetRequiredService<ILogger<BackgroundService>>();
                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var netConfig = provider.GetRequiredService<NetConnectConfig>();
                    var rabbitRepo = provider.GetRequiredService<IRabbitRepo>();
                    var fileRepo = provider.GetRequiredService<IFileRepo>();
                    var processorStates = provider.GetRequiredService<LocalProcessorStates>();
                    var scanProcessorStates=provider.GetRequiredService<LocalScanProcessorStates>();
                    var monitorPingInfoView = provider.GetRequiredService<IMonitorPingInfoView>();

                    return new BackgroundService(logger, netConfig, loggerFactory, rabbitRepo, fileRepo, processorStates, monitorPingInfoView, scanProcessorStates);


                });
#endif
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ProcessorStatesViewModel>>();
                var processorStates = provider.GetRequiredService<LocalProcessorStates>();
                // Choose the appropriate constructor
                return new ProcessorStatesViewModel(logger, processorStates);
            });
            builder.Services.AddSingleton(provider =>
           {
               var logger = provider.GetRequiredService<ILogger<ScanProcessorStatesViewModel>>();
               var scanProcessorStates = provider.GetRequiredService<LocalScanProcessorStates>();
               // Choose the appropriate constructor
               return new ScanProcessorStatesViewModel(logger, scanProcessorStates);
           });

            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<AppShell>>();
                var platformService = provider.GetRequiredService<IPlatformService>();
                return new AppShell(logger, platformService);
            });

            builder.Services.AddSingleton(provider =>
            {
                var authService = provider.GetRequiredService<IAuthService>();
                var netConfig = provider.GetRequiredService<NetConnectConfig>();
                var logger = provider.GetRequiredService<ILogger<MainPage>>();
                var platformService = provider.GetRequiredService<IPlatformService>();
                var processorStatesViewModel = provider.GetRequiredService<ProcessorStatesViewModel>();
                return new MainPage(authService, netConfig, logger, platformService, processorStatesViewModel);
            });

            builder.Services.AddSingleton(provider =>
            {
                var netConfig = provider.GetRequiredService<NetConnectConfig>();
                var logger = provider.GetRequiredService<ILogger<MainPage>>();
                return new ConfigPageViewModel(logger, netConfig);
            });
            builder.Services.AddSingleton(provider =>
            {
                var scanProcessorStatesViewModel = provider.GetRequiredService<ScanProcessorStatesViewModel>();
                var logger = provider.GetRequiredService<ILogger<ScanPage>>();
                return new ScanPage(logger, scanProcessorStatesViewModel);
            });
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<DateViewPage>();
            var app = builder.Build();
            ServiceProvider = app.Services;
            return app;
        }


        private static async Task CopyAssetsToLocalStorage(string directoryName)
        {
            try
            {
                Console.WriteLine($"Starting asset copy from : {directoryName}");

                string[] assetFiles = await ListAssetFiles(directoryName);

                string localPath = Path.Combine(FileSystem.AppDataDirectory, "openssl");
                Directory.CreateDirectory(localPath);

                foreach (var assetFile in assetFiles)
                {
                    string assetFilePath = Path.Combine(directoryName, assetFile);
                    using (var stream = await FileSystem.OpenAppPackageFileAsync(assetFilePath))
                    {
                        if (stream == null)
                        {
                            Console.WriteLine($"Failed to open asset file: {assetFilePath}");
                            continue;
                        }

                        string localFilePath = Path.Combine(localPath, assetFile);
                        string localFileDirectory = Path.GetDirectoryName(localFilePath);

                        if (!Directory.Exists(localFileDirectory))
                            Directory.CreateDirectory(localFileDirectory);

                        using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                        if (IsOpenSSLBinary(assetFile)) SetExecutablePermission(localFilePath);
                    }
                }

                Console.WriteLine($"Directory copied to: {localPath}");
                SetLDLibraryPath(Path.Combine(localPath, "lib64"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying assets: {ex.Message}");
            }
        }
        private static bool IsOpenSSLBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/openssl", StringComparison.OrdinalIgnoreCase);
        }
        private static void SetExecutablePermission(string filePath)
        {
            try
            {
                Process.Start("chmod", $"+x {filePath}").WaitForExit();
                Console.WriteLine($"Set executable permission for: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set executable permission for {filePath}: {ex.Message}");
            }
        }
        private static void SetLDLibraryPath(string libraryPath)
        {
            try
            {
                string ldLibraryPath = $"LD_LIBRARY_PATH={libraryPath}";
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", libraryPath);
                Console.WriteLine($"Set LD_LIBRARY_PATH to: {libraryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set LD_LIBRARY_PATH: {ex.Message}");
            }
        }

        private static async Task<string[]> ListAssetFiles(string directoryName)
        {
            try
            {
                string manifestFileName = Path.Combine(directoryName, "assets_manifest.txt");
                using (var stream = await FileSystem.OpenAppPackageFileAsync(manifestFileName))
                using (var reader = new StreamReader(stream))
                {
                    var fileList = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // Trim any leading ./ from the assetFilePath
                            line = line.TrimStart('.', '/');
                            fileList.Add(line);
                        }
                    }

                    return fileList.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading asset manifest: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static void ListCopiedFiles()
        {
            try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, "openssl");
                string[] files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    Console.WriteLine($"File in local storage: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing copied files: {ex.Message}");
            }
        }

    }
}
