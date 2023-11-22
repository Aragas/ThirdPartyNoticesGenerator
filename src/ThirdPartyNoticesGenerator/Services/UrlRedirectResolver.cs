using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services
{
    internal class UrlRedirectResolver
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public UrlRedirectResolver(ILogger<UrlRedirectResolver> logger, HttpClient client)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<Uri?> GetRedirectUriAsync(Uri uri, CancellationToken ct)
        {
            try
            {
                var httpResponseMessage = await _httpClient.GetAsync(uri, ct);

                var statusCode = (int) httpResponseMessage.StatusCode;
                if (statusCode is < 300 or > 399)
                    return null;

                if (httpResponseMessage.RequestMessage?.RequestUri is null || httpResponseMessage.Headers.Location is not { } redirectUri)
                    return null;

                return !redirectUri.IsAbsoluteUri ? new Uri(httpResponseMessage.RequestMessage.RequestUri, redirectUri) : redirectUri;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while checking the redirect uri! {Uri}", uri);
                return null;
            }
        }
    }
}