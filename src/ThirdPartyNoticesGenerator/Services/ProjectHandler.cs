using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging;

using NuGet.Versioning;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ThirdPartyNoticesGenerator.Models;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ThirdPartyNoticesGenerator.Services
{
    internal sealed class ProjectHandler
    {
        private readonly ILogger _logger;

        public ProjectHandler(ILogger<ProjectHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<Library> ResolveDependencies(Project project)
        {
            var projectInstance = project.CreateProjectInstance();

            if (bool.TryParse(project.GetProperty("UsingMicrosoftNETSdk")?.EvaluatedValue, out var isNewSdk) && isNewSdk)
                return ResolveLibrariesUsingComputeFilesToPublish(projectInstance);
            else
            {
                _logger.LogError("Old style MSBuild projects (non SDK) are currently not supported");
                return ResolveLibrariesUsingResolveAssemblyReferences(projectInstance);
            }
        }

        private IEnumerable<Library> ResolveLibrariesUsingResolveAssemblyReferences(ProjectInstance projectInstance)
        {
            projectInstance.SetProperty("BuildingProject", "false");

            if (!projectInstance.Build("ResolveAssemblyReferences", new Microsoft.Build.Framework.ILogger[] {new ConsoleLogger(LoggerVerbosity.Minimal)}))
            {
                _logger.LogError("Failed to run task 'ResolveAssemblyReferences'!");
                yield break;
            }

            foreach (var item in projectInstance.GetItems("ReferenceCopyLocalPaths"))
            {
                var assemblyPath = item.EvaluatedInclude;
                if (item.GetMetadataValue("ResolvedFrom") == "{HintPathFromItem}" && item.GetMetadataValue("HintPath").StartsWith("..\\packages"))
                {
                    var packagePath = GetPackagePathFromAssemblyPath(assemblyPath);
                    if (packagePath == null)
                        throw new ApplicationException($"Cannot find package path from assembly path ({assemblyPath})");

                    var nuPkgFilePath = Directory.GetFiles(packagePath, "*.nupkg", SearchOption.TopDirectoryOnly).Single();
                    yield return new Library(assemblyPath, Path.GetFileName(assemblyPath), nuPkgFilePath);
                }
                else
                {
                    _logger.LogError("");
                    continue;
                    //yield return resolvedFileInfo;
                }
            }
        }

        private IEnumerable<Library> ResolveLibrariesUsingComputeFilesToPublish(ProjectInstance projectInstance)
        {
            if (!projectInstance.Build("ComputeFilesToPublish", new Microsoft.Build.Framework.ILogger[] { new ConsoleLogger(LoggerVerbosity.Minimal) }))
            {
                _logger.LogError("Failed to run task 'ComputeFilesToPublish'!");
                yield break;
            }

            foreach (var item in projectInstance.GetItems("ResolvedFileToPublish"))
            {
                var assemblyPath = item.EvaluatedInclude;

                var packageName = item.GetMetadataValue(item.HasMetadata("PackageName") ? "PackageName" : "NugetPackageId").ToLowerInvariant();
                var packageVersion = item.GetMetadataValue(item.HasMetadata("PackageName") ? "PackageVersion" : "NugetPackageVersion").ToLowerInvariant();
                if (packageName == string.Empty || packageVersion == string.Empty)
                {
                    // Skip if it's not a NuGet package
                    continue;
                }

                var packagePath = GetPackagePathFromAssemblyPath(assemblyPath);
                if (packagePath == null)
                    throw new ApplicationException($"Cannot find package path from assembly path ({assemblyPath})");

                var relativePath = item.GetMetadataValue("RelativePath");
                // var nuPkgFilePath = Directory.GetFiles(packageFolder, "*.nuspec", SearchOption.TopDirectoryOnly).SingleOrDefault();
                var nuPkgFilePath = Path.Combine(packagePath, $"{packageName}.{packageVersion}.nupkg");
                yield return new Library(assemblyPath, relativePath, nuPkgFilePath);
            }
        }

        private static string? GetPackagePathFromAssemblyPath(string assemblyPath)
        {
            var parentDirectoryInfo = Directory.GetParent(assemblyPath);
            var isValid = false;
            while (parentDirectoryInfo != null && !(isValid = NuGetVersion.TryParse(parentDirectoryInfo.Name, out _)))
            {
                parentDirectoryInfo = parentDirectoryInfo.Parent;
            }
            return isValid ? parentDirectoryInfo?.FullName : null;
        }
    }
}