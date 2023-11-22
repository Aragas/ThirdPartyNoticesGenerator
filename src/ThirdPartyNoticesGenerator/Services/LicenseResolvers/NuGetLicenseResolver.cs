using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal sealed class NuGetLicenseResolver : ILicenseUriLicenseResolver
    {
        private readonly NuGetLicenseClient _nugetLicenseClient;

        public NuGetLicenseResolver(NuGetLicenseClient nugetLicenseClient)
        {
            _nugetLicenseClient = nugetLicenseClient ?? throw new ArgumentNullException(nameof(nugetLicenseClient));
        }

        public bool IsSafe => true;

        public bool CanResolve(Uri licenseUri) => licenseUri.Host == "licenses.nuget.org";

        public Task<string?> ResolveAsync(Uri licenseUri, CancellationToken ct)
        {
            var split = licenseUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length <= 0) return Task.FromResult<string?>(null);
            var licenseId = split[0];
            return _nugetLicenseClient.GetLicenseContentFromIdAsync(licenseId, ct);
        }
    }
}