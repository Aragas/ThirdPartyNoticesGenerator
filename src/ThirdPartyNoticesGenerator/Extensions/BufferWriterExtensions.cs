using System;
using System.Buffers;
using System.Text;

namespace ThirdPartyNoticesGenerator.Extensions
{
    internal static class BufferWriterExtensions
    {
        public static void Write(this IBufferWriter<byte> output, ReadOnlySpan<char> chars, Encoder encoder)
        {
            encoder.Convert(chars, output, false, out _, out _);
        }
    }
}