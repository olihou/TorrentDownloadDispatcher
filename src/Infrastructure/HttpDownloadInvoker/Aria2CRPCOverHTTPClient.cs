using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Configurations.HttpDownloadInvoker;
using ApplicationCore.Messages.Notification;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using ApplicationCore.Contracts;

namespace Infrastructure.HttpDownloadInvoker
{
    public class Aria2CRPCOverHTTPClient : IHttpDownloaderClient
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
                    { "id", Guid.NewGuid() },
                    { "jsonrpc", "2.0" },
                    { "method", "aria2.addUri" }
                };

                foreach (var download in notification.FilesToDownload)
                {
                    root["params"] = new JArray
                    {
                        new JArray(download.ToString())
                    };

                    var json = JsonConvert.SerializeObject(root);
                    using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                    using var message = new HttpRequestMessage(HttpMethod.Post, "jsonrpc") { Content = stringContent };
                    //using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    //response.EnsureSuccessStatusCode();
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
    }
}
