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
var text = File.ReadAllText(path);
Console.WriteLine($"Parsing input for tag \"{tag}\"...");
var ranges = RedditDataReader.GetAllStringsOfTag(text, tag);

Debug.Assert(ranges != null && ranges.Count > 0);

Key[,] preset = LayoutPresets.Instance[PresetType.ThumbKeyEngV4];

Console.WriteLine("Generating layouts");
var thumbKey = new KeyboardLayoutTrainer(text, ranges, 
    count: 1_000_000, 
    generationCount: 300, 
    entriesPerGeneration: 1000, 
    seed: DateTime.UtcNow.Second, 
    preset);
    
    