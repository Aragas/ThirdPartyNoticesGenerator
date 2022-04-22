using System.Collections.Generic;

namespace ThirdPartyNoticesGenerator.Extensions
{
    public record IterationInfo<T>(T Value, int Index, bool IsFirst, bool IsLast);

    public static class EnumerableExtensions
    {
        public static async IAsyncEnumerable<IterationInfo<T>> WithIterationInfo<T>(this IAsyncEnumerable<T> enumerable)
        {
            await using var enumerator = enumerable.GetAsyncEnumerator();
            var isFirst = true;
            var hasNext = await enumerator.MoveNextAsync();
            var index = 0;
            while (hasNext)
            {
                var current = enumerator.Current;
                hasNext = await enumerator.MoveNextAsync();
                yield return new IterationInfo<T>(current, index, isFirst, !hasNext);
                isFirst = false;
                index++;
            }
        }
    }
}