using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

        public async Task<string?> GetLicenseContentFromId(string licenseId)
        {
            try
            {
                var json = await _httpClient.GetStringAsync($"licenses/{licenseId}");
                var jsonDocument = JsonDocument.Parse(json);
                return jsonDocument.RootElement.GetProperty("body").GetString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting GitHub license! {LicenseId}", licenseId);
                return null;
            }
        }

        public async Task<string?> GetLicenseContentFromRepositoryPath(string repositoryPath, string? commit = null)
        {
            try
            {
                repositoryPath = repositoryPath.TrimEnd('/');
                var json = await _httpClient.GetStringAsync($"repos{repositoryPath}/license{(string.IsNullOrEmpty(commit) ? string.Empty : $"?ref={commit}")}");
                var jsonDocument = JsonDocument.Parse(json);

                var rootElement = jsonDocument.RootElement;
                var encoding = rootElement.GetProperty("encoding").GetString();
                var content = rootElement.GetProperty("content").GetString();

                if (encoding != "base64") return content;
                if (content is null)
                {
                    _logger.LogError("Error while getting GitHub license! Content was null! {RepositoryPath}", repositoryPath);
                    return null;
                }

                var bytes = Convert.FromBase64String(content);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting GitHub license! {RepositoryPath}", repositoryPath);
                return null;
            }
        }
    }
}