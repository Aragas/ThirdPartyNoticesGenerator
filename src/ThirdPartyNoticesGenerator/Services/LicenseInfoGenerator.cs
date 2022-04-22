using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

using NuGet.Packaging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ThirdPartyNoticesGenerator.Models;

namespace ThirdPartyNoticesGenerator.Services
{
    internal sealed class LicenseInfoGenerator
    {
        private readonly ILogger _logger;
        private readonly ProjectHandler _projectHandler;
        private readonly LicenseResolver _licenseResolver;

        public LicenseInfoGenerator(ILogger<LicenseInfoGenerator> logger, ProjectHandler projectHandler, LicenseResolver licenseResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectHandler = projectHandler ?? throw new ArgumentNullException(nameof(projectHandler));
            _licenseResolver = licenseResolver ?? throw new ArgumentNullException(nameof(licenseResolver));
        }

        public async IAsyncEnumerable<LicenseForLibraries> GetLicensesForProject(Project project)
        {
            var libraries = _projectHandler.ResolveDependencies(project);
            await foreach (var grouping in GetLicenseForLibrariesAsync(libraries).GroupBy(x => x.Item1, tuple => tuple.Item2))
            {
                yield return new LicenseForLibraries(grouping.Key, await grouping.ToArrayAsync());
            }
        }

        private async IAsyncEnumerable<(string, Library)> GetLicenseForLibrariesAsync(IEnumerable<Library> libraries)
        {
            foreach (var library in libraries)
            {
                _logger.LogInformation("Resolving license for {RelativeOutputPath}", library.RelativeOutputPath);
                if (string.IsNullOrEmpty(library.PackagePath))
                {
                    _logger.LogError("PackagePath for {RelativeOutputPath} is null or empty!", library.RelativeOutputPath);
                    continue;
                }

                await using var fs = File.OpenRead(library.PackagePath);
                using var packageReader = new PackageArchiveReader(fs);

                _logger.LogInformation("Package Id for {RelativeOutputPath}: {PackageId}", library.RelativeOutputPath, packageReader.NuspecReader.GetId());

                var licenseContent = await _licenseResolver.ResolveLicense(packageReader);
                if (licenseContent == null)
                {
                    //unresolvedFiles.Add(resolvedFileInfo);
                    _logger.LogError("No license found for {RelativeOutputPath}. Source path: {SourcePath}. Verify this manually", library.RelativeOutputPath, library.SourcePath);
                    continue;
                }

                yield return (licenseContent, library);
            }
        }
    }
}