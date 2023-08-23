using System.Diagnostics;
using ThumbKey;

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";
const string output = "./output.log";

Console.SetOut(new TextAndConsoleWriter(output, append: false));

Console.WriteLine($"Reading file at {path}");
var text = File.ReadAllText(path);
Console.WriteLine($"Parsing input for tag \"{tag}\"...");
var ranges = RedditDataReader.GetAllStringsOfTag(text, tag, minTextLength: 3);

Debug.Assert(ranges != null && ranges.Count > 0);


KeyboardLayoutTrainer.Start(text, ranges);


    
    