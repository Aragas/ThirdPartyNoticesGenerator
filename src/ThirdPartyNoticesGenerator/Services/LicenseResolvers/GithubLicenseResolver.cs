using System;
using System.Threading;
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

        Task<string?> ILicenseUriLicenseResolver.ResolveAsync(Uri licenseUri, CancellationToken ct) => _urlPlainTextResolver.GetPlainTextAsync(licenseUri.ToRawGithubUserContentUri(), ct);
        Task<string?> IProjectUriLicenseResolver.ResolveAsync(Uri projectUri, CancellationToken ct) => _gitHubClient.GetLicenseContentFromRepositoryPathAsync(projectUri.AbsolutePath, ct: ct);
        Task<string?> IRepositoryUriLicenseResolver.ResolveAsync(string type, Uri sourceUri, string commit, CancellationToken ct)
        {
            var split = sourceUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) return Task.FromResult<string?>(null);
            var repositoryPath = $"/{split[0]}/{split[1].Replace(".git", string.Empty)}";
            return _gitHubClient.GetLicenseContentFromRepositoryPathAsync(repositoryPath, commit, ct);
        }
    }
}