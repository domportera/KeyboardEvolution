// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ThumbKey;

Console.WriteLine("Hello, World from ThumbKey!");

foreach (var arg in args)
{
    break;
}

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";

Console.WriteLine($"Reading file at {path}");
var input = File.ReadAllText(path);
Console.WriteLine($"Parsing input for tag \"{tag}\"...");
RedditDataReader.GetAllStringsOfTag(input, tag, 1000, out var ranges);

Debug.Assert(ranges != null && ranges.Count > 0);

Key[,] preset = LayoutPresets.Instance[PresetType.ThumbKeyEngV4];
// Key[,]? preset = null;
var thumbKey = new KeyboardLayoutTrainer(input, ranges, 1_000_000, 50, 1000, DateTime.UtcNow.Second, preset);