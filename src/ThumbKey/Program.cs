// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ThumbKey;

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";
const string output = "./output.log";

Console.SetOut(new TextAndConsoleWriter(output, append: false));

Console.WriteLine($"Reading file at {path}");
var text = File.ReadAllText(path);
Console.WriteLine($"Parsing input for tag \"{tag}\"...");
var ranges = RedditDataReader.GetAllStringsOfTag(text, tag);

Debug.Assert(ranges != null && ranges.Count > 0);

Key[,] preset = LayoutPresets.Presets[PresetType.FourColumn];

var thumbKey = new KeyboardLayoutTrainer(text, ranges, 
    count: 200_000,
    generationCount: 1220, 
    entriesPerGeneration: 1000, 
    seed: 1, 
    preset);
    
    