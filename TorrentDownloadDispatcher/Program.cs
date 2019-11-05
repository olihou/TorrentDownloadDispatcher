using System;
using System.Reflection;
using System.Threading.Tasks;
using ApplicationCore.Configurations;
using ApplicationCore.Configurations.HttpDownloadInvoker;
using ApplicationCore.Configurations.TorrentHttpConverter;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;
using ApplicationCore.Services;
using Infrastructure.HttpDownloadInvoker;
using Infrastructure.TorrentHttpConverter.RealDebrid;
using Infrastructure.TorrentWatcher;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TorrentDownloadDispatcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
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
                    services.AddOptions();
                    services.Configure<TorrentConfiguration>(hostContext.Configuration.GetSection("Torrent"));

                    var fileSystemConfigKey = "Providers:FileSystem";
                    if (hostContext.Configuration.GetSection(fileSystemConfigKey) != null)
                    {
                        services.Configure<FileSystemWatcherConfiguration>(hostContext.Configuration.GetSection(fileSystemConfigKey));
                        services.AddSingleton<ITorrentWatcher, TorrentFileSystemWatcher>();
                        services.AddSingleton<INotificationHandler<NewTorrent>, NewTorrentHandler>();
                    }
                    var realDebridConfigKey = "Providers:RealDebrid";
                    if (hostContext.Configuration.GetSection(realDebridConfigKey) != null)
                    {
                        services.Configure<RealDebridConfiguration>(hostContext.Configuration.GetSection(realDebridConfigKey));
                        services.AddSingleton<IRequestHandler<TorrentHttpDownloadConverter, TorrentConvertedToHttpFile>, RealDebridClient>();
                    }
                    var aria2cConfigKey = "Providers:Aria2cHttp";
                    if (hostContext.Configuration.GetSection(aria2cConfigKey) != null)
                    {
                        services.AddSingleton<INotificationHandler<InvokeDownload>, Aria2CRPCOverHTTPClient>();
                        services.Configure<Aria2CConfiguration>(hostContext.Configuration.GetSection(aria2cConfigKey));
                    }

                    services.AddSingleton<IRequestHandler<SelectFileOfTorrent, SelectedFileOfTorrent>, TorrentFilesSelectorHandler>();
                    
                    services.AddHostedService<Worker>();
                });
    }
}
