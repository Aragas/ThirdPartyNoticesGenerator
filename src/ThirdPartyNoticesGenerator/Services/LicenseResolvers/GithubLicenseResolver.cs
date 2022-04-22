using System;
using System.Threading.Tasks;

using ThirdPartyNoticesGenerator.Extensions;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal sealed class GithubLicenseResolver : ILicenseUriLicenseResolver, IProjectUriLicenseResolver, IRepositoryUriLicenseResolver
    {
        private readonly GitHubAPIClient _gitHubClient;
        private readonly UrlPlainTextResolver _urlPlainTextResolver;

        public GithubLicenseResolver(GitHubAPIClient gitHubClient, UrlPlainTextResolver urlPlainTextResolver)
        {
            _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            _urlPlainTextResolver = urlPlainTextResolver ?? throw new ArgumentNullException(nameof(urlPlainTextResolver));
        }

        bool ILicenseUriLicenseResolver.IsSafe => true;
        bool IProjectUriLicenseResolver.IsSafe => true;
        bool IRepositoryUriLicenseResolver.IsSafe => true;

        bool ILicenseUriLicenseResolver.CanResolve(Uri uri) => uri.IsGithubUri();
        bool IProjectUriLicenseResolver.CanResolve(Uri uri) => uri.IsGithubUri();
        bool IRepositoryUriLicenseResolver.CanResolve(Uri uri) => uri.IsGithubUri();

        Task<string?> ILicenseUriLicenseResolver.Resolve(Uri licenseUri) => _urlPlainTextResolver.GetPlainText(licenseUri.ToRawGithubUserContentUri());
        Task<string?> IProjectUriLicenseResolver.Resolve(Uri projectUri) => _gitHubClient.GetLicenseContentFromRepositoryPath(projectUri.AbsolutePath);
        Task<string?> IRepositoryUriLicenseResolver.Resolve(string type, Uri sourceUri, string commit)
        {
            var split = sourceUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) return Task.FromResult<string?>(null);
            var repositoryPath = $"/{split[0]}/{split[1].Replace(".git", string.Empty)}";
            return _gitHubClient.GetLicenseContentFromRepositoryPath(repositoryPath, commit);
        }
    }
}