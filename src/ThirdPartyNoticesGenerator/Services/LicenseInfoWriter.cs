using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;

using ThirdPartyNoticesGenerator.Extensions;
using ThirdPartyNoticesGenerator.Models;

namespace ThirdPartyNoticesGenerator.Services
{
    internal class LicenseInfoWriter
    {
        private const string LicenseNotice = "License notice for the following libraries:";

        private static readonly Encoding Encoding = Encoding.UTF8;
        private static readonly byte[] LicenseNoticeBytes = Encoding.GetBytes(LicenseNotice);
        private static readonly byte[] NewLine = { (byte) '\r', (byte) '\n' };

        public void WriteLicense(LicenseForLibraries licenseForLibraries, IBufferWriter<byte> writer)
        {
            var encoder = Encoding.GetEncoder();

            var longestNameLen = licenseForLibraries.Libraries.Select(x => x.RelativeOutputPath).Concat(new []{ LicenseNotice }).Max(x => x.Length);
            Span<char> longestName = stackalloc char[longestNameLen];
            longestName.Fill('=');

            writer.Write(LicenseNoticeBytes);
            writer.Write(NewLine);
            foreach (var resolvedFileInfo in licenseForLibraries.Libraries.OrderBy(x => Path.GetFileNameWithoutExtension(x.PackagePath)))
            {
                writer.Write(resolvedFileInfo.RelativeOutputPath, encoder);
                writer.Write(NewLine);
            }
            writer.Write(longestName, encoder);
            writer.Write(NewLine);
            writer.Write(licenseForLibraries.LicenseContent, encoder);
            writer.Write(NewLine);
            writer.Write(NewLine);
        }
    }
}