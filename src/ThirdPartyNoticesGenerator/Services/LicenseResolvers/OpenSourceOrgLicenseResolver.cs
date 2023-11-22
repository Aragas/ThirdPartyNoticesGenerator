using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal sealed class OpenSourceOrgLicenseResolver : ILicenseUriLicenseResolver
    {
        private readonly GitHubAPIClient _gitHubClient;

        public OpenSourceOrgLicenseResolver(GitHubAPIClient gitHubClient)
        {
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
        }

        public bool IsSafe => true;

        public bool CanResolve(Uri licenseUri) => licenseUri.Host == "opensource.org";

        public Task<string?> ResolveAsync(Uri licenseUri, CancellationToken ct)
        {
            var split = licenseUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length <= 0 || split[0] != "licenses") return Task.FromResult<string?>(null);
            var licenseId = split[1];
            return _gitHubClient.GetLicenseContentFromIdAsync(licenseId, ct);
        }
    }
}