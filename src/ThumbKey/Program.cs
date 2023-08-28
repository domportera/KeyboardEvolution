using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThumbKey;

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";
const string output = "./output.log";

Console.SetOut(new TextAndConsoleWriter(output, append: false));

using Process process = Process.GetCurrentProcess();
process.PriorityClass = ProcessPriorityClass.Idle;

string settingsFilePath = "./settings.json";

TrainerSettings? settings;
if (!File.Exists(settingsFilePath))
{
    settings = new();
    string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    });
    File.WriteAllText(settingsFilePath, json);
}
else
{
    string settingsJson = File.ReadAllText(settingsFilePath);
    settings = JsonSerializer.Deserialize<TrainerSettings>(settingsJson);
}

if (settings == null)
    throw new Exception($"Settings deserialized to null - check {Path.GetFullPath(settingsFilePath)}");

Console.WriteLine($"Reading file at {settings.JsonPath}");
var text = File.ReadAllText(settings.JsonPath);
Console.WriteLine($"Parsing input for tag \"{settings.JsonTag}\"...");
var ranges =
    RedditDataReader.GetAllStringsOfTag(text, settings.JsonTag, settings.MinCommentLength, settings.IgnoredPhrases);

Debug.Assert(ranges != null && ranges.Count > 0);


KeyboardLayoutTrainer.StartTraining(text, ranges, settings);