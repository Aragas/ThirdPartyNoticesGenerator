using System;
using System.Threading.Tasks;

namespace ThirdPartyNoticesGenerator.Services.LicenseResolvers
{
    internal interface ILicenseUriLicenseResolver
    {
        bool IsSafe { get; }
        bool CanResolve(Uri licenseUri);
        Task<string?> Resolve(Uri licenseUri);
    }
}