// See https://aka.ms/new-console-template for more information

using ThumbKey;

Console.WriteLine("Hello, World from ThumbKey!");

foreach (var arg in args)
{
    break;
}

const string path = @"C:\Users\Dom\Downloads\reddit_casual.json";
const string tag = "text";
var thumbKey = new KeyboardLayoutTrainer(path, tag, 100,100,100);