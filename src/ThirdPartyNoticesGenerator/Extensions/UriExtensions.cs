using System;

namespace ThirdPartyNoticesGenerator.Extensions
{
    internal static class UriExtensions
    {
        public static bool IsGithubUri(this Uri uri) => uri.Host == "github.com";
        public static bool IsGithubIOUri(this Uri uri) => uri.Host.EndsWith("github.io");

        public static Uri ToRawGithubUserContentUri(this Uri uri)
        {
            if (!IsGithubUri(uri)) throw new InvalidOperationException();

            var uriBuilder = new UriBuilder(uri) { Host = "raw.githubusercontent.com" };
            uriBuilder.Path = uriBuilder.Path.Replace("/blob", string.Empty);
            return uriBuilder.Uri;
        }
    }
}