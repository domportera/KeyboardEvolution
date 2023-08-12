using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Core;
using Core.Util;
using ThumbKey.Visualization;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer : IEvolverAsexual<TextRange, KeyboardLayout, Key[,]>
{
    // todo: all punctuation in alphabet?

    public KeyboardLayoutTrainer(string inputText, List<Range> ranges, int count, int generationCount,
        int entriesPerGeneration,
        int seed, Key[,]? startingLayout)
    {
        float[,] positionPreferences = PositionPreferences[Dimensions];

        var layouts = new KeyboardLayout[count];

        Vector2Int dimensions = startingLayout == null
            ? Dimensions
            : new(startingLayout.GetLength(0), startingLayout.GetLength(1));

        string charSetString = CharacterSetString.ToHashSet().ToArray().AsSpan().ToString(); // ensure uniqueness
        if (startingLayout != null)
        {
            AddAdditionalCharactersToCharacterSet(ref charSetString, startingLayout);
            var characterSet = charSetString.ToHashSet();
            AddMissingCharactersToLayout(seed, startingLayout, dimensions, characterSet);
        }

        foreach (var c in CharacterSetString)
            CharacterFrequencies.AddCharacterIfNotIncluded(c);

        Debug.Assert(positionPreferences.GetLength(0) == dimensions.Y &&
                     positionPreferences.GetLength(1) == dimensions.X);

        var charSet = charSetString.ToFrozenSet();
        var keySpecificSwipeDirectionPreferences =
            GenerateKeySpecificSwipeDirections(KeysTowardsCenterWeight, SwipeDirectionPreferences);
        

        var controlLayout = new KeyboardLayout(dimensions, charSet, seed, UseStandardSpaceBar,
            positionPreferences, in FitnessWeights, keySpecificSwipeDirectionPreferences, SwipeDirectionPreferences,
            startingLayout);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        int partitionCount = 16;
        var customPartitioner = Partitioner.Create(0, count, count / partitionCount);
        Parallel.ForEach(customPartitioner, tuple =>
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                layouts[i] = new KeyboardLayout(dimensions, charSet, seed + i, UseStandardSpaceBar,
                    positionPreferences, in FitnessWeights, keySpecificSwipeDirectionPreferences, SwipeDirectionPreferences,
                    null);
            }
        });

        stopwatch.Stop();
        Console.WriteLine(
            $"Created {count} layouts in {stopwatch.Elapsed.Seconds}.{stopwatch.Elapsed.Milliseconds} seconds");

        entriesPerGeneration = entriesPerGeneration <= 0 ? ranges.Count : entriesPerGeneration;
        EvolutionLoop(generationCount, entriesPerGeneration, inputText, ranges, layouts, controlLayout);
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

    static void AddMissingCharactersToLayout(int seed, Key[,] startingLayout, Vector2Int dimensions,
        IReadOnlySet<char> characterSet)
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
        KeyboardLayout[] layouts, KeyboardLayout controlLayout)
    {
        ranges = ranges[..entriesPerGeneration];
        var visualizers =
            layouts.AsParallel().Select(x => new LayoutVisualizer(x)).ToFrozenDictionary(x => x.LayoutToVisualize);

        var controlVisualizer = new LayoutVisualizer(controlLayout);
        //visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());

        Stopwatch stopwatch = new();

        var inputInfo = new TextRange(input, default);

        foreach (var layout in layouts)
            layout.SetStimulus(inputInfo);

        controlLayout.SetStimulus(inputInfo);

        Console.WriteLine($"CONTROL");
        controlLayout.Evaluate(ranges);
        controlVisualizer.Visualize();
        var controlFitness = controlLayout.Fitness;

        var previousAverageFitness = controlFitness;
        var previousBestFitness = controlFitness;

        for (int i = 0; i < generationCount; i++)
        {
            Console.WriteLine($"Generation {i + 1}...");
            stopwatch.Start();
            layouts.AsParallel().ForAll(x => x.Evaluate(ranges));

            stopwatch.Stop();
            Console.WriteLine(
                $"Took {stopwatch.ElapsedMilliseconds}ms to process {entriesPerGeneration} entries for {layouts.Length} layouts\n" +
                $"{stopwatch.ElapsedMilliseconds / (double)layouts.Length}ms per layout for {entriesPerGeneration * layouts.Length} calculations total\n");
            stopwatch.Reset();

            HandleResults();

            Console.WriteLine("Evolving...");
            EvolveLayouts(layouts);
        }

        Console.WriteLine("---------------------------------------------------------------------");
        Console.WriteLine("\n\n<<<<<<<<<<<<<<<<<<<<<<<<< FINAL RESULTS >>>>>>>>>>>>>>>>>>>>>>>>>\n\n");
        Console.WriteLine("---------------------------------------------------------------------");

        HandleResults();

        void HandleResults()
        {
            layouts = layouts.OrderByDescending(layout => layout.Fitness).ToArray();
            PrintFitnessReport();

            Console.WriteLine("\n\nBest layout this generation:");
            visualizers[layouts[0]].Visualize();
        }

        void PrintFitnessReport()
        {
            float averageFitness = layouts.Average(x => x.Fitness);

            Console.WriteLine(
                $"Average fitness ({averageFitness:f3}) {(averageFitness > controlFitness ? ">" : "<")} than control {controlFitness:f3}\n" +
                $"and {(averageFitness > previousAverageFitness ? ">" : "<")} than previous {previousAverageFitness:f3}\n");
            previousAverageFitness = averageFitness;

            var bestFitness = layouts[0].Fitness;
            Console.WriteLine(
                $"Best fitness: {bestFitness:f3} is {(bestFitness > controlFitness ? "greater" : "less")} than {controlFitness:f3} for control layout\n" +
                $"and is {(bestFitness > previousBestFitness ? "greater" : "less")} than that of previous generation");
            previousBestFitness = bestFitness;
        }
    }

    static readonly List<ReproductionGroup> ReproductionGroups = new();
    static float _previousDelta = 0;

    static void EvolveLayouts(KeyboardLayout[] layoutsSortedDescending)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        int quantityToReproduce = (int)Math.Floor(layoutsSortedDescending.Length * ReproductionRatio);
        if (quantityToReproduce == 0)
            quantityToReproduce = 1;

        int quantityToReplace = layoutsSortedDescending.Length - quantityToReproduce;

        Debug.Assert(ReproductionRatio <= 0.5);

        float childrenPerCouple = quantityToReplace / (float)quantityToReproduce;

        float averageFitnessReproductivePopulation =
            layoutsSortedDescending[0..quantityToReproduce].Average(x => x.Fitness);
        float averageFitnessNonReproductivePopulation =
            layoutsSortedDescending[quantityToReproduce..].Average(x => x.Fitness);
        float delta = averageFitnessReproductivePopulation - averageFitnessNonReproductivePopulation;
        Console.WriteLine(
            $"Average fitness of reproductive population: {averageFitnessReproductivePopulation:f3} vs {averageFitnessNonReproductivePopulation:f3} for non-reproductive");
        Console.WriteLine($"Reproductive population: {quantityToReproduce} with {childrenPerCouple} children each");
        Console.WriteLine($"Delta: {delta:f3} is {(delta > _previousDelta ? '>' : '<')} {_previousDelta:f3}");
        _previousDelta = delta;

        // Generate reproduction groups
        ReproductionGroups.Clear();
        for (int i = 0; i < quantityToReproduce; i++)
        {
            var parent = layoutsSortedDescending[i];
            int childCount = (int)childrenPerCouple;

            int childStartIndex = layoutsSortedDescending.Length - i - childCount;
            int childEndIndex = childStartIndex + childCount;

            childStartIndex = childStartIndex < i ? childStartIndex : i;

            if (childStartIndex > childEndIndex) // we've populated them all!
                break;

            var range = new Range(childStartIndex, childEndIndex);
            ReproductionGroups.Add(new ReproductionGroup(parent, range));

            // get children from end of array
            Span<KeyboardLayout> childrenToOverwrite = layoutsSortedDescending
                .AsSpan()
                .Slice(childStartIndex, childCount);

            Reproduce(parent, childrenToOverwrite);
        }

        // now we have a list of parents and their children
        // we need to use the parents to overwrite the children and then mutate them
        ReproductionGroups.AsParallel().ForAll(x =>
        {
            var parent = x.Parent;
            var children = layoutsSortedDescending.AsSpan()[x.Children];
            Reproduce(parent, children);
        });

        stopwatch.Stop();
        Console.WriteLine(
            $"Took {stopwatch.ElapsedMilliseconds}ms to reproduce {quantityToReproduce} parents and {quantityToReplace} children");
    }

    readonly struct ReproductionGroup
    {
        public readonly KeyboardLayout Parent;
        public readonly Range Children;

        public ReproductionGroup(KeyboardLayout parent, Range children)
        {
            Parent = parent;
            Children = children;
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

    static FrozenDictionary<SwipeDirection, float>[,] GenerateKeySpecificSwipeDirections(float keysTowardsCenterWeight,
        IReadOnlyDictionary<SwipeDirection, float> swipeDirectionPreferences)
    {
        var keySpecificSwipeDirections = new FrozenDictionary<SwipeDirection, float>[Dimensions.Y, Dimensions.X];
        for (int y = 0; y < Dimensions.Y; y++)
        {
            for (int x = 0; x < Dimensions.X; x++)
            {
                // Identifying the keys position on the keyboard.
                bool isLeftEdge = x == 0;
                bool isRightEdge = x == Dimensions.X - 1;
                bool isTopEdge = y == 0;
                bool isBottomEdge = y == Dimensions.Y - 1;

                // Initialize a new dictionary to store swipe preferences based on position.
                var positionBasedSwipePreferences = new Dictionary<SwipeDirection, float>(swipeDirectionPreferences);

                positionBasedSwipePreferences[SwipeDirection.Center] *= (1 + keysTowardsCenterWeight);

                if (isLeftEdge && isTopEdge) // Top-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge && isTopEdge) // Top-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isLeftEdge && isBottomEdge) // Bottom-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge && isBottomEdge) // Bottom-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isLeftEdge) // Left edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge) // Right edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isTopEdge) // Top edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isBottomEdge) // Bottom edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                }

                keySpecificSwipeDirections[y, x] =
                    positionBasedSwipePreferences.ToFrozenDictionary(x => x.Key, x => x.Value);
            }
        }

        return keySpecificSwipeDirections;
    }
}