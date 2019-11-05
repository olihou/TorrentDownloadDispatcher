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

namespace Infrastructure.HttpDownloadInvoker
{
    public class Aria2CRPCOverHTTPClient : INotificationHandler<InvokeDownload>
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
            JObject root = new JObject();
            root.Add("jsonrpc", "2.0");
            root.Add("id", notification.Id);
            root.Add("method", "aria2.addUri");
            root.Add("params", new JArray(notification.FilesToDownload.Select(p => new JArray(p.ToString()))));
            var json = JsonConvert.SerializeObject(root);

            try
            {
                using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                using var message = new HttpRequestMessage(HttpMethod.Post, "jsonrpc") { Content = stringContent };
                using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
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
