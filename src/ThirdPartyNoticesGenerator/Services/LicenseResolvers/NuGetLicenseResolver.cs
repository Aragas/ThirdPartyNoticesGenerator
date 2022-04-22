using System;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal sealed class NuGetLicenseResolver : ILicenseUriLicenseResolver
    {
        private readonly GitHubAPIClient _gitHubClient;

        public NuGetLicenseResolver(GitHubAPIClient gitHubClient)
        {
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
        }

        public bool IsSafe => true;

        public bool CanResolve(Uri licenseUri) => licenseUri.Host == "licenses.nuget.org";

        public Task<string?> Resolve(Uri licenseUri)
        {
            var split = licenseUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length <= 0) return Task.FromResult<string?>(null);
            var licenseId = split[0];
            return _gitHubClient.GetLicenseContentFromId(licenseId);
        }
    }
}