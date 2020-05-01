using System;
using System.IO;
using ApplicationCore.Configurations;
using ApplicationCore.Configurations.HttpDownloadInvoker;
using ApplicationCore.Configurations.TorrentHttpConverter;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Contract;
using ApplicationCore.Services;
using Infrastructure.HttpDownloadInvoker;
using Infrastructure.TorrentHttpConverter.RealDebrid;
using Infrastructure.TorrentWatcher;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TorrentDownloadDispatcher.Console.Display;

namespace TorrentDownloadDispatcher.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        foreach (var appAssembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            config.AddUserSecrets(appAssembly, optional: true);
                        }
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<TorrentConfiguration>(hostContext.Configuration.GetSection("Torrent"));
                    services.AddSingleton<DownloadPipelineBuilder>();
                    services.AddSingleton<IDownloadProgressTracker, DownloadProgressReport>();

                    var fileSystemConfigKey = "Providers:FileSystem";
                    if (hostContext.Configuration.GetSection(fileSystemConfigKey) != null)
                    {
                        services.Configure<FileSystemWatcherConfiguration>(hostContext.Configuration.GetSection(fileSystemConfigKey));
                        services.AddSingleton<ITorrentWatcher, TorrentFileSystemWatcher>();
                    }

                    var realDebridConfigKey = "Providers:RealDebrid";
                    if (hostContext.Configuration.GetSection(realDebridConfigKey) != null)
                    {
                        services.Configure<RealDebridConfiguration>(hostContext.Configuration.GetSection(realDebridConfigKey));
                        services.AddSingleton<ITorrentToHttpConverter, RealDebridClient>();
                    }


                    var aria2cConfigKey = "Providers:Aria2cHttp";
                    if (hostContext.Configuration.GetSection(aria2cConfigKey) != null)
                    {
                        services.AddSingleton<IHttpDownloadInvoker, Aria2CRPCOverHTTPClient>();
                        services.Configure<Aria2CConfiguration>(hostContext.Configuration.GetSection(aria2cConfigKey));
                    }

                    services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

                    services.AddHostedService<Worker>();
                })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.ClearProviders();
            });
    }
}
