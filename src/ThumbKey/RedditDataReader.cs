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
    public static void GetAllStringsOfTag(ReadOnlySpan<char> inputSpan, string tag, int capacity, out List<Range> range)
    {
        Console.WriteLine("Parsing body text...");
        string wholeTag = $"\"{tag}\":";
        var tagSpan = wholeTag.AsSpan();

        range = new List<Range>(capacity);

        
        var startIndex = inputSpan.IndexOf(tagSpan);

        do
        {
            startIndex += tagSpan.Length;
            inputSpan = inputSpan.Slice(startIndex);
            
            int length;
            bool gotEnd;

            do
            {
                length = inputSpan.IndexOf('\"');
                gotEnd = inputSpan[length - 1] != '\\';
            } while (!gotEnd);

            range.Add(new Range(startIndex, startIndex + length));
            startIndex = inputSpan.IndexOf(tagSpan);
        } while (startIndex != -1);
    }
}