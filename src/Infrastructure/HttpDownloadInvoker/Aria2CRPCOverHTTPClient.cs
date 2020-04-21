using System;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Configurations.HttpDownloadInvoker;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Infrastructure.HttpDownloadInvoker
{
    public class Aria2CRPCOverHTTPClient : IHttpDownloadInvoker
    {
        private readonly Aria2CConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public Aria2CRPCOverHTTPClient(
            IOptions<Aria2CConfiguration> config,
            ILogger<Aria2CRPCOverHTTPClient> logger)
        {
            _config = config.Value;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = _config.Uri
            };
        }

        public async Task Handle(InvokeDownload notification, CancellationToken cancellationToken)
        {
            try
            {
                JObject root = new JObject
                {
                    { "jsonrpc", "2.0" },
                    { "id", Guid.NewGuid() },
                    { "method", "aria2.addUri" }
                };

                foreach (var download in notification.FilesToDownload)
                {
                    var url = new JArray();
                    url.Add(new JArray(download.ToString()));
                    root["params"] = url;

                    var json = JsonConvert.SerializeObject(root);
                    using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                    using var message = new HttpRequestMessage(HttpMethod.Post, "jsonrpc") { Content = stringContent };
                    using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch(HttpRequestException reqEx)
            {
                _logger.LogError(reqEx, "Download server return unsuccessfull status code");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download server is not available");
                throw;
            }
        }

        IObserver<InvokeDownload> IHttpDownloadInvoker.Handler => Observer.Create<InvokeDownload>(OnNext, OnError, OnCompleted);
        

        public void OnCompleted()
        {
            _httpClient.Dispose();
        }

        public void OnError(Exception error)
        {
            _logger.LogError(error, "unknown error occured");
        }

        public void OnNext(InvokeDownload value)
        {
            Handle(value, default).GetAwaiter().OnCompleted(() => _logger.LogDebug("Download invoked"));
        }
    }
}
