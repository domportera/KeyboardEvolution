using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Core.Util;

namespace ThumbKey;

public static class RedditDataReader
{
    /// <summary>
    /// Returns info to generate a span
    /// </summary>
    /// <param name="text"></param>
    /// <param name="tag"></param>
    /// <param name="minTextLength"></param>
    /// <param name="ignoredPhrases"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static List<Range> GetAllStringsOfTag(string text, string tag, int minTextLength, params string[]? ignoredPhrases)
    {
        Console.WriteLine("Parsing body text...");

        ignoredPhrases ??= Array.Empty<string>();
        ignoredPhrases = ignoredPhrases.Select(s => s.ToLower()).ToArray();
        const int partitionQuantity = 100;
        int partitionSize = text.Length / partitionQuantity;

        var customPartitioner = Partitioner.Create(0, text.Length, partitionSize);
        var rangeBag = new ConcurrentBag<Range>();

        Parallel.ForEach(customPartitioner, (range, state) =>
        {
            string wholeTag = $"\"{tag}\":";
            var input = text.AsSpan();
            var tagSpan = wholeTag.AsSpan();
            int endIndex = range.Item2;
            int startIndex = GetIndexAfterTag(input, tagSpan, range.Item1);

            while (startIndex > 0)
            {
                if (startIndex > endIndex)
                    break;

                bool gotEnd = false;

                var remainingText = input[startIndex..endIndex];
                int openQuotesIndex = remainingText.IndexOf('\"');
                if (openQuotesIndex < 0)
                    return;

                 // increment to exclude opening quotation mark
                startIndex += openQuotesIndex + 1;
                remainingText = input[startIndex..endIndex];

                int length = 0;
                int searchIndex = 0;
                while(!gotEnd)
                {
                    var searchSpan = remainingText[searchIndex..];
                    length += searchSpan.IndexOf('\"');
                    if (length < 0)
                        return;
                    gotEnd = length == 0 || remainingText[length - 1] != '\\';
                    searchIndex += length + 1;
                }

                int rangeEnd = startIndex + length - 1; // exclude closing quotation mark

                if (rangeEnd - startIndex >= minTextLength)
                {
                    var rangeToAdd = new Range(startIndex, rangeEnd);
                    var currentEntryLowercase = input[rangeToAdd].ToString().ToLower();
                    var shouldUse = true;
                    foreach(var phrase in ignoredPhrases)
                    {
                        if (currentEntryLowercase.IndexOf(phrase, StringComparison.Ordinal) != -1)
                        {
                            shouldUse = false;
                            break;
                        }
                    }
                    
                    if(shouldUse)
                        rangeBag.Add(rangeToAdd);
                }

                startIndex = GetIndexAfterTag(input, tagSpan, rangeEnd);
            }
        });

        return rangeBag.ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetIndexAfterTag(ReadOnlySpan<char> input, ReadOnlySpan<char> tagSpan, int startIndex)
        {
            input = input.Slice(startIndex);
            return input.IndexOf(tagSpan) + tagSpan.Length + startIndex + 1;
        }
    }
}