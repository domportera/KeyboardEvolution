// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ThumbKey;

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";

Console.WriteLine($"Reading file at {path}");
var text = File.ReadAllText(path);
Console.WriteLine($"Parsing input for tag \"{tag}\"...");
var ranges = RedditDataReader.GetAllStringsOfTag(text, tag);

Debug.Assert(ranges != null && ranges.Count > 0);

Key[,] preset = null;//LayoutPresets.Instance[PresetType.ThumbKeyEngV4];

var thumbKey = new KeyboardLayoutTrainer(text, ranges, 
    count: 100_000,
    generationCount: 220, 
    entriesPerGeneration: 1000, 
    seed: 1, 
    preset);
    
    