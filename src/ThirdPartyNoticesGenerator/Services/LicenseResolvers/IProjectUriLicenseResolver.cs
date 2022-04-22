using System;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal interface IProjectUriLicenseResolver
    {
        bool IsSafe { get; }
        bool CanResolve(Uri projectUri);
        Task<string?> Resolve(Uri projectUri);
    }
}