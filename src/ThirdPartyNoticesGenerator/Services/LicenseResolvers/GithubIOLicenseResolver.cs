using System;
using System.Threading.Tasks;

using ThirdPartyNoticesGenerator.Extensions;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal sealed class GithubIOLicenseResolver : ILicenseUriLicenseResolver, IProjectUriLicenseResolver
    {
        private readonly GitHubAPIClient _gitHubClient;
        private readonly UrlPlainTextResolver _urlPlainTextResolver;

        public GithubIOLicenseResolver(GitHubAPIClient gitHubClient, UrlPlainTextResolver urlPlainTextResolver)
        {
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            _urlPlainTextResolver = urlPlainTextResolver ?? throw new ArgumentNullException(nameof(urlPlainTextResolver));
        }

        bool ILicenseUriLicenseResolver.IsSafe => false;
        bool IProjectUriLicenseResolver.IsSafe => false;

        bool ILicenseUriLicenseResolver.CanResolve(Uri uri) => uri.IsGithubIOUri();
        bool IProjectUriLicenseResolver.CanResolve(Uri uri) => uri.IsGithubIOUri();

        Task<string?> ILicenseUriLicenseResolver.Resolve(Uri licenseUri) => _urlPlainTextResolver.GetPlainText(licenseUri.ToRawGithubUserContentUri());
        Task<string?> IProjectUriLicenseResolver.Resolve(Uri projectUri) => _gitHubClient.GetLicenseContentFromRepositoryPath($"/{projectUri.Host.Split('.')[0]}{projectUri.AbsolutePath}");
    }
}