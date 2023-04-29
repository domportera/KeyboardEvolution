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
    /// <param name="inputSpan"></param>
    /// <param name="tag"></param>
    /// <param name="capacity"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool GetAllStringsOfTag(ReadOnlySpan<char> inputSpan, string tag, int capacity, out List<Range> range)
    {
        Console.WriteLine("Parsing body text...");
        string wholeTag = $"\"{tag}\":";
        var tagSpan = wholeTag.AsSpan();

        range = new List<Range>(capacity);

        
        var startIndex = inputSpan.IndexOf(tagSpan);

        do
        {
            inputSpan = inputSpan.Slice(startIndex + tagSpan.Length);
            int stringStartIndex = inputSpan.IndexOf('\"') + 1;

            int stringEndIndex;
            bool gotEnd;

            do
            {
                stringEndIndex = inputSpan.IndexOf('\"');
                gotEnd = inputSpan[stringEndIndex - 1] != '\\';
            } while (!gotEnd);

            int length = stringEndIndex - stringStartIndex;
            range.Add(new Range(stringStartIndex, length));
            startIndex = inputSpan.IndexOf(tagSpan);
        } while (startIndex != -1);
    }
}