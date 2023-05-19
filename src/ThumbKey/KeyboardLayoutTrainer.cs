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

    public KeyboardLayoutTrainer(string inputText, List<Range> ranges, int count, int generationCount,
        int entriesPerGeneration,
        int seed, int printEveryNGenerations, Key[,]? startingLayout)
    {
        double[,] positionPreferences = PositionPreferences[Dimensions];

        var layouts = new KeyboardLayout[count];

        Vector2Int dimensions = startingLayout == null
            ? Dimensions
            : new(startingLayout.GetLength(0),startingLayout.GetLength(1));

        string charSetString = CharacterSetString.ToHashSet().ToArray().AsSpan().ToString(); // ensure uniqueness
        if (startingLayout != null)
        {
            AddAdditionalCharactersToCharacterSet(ref charSetString, startingLayout);
            var characterSet = charSetString.ToHashSet();
            AddMissingCharactersToLayout(seed, startingLayout, dimensions, characterSet);
        }
        
        foreach(var c in CharacterSetString)
            CharacterFrequencies.AddCharacterIfNotIncluded(c);

        Debug.Assert(positionPreferences.GetLength(0) == dimensions.Y &&
                     positionPreferences.GetLength(1) == dimensions.X);
        
        for (int i = 0; i < count; i++)
        {
            layouts[i] = new KeyboardLayout(dimensions, charSetString.ToFrozenSet(), seed + i, UseStandardSpaceBar,
                positionPreferences, in FitnessWeights, KeysTowardsCenterWeight, SwipeDirectionPreferences,
                startingLayout);
        }

        entriesPerGeneration = entriesPerGeneration <= 0 ? ranges.Count : entriesPerGeneration;
        EvolutionLoop(generationCount, entriesPerGeneration, inputText, ranges, layouts, printEveryNGenerations);
    }

    void AddAdditionalCharactersToCharacterSet(ref string charSetString, Key[,] startingLayout)
    {
        //adds characters from the starting layout that aren't present in our character set
        List<char> charactersToAdd = new();
        foreach (Key key in startingLayout)
        {
            foreach (char c in key.Characters)
            {
                if (charSetString.Contains(c)) continue;

                charactersToAdd.Add(c);
            }
        }

        ReadOnlySpan<char> chars = charactersToAdd.ToArray().AsSpan();
        charSetString += chars.ToString();
    }

    static void AddMissingCharactersToLayout(int seed, Key[,] startingLayout, Vector2Int dimensions, IReadOnlySet<char> characterSet)
    {
        // add any missing characters from CharacterSet to the starting layout
        List<char> missingCharacters = new();
        foreach (char c in characterSet)
        {
            bool found = false;
            for (int y = 0; y < dimensions.Y; y++)
            for (int x = 0; x < dimensions.X; x++)
            {
                Key key = startingLayout[y, x];
                if (!key.Contains(c)) continue;

                found = true;
                break;
            }

            if (!found)
            {
                missingCharacters.Add(c);
            }
        }

        int missingCharacterCount = missingCharacters.Count;
        int missingCharactersPerKey = missingCharacterCount / startingLayout.Length;
        int remainder = missingCharacterCount % startingLayout.Length;

        if (missingCharacterCount == 0)
            return;

        Random random = new(seed);

        // add missing keys to the starting layout
        for (int y = 0; y < Dimensions.Y; y++)
        for (int x = 0; x < Dimensions.X; x++)
        {
            Key key = startingLayout[y, x];
            int missingCharactersForThisKey = missingCharactersPerKey;
            if (remainder > 0)
            {
                missingCharactersForThisKey++;
                remainder--;
            }

            for (int i = 0; i < missingCharactersForThisKey; i++)
            {
                key.TryAddCharacter(missingCharacters[^1], random);
                missingCharacters.RemoveAt(missingCharacters.Count - 1);
            }
        }
        
        Debug.Assert(missingCharacters.Count == 0);
    }

    static void EvolutionLoop(int generationCount, int entriesPerGeneration, string input, List<Range> ranges,
        KeyboardLayout[] layouts, int printEveryNGenerations)
    {
        LayoutVisualizer[] visualizers = layouts.AsParallel().Select(x => new LayoutVisualizer(x)).ToArray();

        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
        var inputSpan = input.AsSpan();

        Stopwatch stopwatch = new();

        var inputInfo = new TextRange(input, default);

        foreach (var layout in layouts)
            layout.SetStimulus(inputInfo);

        int whichRange = 0;
        for (int i = 0; i < generationCount; i++)
        {
            if (i % printEveryNGenerations == printEveryNGenerations - 1)
            {
                Console.WriteLine("Printing visualizations...");
                foreach (var visualizer in visualizers) visualizer.Visualize();
            }

            // reset fitness
            foreach (var layout in layouts)
                layout.ResetFitness();

            Console.WriteLine($"Generation {i + 1}...");
            stopwatch.Start();
            for (int j = 0; j < entriesPerGeneration; j++)
            {
                whichRange++;
                if (whichRange >= ranges.Count)
                    whichRange = 0;
                var range = ranges[whichRange];
                inputInfo.Range = range;
                layouts.AsParallel().ForAll(x => x.Evaluate());
            }

            stopwatch.Stop();
            Console.WriteLine(
                $"Took {stopwatch.ElapsedMilliseconds}ms to process {entriesPerGeneration} entries for {layouts.Length} layouts\n" +
                $"{stopwatch.ElapsedMilliseconds / (double)layouts.Length}ms per layout for {entriesPerGeneration * layouts.Length} calculations total\n");
            stopwatch.Reset();

            PrintAverageFitness();
            Console.WriteLine("Evolving...");
            //  evolve
            EvolveLayouts(ref layouts);
        }

        Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<< FINAL RESULTS >>>>>>>>>>>>>>>>>>>>>>>>>");
        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
        PrintAverageFitness();

        void PrintAverageFitness()
        {
            double averageFitness = layouts.Average(x => x.Fitness);
            Console.WriteLine("Average fitness of this generation: " +
                              averageFitness.ToString(CultureInfo.InvariantCulture));
        }
    }

    static void EvolveLayouts(ref KeyboardLayout[] layouts)
    {
        layouts = layouts.AsParallel().OrderByDescending(layout => layout.Fitness).ToArray();

        int quantityToReproduce = (int)Math.Floor(layouts.Length * ReproductionPercentage);
        quantityToReproduce = quantityToReproduce % 2 == 0 ? quantityToReproduce : quantityToReproduce + 1;

        int quantityToReplace = layouts.Length - quantityToReproduce;

        Debug.Assert(ReproductionPercentage <= 0.5);

        double childrenPerCouple = quantityToReplace / (double)quantityToReproduce;

        double averageFitnessReproductivePopulation = layouts[0..quantityToReproduce].Average(x => x.Fitness);
        double averageFitnessNonReproductivePopulation = layouts[quantityToReproduce..].Average(x => x.Fitness);
        Console.WriteLine(
            $"Average fitness of reproductive population: {averageFitnessReproductivePopulation} vs {averageFitnessNonReproductivePopulation} for non-reproductive");
        Console.WriteLine($"Reproductive population: {quantityToReproduce} with {childrenPerCouple} children each");

        for (int i = 0; i < quantityToReproduce; i++)
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