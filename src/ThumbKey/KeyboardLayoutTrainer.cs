using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Core;
using Core.Util;
using ThumbKey.Visualization;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer : IEvolverAsexual<TextRange, KeyboardLayout, Key[,]>
{
    // todo: all punctuation in alphabet?
    public static readonly FrozenSet<char> CharacterSetDict = CharacterSet.ToFrozenSet();

    public KeyboardLayoutTrainer(string textPath, string tag, int count, int iterationCount, int seed)
    {
        double[,] positionPreferences = PositionPreferences[Dimensions];
        
        var layouts = new KeyboardLayout[count];
        for (int i = 0; i < count; i++)
        {
            layouts[i] = new KeyboardLayout(Dimensions, CharacterSet, seed + i, UseStandardSpaceBar, positionPreferences, in FitnessWeights, SwipeDirectionPreferences);
        }

        Console.WriteLine($"Reading file at {textPath}");
        var input = File.ReadAllText(textPath);
        EvolutionLoop(iterationCount, input, tag, layouts);
    }
    
    void EvolutionLoop(int iterationCount, string input, string tag, KeyboardLayout[] layouts)
    {
        LayoutVisualizer[] visualizers = layouts.AsParallel().Select(x => new LayoutVisualizer(x)).ToArray();
        
        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
        var inputSpan = input.AsSpan();

        Stopwatch stopwatch = new();
        Console.WriteLine($"Parsing input for tag \"{tag}\"...");
        RedditDataReader.GetAllStringsOfTag(inputSpan, tag, 1000, out var ranges);
        
        for (int i = 0; i < iterationCount; i++)
        {
            if (i % 10 == 0)
            {
                Console.WriteLine("Printing visualizations...");
                foreach (var visualizer in visualizers) visualizer.Visualize();
            }

            Console.WriteLine($"Generation {i + 1}...");
            stopwatch.Start();
            foreach(var range in ranges)
            {
                var inputInfo = new TextRange(input, range);// todo: spans use Range's End as end index instead of length - fix this
                layouts.AsParallel().ForAll(layout => layout.AddStimulus(inputInfo));
            }
            stopwatch.Stop();
            Console.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to process {ranges.Count} ranges");
            stopwatch.Reset();

            double averageFitness = layouts.Average(x => x.Fitness);
            Console.WriteLine("Average fitness of this generation: " + averageFitness.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("Evolving...");
            //  evolve
            EvolveLayouts(ref layouts);
            
            // reset fitness
            foreach(var layout in layouts)
                layout.ResetFitness();
        }
        
        Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<< FINAL RESULTS >>>>>>>>>>>>>>>>>>>>>>>>>");
        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
    }

    static void EvolveLayouts(ref KeyboardLayout[] layouts)
    {
        layouts = layouts.AsParallel().OrderByDescending(layout => layout.Fitness).ToArray();
        
        int quantityToReproduce = (int)Math.Floor(layouts.Length * ReproductionPercentage);
        quantityToReproduce = quantityToReproduce % 2 == 0 ? quantityToReproduce : quantityToReproduce + 1;
        
        int quantityToReplace = layouts.Length - quantityToReproduce;
        
        Debug.Assert(ReproductionPercentage <= 0.5);
        
        double childrenPerCouple = quantityToReplace / (double) quantityToReproduce;

        for (int i = 0; i < layouts.Length; i ++)
        {
            var parent = layouts[i];
            int childCount = (int)childrenPerCouple;

            int childStartIndex = layouts.Length - i - childCount;
            int childEndIndex = childStartIndex + childCount;
            
            childStartIndex = childStartIndex < i ? childStartIndex : i;

            if (childStartIndex > childEndIndex) // we've populated them all!
                break;
            
            // get children from end of array
            Span<KeyboardLayout> childrenToOverwrite = layouts
                .AsSpan()
                .Slice(childStartIndex, childCount);
            
            Reproduce(parent, childrenToOverwrite);
        }
    }

    public static void Reproduce(KeyboardLayout parent, Span<KeyboardLayout> childrenToOverwrite)
    {
        Key[,] parentKeys = parent.Traits;

        foreach (var child in childrenToOverwrite)
        {
            child.OverwriteTraits(parentKeys);
            child.Mutate(MutationFactor);
        }
    }
    
    
}