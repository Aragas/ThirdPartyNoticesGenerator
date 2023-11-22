using Microsoft.Extensions.Logging;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services
{
    internal class GitHubAPIClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public GitHubAPIClient(ILogger<GitHubAPIClient> logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string?> GetLicenseContentFromIdAsync(string licenseId, CancellationToken ct)
        {
            try
            {
                var json = await _httpClient.GetStringAsync($"licenses/{licenseId}", ct);
                var jsonDocument = JsonDocument.Parse(json);
                return jsonDocument.RootElement.GetProperty("body").GetString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting GitHub license! License Id: '{LicenseId}'", licenseId);
                return null;
            }
        }

        public async Task<string?> GetLicenseContentFromRepositoryPathAsync(string repositoryPath, string? commit = null, CancellationToken ct = default)
        {
            try
            {
                repositoryPath = repositoryPath.TrimEnd('/');
                using var request = new HttpRequestMessage(HttpMethod.Get, $"repos{repositoryPath}/license{(string.IsNullOrEmpty(commit) ? string.Empty : $"?ref={commit}")}");
                using var response = await _httpClient.SendAsync(request, ct);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                var jsonDocument = JsonDocument.Parse(json);

                var rootElement = jsonDocument.RootElement;
                var encoding = rootElement.GetProperty("encoding").GetString();
                var content = rootElement.GetProperty("content").GetString();

                if (encoding != "base64") return content;
                if (content is null)
                {
                    _logger.LogError("Error while getting GitHub license! Content was null! Repository: '{RepositoryPath}'", repositoryPath);
                    return null;
                }

                var bytes = Convert.FromBase64String(content);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting GitHub license! Repository: '{RepositoryPath}'", repositoryPath);
                return null;
            }
        }
    }
}