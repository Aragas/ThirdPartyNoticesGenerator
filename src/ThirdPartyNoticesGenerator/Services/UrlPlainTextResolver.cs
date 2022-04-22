using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services
{
    internal class UrlPlainTextResolver
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public UrlPlainTextResolver(ILogger<UrlPlainTextResolver> logger, HttpClient client)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<string?> GetPlainText(Uri uri)
        {
            try
            {
                var httpResponseMessage = await _httpClient.GetAsync(uri);
                if (!httpResponseMessage.IsSuccessStatusCode && uri.AbsolutePath.EndsWith(".txt"))
                {
                    // try without .txt extension
                    var fixedUri = new UriBuilder(uri);
                    fixedUri.Path = fixedUri.Path.Remove(fixedUri.Path.Length - 4);
                    httpResponseMessage = await _httpClient.GetAsync(fixedUri.Uri);
                    if (!httpResponseMessage.IsSuccessStatusCode)
                        return null;
                }

                if (httpResponseMessage.Content.Headers.ContentType?.MediaType != "text/plain")
                    return uri.ToString();

                return await httpResponseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting the plain text content! {Uri}", uri);
                return null;
            }
        }
    }
}