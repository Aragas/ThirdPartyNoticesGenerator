namespace ThirdPartyNoticesGenerator.Options
{
    public class CLIOptions
    {
        public string? ScanDir { get; set; } = default!;
        public string OutputFileName { get; set; } = default!;
        public string ProjectConfiguration { get; set; } = default!;
        public bool CopyToProjectOutputDir { get; set; } = default!;
        public bool UseUnsafeResolvers { get; set; } = default!;
        public string GitHubOAuth { get; set; } = default!;
    }
}