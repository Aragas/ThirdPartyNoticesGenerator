using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal interface IRepositoryUriLicenseResolver
    {
        bool IsSafe { get; }
        bool CanResolve(Uri sourceUri);
        Task<string?> ResolveAsync(string type, Uri sourceUri, string commit, CancellationToken ct);
    }
}