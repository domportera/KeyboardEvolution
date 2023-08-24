using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Core.Util;
using ThumbKey.Visualization;

namespace ThumbKey;

public static partial class KeyboardLayoutTrainer 
{
    // todo: all punctuation in alphabet?

    static void StartTraining(string inputText, List<Range> ranges, int count, int generationCount,
        int entriesPerGeneration,
        int seed, Key[,]? startingLayout, Array2DCoords dimensions)
    {

        if(startingLayout!= null)
            dimensions = new Array2DCoords(columnX: startingLayout.GetLength(1), 
                rowY: startingLayout.GetLength(0));
        
        float[,] positionPreferences = PositionPreferences[dimensions];

        var layouts = new KeyboardLayout[count];

        float[] swipeDirectionPreferences = new float[9];
        swipeDirectionPreferences[(int)SwipeDirection.Center] = CenterPreference;
        swipeDirectionPreferences[(int)SwipeDirection.Up] = CardinalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.Down] = CardinalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.Left] = CardinalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.Right] = CardinalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.UpLeft] = DiagonalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.UpRight] = DiagonalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.DownLeft] = DiagonalPreference;
        swipeDirectionPreferences[(int)SwipeDirection.DownRight] = DiagonalPreference;
        
        

        string charSetString = CharacterSetString.ToHashSet().ToArray().AsSpan().ToString(); // ensure uniqueness
        if (startingLayout != null)
        {
            AddAdditionalCharactersToCharacterSet(ref charSetString, startingLayout);
            var characterSet = charSetString.ToHashSet();
            AddMissingCharactersToLayout(seed, startingLayout, dimensions, characterSet);
        }

        foreach (var c in CharacterSetString)
            CharacterFrequencies.AddCharacterIfNotIncluded(c);

        var charSet = charSetString.ToFrozenSet();
        var keySpecificSwipeDirectionPreferences =
            GenerateKeySpecificSwipeDirections(swipeDirectionPreferences, dimensions);


        var controlLayout = new KeyboardLayout(dimensions, charSet, seed, UseStandardSpaceBar,
            positionPreferences, in FitnessWeights, keySpecificSwipeDirectionPreferences, swipeDirectionPreferences,
            startingLayout);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine("Generating layouts");
        int partitionCount = 16;
        var customPartitioner = Partitioner.Create(0, count, count / partitionCount);
        Parallel.ForEach(customPartitioner, tuple =>
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                layouts[i] = new KeyboardLayout(dimensions, charSet, seed + i, UseStandardSpaceBar,
                    positionPreferences, in FitnessWeights, keySpecificSwipeDirectionPreferences,
                    swipeDirectionPreferences,
                    null);
            }
        });

        stopwatch.Stop();
        Console.WriteLine(
            $"Created {count} layouts in {stopwatch.Elapsed.Seconds}.{stopwatch.Elapsed.Milliseconds} seconds");

        var random = new Random(seed);
        random.Shuffle(ranges);
        entriesPerGeneration = entriesPerGeneration <= 0 ? ranges.Count : entriesPerGeneration;
        EvolutionLoop(generationCount, entriesPerGeneration, inputText, ranges, layouts, controlLayout);
    }

    static void AddAdditionalCharactersToCharacterSet(ref string charSetString, Key[,] startingLayout)
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

    static void AddMissingCharactersToLayout(int seed, Key[,] startingLayout, Array2DCoords coords,
        IReadOnlySet<char> characterSet)
    {
        // add any missing characters from CharacterSet to the starting layout
        List<char> missingCharacters = new();
        foreach (char c in characterSet)
        {
            bool found = false;
            for (int y = 0; y < coords.RowY; y++)
            for (int x = 0; x < coords.ColumnX; x++)
            {
                var index2d = new Array2DCoords(x, y);
                Key key = startingLayout.Get(index2d);
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

        if (missingCharacterCount == 0)
            return;

        Random random = new(seed);
        List<Key> allKeys = new();
        foreach (Key key in startingLayout)
            allKeys.Add(key);

        random.Shuffle(allKeys);
        
        while(missingCharacters.Count > 0)
        {
            foreach (Key key in allKeys)
            {
                var added = key.TryAddCharacter(missingCharacters[^1], random);
                if (added)
                {
                    missingCharacters.RemoveAt(missingCharacters.Count - 1);
                    if (missingCharacters.Count == 0)
                        break;
                }
            }
        }
    }

    static void EvolutionLoop(int generationCount, int entriesPerGeneration, string input, List<Range> ranges,
        KeyboardLayout[] layouts, KeyboardLayout controlLayout)
    {
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

        int processorCount = Environment.ProcessorCount;
        int partitionSize = layouts.Length / processorCount;
        
        if (partitionSize == 0)
            throw new Exception($"Layout quantity should be considerably higher than processor count {processorCount}");

        var customPartitioner = Partitioner.Create(0, layouts.Length, partitionSize);

        for (int i = 0; i < generationCount; i++)
        {
            Console.WriteLine($"\n\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\nGeneration {i + 1}\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            stopwatch.Start();
            List<Range> thisRange = entriesPerGeneration == ranges.Count ? ranges : GetRangeForThisGeneration(entriesPerGeneration, ranges, i);
            
            layouts.AsParallel().ForAll(layout => layout.Evaluate(thisRange));
            //var myLayouts = layouts;
            //Parallel.ForEach(customPartitioner, (indexRange, _) =>
            //{
            //    int min = indexRange.Item1;
            //    int max = indexRange.Item2;
            //    for(int index = min; index < max; index++)
            //        myLayouts[index].Evaluate(thisRange);
            //});

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
            _sortStopwatch.Start();
            layouts = layouts.AsParallel().OrderByDescending(layout => layout.Fitness).ToArray();
            _sortStopwatch.Stop();
            Console.WriteLine($"Took {_sortStopwatch.ElapsedMilliseconds}ms to sort layouts");
            _sortStopwatch.Reset();
            PrintFitnessReport();

            Console.WriteLine("\n\nBest layout this generation:");
            visualizers[layouts[0]].Visualize();
        }

        void PrintFitnessReport()
        {
            double averageFitness = layouts.Average(x => x.Fitness);

            Console.WriteLine(
                $"Average fitness ({averageFitness:f5}) {(averageFitness > controlFitness ? ">" : "<")} than control {controlFitness:f3}\n" +
                $"and {(averageFitness > previousAverageFitness ? ">" : "<")} than previous {previousAverageFitness:f3}\n");
            previousAverageFitness = averageFitness;

            var bestFitness = layouts[0].Fitness;
            Console.WriteLine(
                $"Best fitness: {bestFitness:f5} is {(bestFitness > controlFitness ? "greater" : "less")} than {controlFitness:f5} for control layout\n" +
                $"and is {(bestFitness > previousBestFitness ? "greater" : "less")} than that of previous generation");
            previousBestFitness = bestFitness;
        }

        List<Range> GetRangeForThisGeneration(int entriesPerGeneration1, List<Range> list, int i)
        {
            var minRange = (i * entriesPerGeneration1) % list.Count;
            var maxRange = ((i + 1) * entriesPerGeneration1) % list.Count;
            if (maxRange < minRange)
            {
                minRange = 0; 
                maxRange = entriesPerGeneration1;
            }

            var thisRange = list[minRange..maxRange];
            return thisRange;
        }
    }
    static Stopwatch _sortStopwatch = new();
    static readonly List<ReproductionGroup> ReproductionGroups = new();
    static double _previousDelta = 0;

    static void EvolveLayouts(KeyboardLayout[] layoutsSortedDescending)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        int quantityToReproduce = (int)Math.Floor(layoutsSortedDescending.Length * ReproductionRatio);
        if (quantityToReproduce == 0)
            quantityToReproduce = 1;

        int quantityToReplace = layoutsSortedDescending.Length - quantityToReproduce;

        Debug.Assert(ReproductionRatio <= 0.5);

        float childrenPerParent = quantityToReplace / (float)quantityToReproduce;

        double averageFitnessReproductivePopulation =
            layoutsSortedDescending[0..quantityToReproduce].Average(x => x.Fitness);
        double averageFitnessNonReproductivePopulation =
            layoutsSortedDescending[quantityToReproduce..].Average(x => x.Fitness);
        double delta = averageFitnessReproductivePopulation - averageFitnessNonReproductivePopulation;
        Console.WriteLine(
            $"Average fitness of reproductive population: {averageFitnessReproductivePopulation:f3} vs {averageFitnessNonReproductivePopulation:f3} for non-reproductive");
        Console.WriteLine($"Reproductive population: {quantityToReproduce} with {childrenPerParent} children each");
        Console.WriteLine($"Delta: {delta:f3} is {(delta > _previousDelta ? '>' : '<')} {_previousDelta:f3}");
        _previousDelta = delta;

        // Generate reproduction groups
        ReproductionGroups.Clear();
        for (int i = 0; i < quantityToReproduce; i++)
        {
            var parent = layoutsSortedDescending[i];
            int childCount = (int)childrenPerParent;

            int childStartIndex = layoutsSortedDescending.Length - (i * childCount) - childCount;
            int childEndIndex = childStartIndex + childCount;

            var lastIteration = childStartIndex < quantityToReproduce;
            if (lastIteration)
                childStartIndex = quantityToReproduce;

            if (childStartIndex > childEndIndex) // we've populated them all!
                break;

            Debug.Assert(childStartIndex >= quantityToReproduce);
            var range = new Range(childStartIndex, childEndIndex);
            ReproductionGroups.Add(new ReproductionGroup(parent, range));

            if (lastIteration)
                break;
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

        var random = parent.Random;
        float multiplicationFactor = 1f;
        
        foreach (var child in childrenToOverwrite)
        {
            child.OverwriteTraits(parentKeys);

            if (UseRandomMutation)
            {
                multiplicationFactor = random.NextSingle();
                if(SqrtRandomMutation)
                    multiplicationFactor = MathF.Sqrt(multiplicationFactor);
            }
            
            float mutationFactor = MutationFactor * multiplicationFactor;
            child.Mutate(mutationFactor);
        }
    }

    static float[,][] GenerateKeySpecificSwipeDirections(float[] swipeDirectionPreferences, Array2DCoords coords)
    {
        var keySpecificSwipeDirections = new float[coords.RowY, coords.ColumnX][];
        for (int y = 0; y < coords.RowY; y++)
        {
            for (int x = 0; x < coords.ColumnX; x++)
            {
                // Identifying the keys position on the keyboard.
                bool isLeftEdge = x == 0;
                bool isRightEdge = x == coords.ColumnX - 1;
                bool isTopEdge = y == 0;
                bool isBottomEdge = y == coords.RowY - 1;

                // Initialize a new dictionary to store swipe preferences based on position.
                var positionBasedSwipePreferences = new Dictionary<SwipeDirection, float>();
                
                for(int i = 0; i < swipeDirectionPreferences.Length; i++)
                    positionBasedSwipePreferences[(SwipeDirection)i] = swipeDirectionPreferences[i];

                positionBasedSwipePreferences[SwipeDirection.Center] *= (1 + KeysTowardsCenterWeight);

                if (isLeftEdge && isTopEdge) // Top-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isRightEdge && isTopEdge) // Top-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isLeftEdge && isBottomEdge) // Bottom-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isRightEdge && isBottomEdge) // Bottom-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isLeftEdge) // Left edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isRightEdge) // Right edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isTopEdge) // Top edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + KeysTowardsCenterWeight);
                }
                else if (isBottomEdge) // Bottom edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + KeysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + KeysTowardsCenterWeight);
                }

                var preferenceArray = new float[swipeDirectionPreferences.Length];
                for (int i = 0; i < preferenceArray.Length; i++)
                    preferenceArray[i] = positionBasedSwipePreferences[(SwipeDirection)i];
                
                keySpecificSwipeDirections.Set(new(x, y), preferenceArray);
            }
        }

        return keySpecificSwipeDirections;
    }
}