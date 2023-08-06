using System.Collections.Concurrent;
using Core.Util;

namespace ThumbKey;

public static class RedditDataReader
{
    public static string InitFile(string path)
    {
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Returns info to generate a span
    /// </summary>
    /// <param name="text"></param>
    /// <param name="tag"></param>
    /// <param name="capacity"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static List<Range> GetAllStringsOfTag(string text, string tag)
    {
        Console.WriteLine("Parsing body text...");

        const int partitionQuantity = 100;
        int partitionSize = text.Length / partitionQuantity;

        var customPartitioner = Partitioner.Create(0, text.Length, partitionSize);
        var rangeBag = new ConcurrentBag<Range>();

        Parallel.ForEach(customPartitioner, (range, state) =>
        {
            string wholeTag = $"\"{tag}\":";
            var input = text.AsSpan(range.Item1, range.Item2 - range.Item1);
            var tagSpan = wholeTag.AsSpan();
            int tagStart = input.IndexOf(tagSpan);

            while (tagStart != -1)
            {
                var start = tagStart + tagSpan.Length;
                if (start >= input.Length)
                    break;
                input = input.Slice(start);

                int length;
                bool gotEnd;

                do
                {
                    length = input.IndexOf('\"');
                    if (length == -1)
                        return;

                    gotEnd = input[length - 1] != '\\';
                } while (!gotEnd);

                var rangeToAdd = new Range(range.Item1 + tagStart, range.Item1 + tagStart + length);
                rangeBag.Add(rangeToAdd);

                tagStart = input.IndexOf(tagSpan);
            }
        });

        return rangeBag.ToList();
    }
}