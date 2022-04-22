using System.Collections.Generic;

namespace ThirdPartyNoticesGenerator.Models
{
    internal record LicenseForLibraries(string LicenseContent, IReadOnlyCollection<Library> Libraries);
}