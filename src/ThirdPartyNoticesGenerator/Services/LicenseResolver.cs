using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NuGet.Packaging;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ThirdPartyNoticesGenerator.Options;
using ThirdPartyNoticesGenerator.Services.LicenseResolvers;

namespace ThirdPartyNoticesGenerator.Services
{
    internal sealed class LicenseResolver
    {
        private readonly ILogger _logger;
        private readonly LicenseResolverOptions _options;
        private readonly UrlPlainTextResolver _urlPlainTextResolver;
        private readonly UrlRedirectResolver _urlRedirectResolver;
        private readonly IEnumerable<ILicenseUriLicenseResolver> _licenseUriLicenseResolvers;
        private readonly IEnumerable<IRepositoryUriLicenseResolver> _repositoryUriLicenseResolvers;
        private readonly IEnumerable<IProjectUriLicenseResolver> _projectUriLicenseResolvers;
        private readonly IMemoryCache _licenseCache;

        public LicenseResolver(
            ILogger<LicenseResolver> logger,
            IOptions<LicenseResolverOptions> options,
            UrlPlainTextResolver urlPlainTextResolver,
            UrlRedirectResolver urlRedirectResolver,
            IEnumerable<ILicenseUriLicenseResolver> licenseUriLicenseResolvers,
            IEnumerable<IRepositoryUriLicenseResolver> repositoryUriLicenseResolvers,
            IEnumerable<IProjectUriLicenseResolver> projectUriLicenseResolvers,
            IMemoryCache memoryCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _urlPlainTextResolver = urlPlainTextResolver ?? throw new ArgumentNullException(nameof(urlPlainTextResolver));
            _urlRedirectResolver = urlRedirectResolver ?? throw new ArgumentNullException(nameof(urlRedirectResolver));
            _licenseUriLicenseResolvers = licenseUriLicenseResolvers ?? throw new ArgumentNullException(nameof(licenseUriLicenseResolvers));
            _repositoryUriLicenseResolvers = repositoryUriLicenseResolvers ?? throw new ArgumentNullException(nameof(repositoryUriLicenseResolvers));
            _projectUriLicenseResolvers = projectUriLicenseResolvers ?? throw new ArgumentNullException(nameof(projectUriLicenseResolvers));
            _licenseCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// Priority:
        /// * File based License
        /// * Repository based License
        /// * Generic LicenseUrl
        /// * ProjectUrl based License
        /// </summary>
        public async Task<string?> ResolveLicense(PackageArchiveReader packageReader)
        {
            if (packageReader is null) throw new ArgumentNullException(nameof(packageReader));

            var licenseMetadata = packageReader.NuspecReader.GetLicenseMetadata();
            var licenseUrl = packageReader.NuspecReader.GetLicenseUrl();
            var repositoryMetadata = packageReader.NuspecReader.GetRepositoryMetadata();
            var projectUrl = packageReader.NuspecReader.GetProjectUrl();
            
            // The license contained in the nupkg
            if (licenseMetadata is not null && licenseMetadata.Type == LicenseType.File && !string.IsNullOrEmpty(licenseMetadata.License))
            {
                await using var licenseStream = packageReader.GetEntry(licenseMetadata.License).Open();
                using var licenseStreamReader = new StreamReader(licenseStream);
                var license = await licenseStreamReader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(license)) return license;
            }

            // The license from the specified commit
            if (repositoryMetadata is not null && !string.IsNullOrEmpty(repositoryMetadata.Url) && !string.IsNullOrEmpty(repositoryMetadata.Commit))
            {
                var type = repositoryMetadata.Type;
                var url = repositoryMetadata.Url;
                var commit = repositoryMetadata.Commit;
                var license = await _licenseCache.GetOrCreateAsync($"{url}/{commit}", async _ => await ResolveLicenseFromRepositoryUri(type, new Uri(url), commit));
                if (!string.IsNullOrEmpty(license)) return license;
            }

            // Try to get the generic/concrete license from license url
            if (!string.IsNullOrEmpty(licenseUrl))
            {
                var license = await _licenseCache.GetOrCreateAsync(licenseUrl, async _ => await ResolveLicenseFromLicenseUri(new Uri(licenseUrl)));
                if (!string.IsNullOrEmpty(license)) return license;
            }

            // License from the repository default branch
            if (!string.IsNullOrEmpty(projectUrl))
            {
                var license = await _licenseCache.GetOrCreateAsync(projectUrl, async _ => await ResolveLicenseFromProjectUri(new Uri(projectUrl)));
                if (!string.IsNullOrEmpty(license)) return license;
            }

            return null;
        }

        private bool TryFindLicenseUriLicenseResolver(Uri licenseUri, [NotNullWhen(true)] out ILicenseUriLicenseResolver? resolver)
        {
            var allowUnsafeResolvers = _options.UseUnsafeResolvers;
            resolver = _licenseUriLicenseResolvers.FirstOrDefault(r => (!allowUnsafeResolvers && r.IsSafe || allowUnsafeResolvers) && r.CanResolve(licenseUri));
            return resolver != null;
        }

        private bool TryFindRepositoryUriLicenseResolver(Uri projectUri, [NotNullWhen(true)] out IRepositoryUriLicenseResolver? resolver)
        {
            var allowUnsafeResolvers = _options.UseUnsafeResolvers;
            resolver = _repositoryUriLicenseResolvers.FirstOrDefault(r => (!allowUnsafeResolvers && r.IsSafe || allowUnsafeResolvers) && r.CanResolve(projectUri));
            return resolver != null;
        }

        private bool TryFindProjectUriLicenseResolver(Uri projectUri, [NotNullWhen(true)] out IProjectUriLicenseResolver? resolver)
        {
            var allowUnsafeResolvers = _options.UseUnsafeResolvers;
            resolver = _projectUriLicenseResolvers.FirstOrDefault(r => (!allowUnsafeResolvers && r.IsSafe || allowUnsafeResolvers) && r.CanResolve(projectUri));
            return resolver != null;
        }

        private async Task<string?> ResolveLicenseFromLicenseUri(Uri licenseUri)
        {
            while (true)
            {
                if (TryFindLicenseUriLicenseResolver(licenseUri, out var licenseUriLicenseResolver))
                    return await licenseUriLicenseResolver.Resolve(licenseUri);

                var redirectUri = await _urlRedirectResolver.GetRedirectUri(licenseUri);
                if (redirectUri != null)
                {
                    licenseUri = redirectUri;
                    continue;
                }

                // Finally, if no license uri can be found despite all the redirects, try to blindly get it
                return await _urlPlainTextResolver.GetPlainText(licenseUri);
            }
        }

        private async Task<string?> ResolveLicenseFromRepositoryUri(string type, Uri sourceUri, string commit)
        {
            while (true)
            {
                if (TryFindRepositoryUriLicenseResolver(sourceUri, out var repositoryUriLicenseResolver))
                    return await repositoryUriLicenseResolver.Resolve(type, sourceUri, commit);

                var redirectUri = await _urlRedirectResolver.GetRedirectUri(sourceUri);
                if (redirectUri != null)
                {
                    sourceUri = redirectUri;
                    continue;
                }

                return null;
            }
        }

        private async Task<string?> ResolveLicenseFromProjectUri(Uri projectUri)
        {
            while (true)
            {
                if (TryFindProjectUriLicenseResolver(projectUri, out var projectUriLicenseResolver))
                    return await projectUriLicenseResolver.Resolve(projectUri);

                var redirectUri = await _urlRedirectResolver.GetRedirectUri(projectUri);
                if (redirectUri != null)
                {
                    projectUri = redirectUri;
                    continue;
                }

                return null;
            }
        }
    }
}