using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using ThirdPartyNoticesGenerator.Options;
using ThirdPartyNoticesGenerator.Services;
using ThirdPartyNoticesGenerator.Services.LicenseResolvers;

namespace ThirdPartyNoticesGenerator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            await new CommandLineBuilder(new GenerateThirdPartyNoticesCommand())
                .UseHost(_ => new HostBuilder(), host => host.ConfigureServices((ctx, services) =>
                {
                    services.AddMemoryCache();

                    services.AddHttpClient<GitHubAPIClient>().ConfigureHttpClient((sp, client) =>
                    {
                        var opts = sp.GetRequiredService<IOptions<GitHubAPIClientOptions>>().Value;

                        client.BaseAddress = new Uri("https://api.github.com");
                        client.Timeout = TimeSpan.FromSeconds(3);
                        // https://developer.github.com/v3/#user-agent-required
                        client.DefaultRequestHeaders.Add("User-Agent", "ThirdPartyNoticesGenerator");
                        if (!string.IsNullOrEmpty(opts.GitHubOAuth))
                        {
                            var basicAuthValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(opts.GitHubOAuth));
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthValue);
                        }
                        if (!string.IsNullOrEmpty(opts.GitHubToken))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.GitHubToken);
                        }
                    });
                    services.AddHttpClient<NuGetLicenseClient>().ConfigureHttpClient((sp, client) =>
                    {
                        client.BaseAddress = new Uri("https://licenses.nuget.org/");
                        client.Timeout = TimeSpan.FromSeconds(3);
                        // https://developer.github.com/v3/#user-agent-required
                        client.DefaultRequestHeaders.Add("User-Agent", "ThirdPartyNoticesGenerator");
                    });

                    services.AddHttpClient<UrlRedirectResolver>()
                        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

                    services.AddHttpClient<UrlPlainTextResolver>();

                    services.AddTransient<ProjectHandler>();
                    services.AddTransient<LicenseResolver>();
                    services.AddTransient<LicenseInfoGenerator>();
                    services.AddTransient<LicenseInfoWriter>();

                    services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseUriLicenseResolver, GithubIOLicenseResolver>());
                    services.TryAddEnumerable(ServiceDescriptor.Transient<IProjectUriLicenseResolver, GithubIOLicenseResolver>());

                    services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseUriLicenseResolver, GithubLicenseResolver>());
                    services.TryAddEnumerable(ServiceDescriptor.Transient<IProjectUriLicenseResolver, GithubLicenseResolver>());
                    services.TryAddEnumerable(ServiceDescriptor.Transient<IRepositoryUriLicenseResolver, GithubLicenseResolver>());

                    services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseUriLicenseResolver, NuGetLicenseResolver>());

                    services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseUriLicenseResolver, OpenSourceOrgLicenseResolver>());

                    services.AddOptions<CLIOptions>()
                        .Configure<BindingContext>((opts, bindingContext) => new ModelBinder<CLIOptions>().UpdateInstance(opts, bindingContext));
                    
                    services.AddOptions<LicenseResolverOptions>()
                        .Configure<BindingContext>((opts, bindingContext) => new ModelBinder<LicenseResolverOptions>().UpdateInstance(opts, bindingContext));

                    services.AddOptions<GitHubAPIClientOptions>()
                        .Configure<BindingContext>((opts, bindingContext) => new ModelBinder<GitHubAPIClientOptions>().UpdateInstance(opts, bindingContext));

                }).ConfigureLogging((ctx, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss:fff ";
                    });
                    builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
                }).UseCommandHandler<GenerateThirdPartyNoticesCommand, GenerateThirdPartyNoticesCommand.Handler>())
                .UseDefaults().Build().InvokeAsync(args);
        }
    }
}