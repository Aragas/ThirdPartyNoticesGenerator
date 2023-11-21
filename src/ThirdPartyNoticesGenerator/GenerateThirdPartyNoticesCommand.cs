using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

using ThirdPartyNoticesGenerator.Extensions;
using ThirdPartyNoticesGenerator.Options;
using ThirdPartyNoticesGenerator.Services;

namespace ThirdPartyNoticesGenerator
{
    public class GenerateThirdPartyNoticesCommand : RootCommand
    {
        public GenerateThirdPartyNoticesCommand() : base("Generates ThirdPartyNotices")
        {
            AddArgument(new Argument<string>("scan-dir", "Path of the directory to look for projects (optional)"));
            AddOption(new Option<string>("--output-filename", () => "third-party-notices.txt", "Output filename"));
            AddOption(new Option<string>("--project-configuration", () => "Release", "Which project configuration to use"));
            AddOption(new Option<bool>("--copy-to-project-outdir", () => false, "Copy to output directory in Release configuration"));
            AddOption(new Option<bool>("--use-unsafe-resolvers", () => false, "Enable unsafe license resolvers that can yield misleading licenses"));
            AddOption(new Option<string>("--github-oauth", () => string.Empty, "GitHub's OAuth string in the format of 'ClientId:ClientSecret'"));
        }

        internal new class Handler : ICommandHandler
        {
            private static readonly string[] MSBuildProjectTypes = { ".csproj", ".vbproj", ".fsproj" };

            private readonly ILogger _logger;
            private readonly CLIOptions _options;
            private readonly LicenseInfoGenerator _licenseInfoGenerator;
            private readonly LicenseInfoWriter _licenseInfoWriter;

            public Handler(ILogger<Handler> logger, IOptions<CLIOptions> options, LicenseInfoGenerator licenseInfoGenerator, LicenseInfoWriter licenseInfoWriter)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _options = options.Value ?? throw new ArgumentNullException(nameof(options));
                _licenseInfoGenerator = licenseInfoGenerator ?? throw new ArgumentNullException(nameof(licenseInfoGenerator));
                _licenseInfoWriter = licenseInfoWriter ?? throw new ArgumentNullException(nameof(licenseInfoWriter));
            }

            public int Invoke(InvocationContext context) => throw new NotSupportedException();

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var scanDirectory = _options.ScanDir ?? Directory.GetCurrentDirectory();
                _logger.LogInformation("Scanning directory '{Directory}'", scanDirectory);
                if (Directory.GetFiles(scanDirectory, "*.*", SearchOption.TopDirectoryOnly).SingleOrDefault(x => MSBuildProjectTypes.Any(x.EndsWith)) is not { } projectFilePath)
                {
                    _logger.LogInformation("No C#, F#, or VisualBasic project file found in the current directory.");
                    return 1;
                }

                _logger.LogInformation("Using project configuration '{Configuration}'", _options.ProjectConfiguration);
                var project = new Project(projectFilePath);
                project.SetProperty("Configuration", _options.ProjectConfiguration);
                project.SetProperty("DesignTimeBuild", "true");

                var outputFileName = _options.OutputFileName;
                if (_options.CopyToProjectOutputDir)
                    outputFileName = Path.Combine(Path.Combine(scanDirectory, project.GetPropertyValue("OutDir")), Path.GetFileName(outputFileName));
                _logger.LogInformation("Using output file path '{OutputFilePath}'", outputFileName);

                await using var stream = File.Create(outputFileName);
                var writer = PipeWriter.Create(stream);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var licenseCount = 0;
                var resolvedLibrariesCount = 0;
                await foreach (var (licenseInfo, _, _, isLast) in _licenseInfoGenerator.GetLicensesForProject(project).WithIterationInfo())
                {
                    _licenseInfoWriter.WriteLicense(licenseInfo, writer, isLast);
                    await writer.FlushAsync();
                    resolvedLibrariesCount += licenseInfo.Libraries.Count;
                    licenseCount++;
                }

                stopwatch.Stop();
                _logger.LogInformation("Resolved {LicenseCount} licenses for {ResolvedLibrariesCount} libraries in {Milliseconds}ms", licenseCount, resolvedLibrariesCount, stopwatch.ElapsedMilliseconds);
                return 0;
            }
        }
    }
}