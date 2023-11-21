using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services;

internal class NuGetLicenseClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public NuGetLicenseClient(ILogger<NuGetLicenseClient> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string?> GetLicenseContentFromId(string licenseId)
    {
        try
        {
            var text = await _httpClient.GetStringAsync($"{licenseId}");
            return text;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting NuGet license! License Id: '{LicenseId}'", licenseId);
            return null;
        }
    }
}