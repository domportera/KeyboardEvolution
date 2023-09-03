using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Core.Util;
using ThumbKey.Visualization;

namespace ThumbKey;

public static class KeyboardLayoutTrainer
{
    // todo: all punctuation in alphabet?

    public static void StartTraining(char[] inputText, List<Range> ranges, TrainerSettings settings)
    {
        Console.WriteLine($"Starting training with {ranges.Count} entries");
        ApplySettings(settings);
        int seed = settings.Seed;
        int entriesPerGeneration = settings.EntriesPerGeneration;
        int generationCount = settings.GenerationCount;
        int count = settings.TotalCount;
        
        // write settings above to console
        string countSettingsLog = $"Total count: {count}\n" +
                                  $"Parent count: {settings.ParentCount}\n" +
                                  $"Children per parent: {settings.ChildrenPerParent}\n" +
                                  $"Entries per generation: {entriesPerGeneration}\n" +
                                  $"Generation count: {generationCount}\n" +
                                  $"Seed: {seed}\n" +
                                  $"Preset type: {settings.PresetType}\n" +
                                  $"Character set: {CharacterSetString}\n" +
                                  $"Use standard spacebar: {UseStandardSpaceBar}\n" +
                                  $"Use random mutation: {UseRandomMutation}\n" +
                                  $"Mutation factor: {MutationFactor}\n" +
                                  $"Mutation exponent: {MutationExponent}\n" +
                                  $"Reproduction ratio: {ReproductionRatio}\n" +
                                  $"Use key-specific swipe direction preferences: {UseKeySpecificSwipeDirectionPreferences}\n" +
                                  $"Redistribute key characters based on frequency: {RedistributeKeyCharactersBasedOnFrequency}\n" +
                                  $"Allow cardinal-diagonal swaps: {AllowCardinalDiagonalSwaps}\n" +
                                  $"Fitness weights: {FitnessWeights}\n" +
                                  $"Keys towards center weight: {_keysTowardsCenterWeight}\n" +
                                  $"Cardinal preference: {CardinalPreference}\n" +
                                  $"Diagonal preference: {DiagonalPreference}\n" +
                                  $"Center preference: {CenterPreference}\n";
        
        Console.WriteLine(countSettingsLog);

        var startingLayout = LayoutPresets.Presets[settings.PresetType];
        if (seed == -1)
            seed = (int)(DateTime.UtcNow.TimeOfDay.TotalSeconds);

        var dimensions = new Array2DCoords(columnX: startingLayout.GetLength(1),
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
        int partitionCount = Environment.ProcessorCount;
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

        while (missingCharacters.Count > 0)
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

    static void EvolutionLoop(int generationCount, int entriesPerGeneration, char[] input, List<Range> ranges,
        KeyboardLayout[] layouts, KeyboardLayout controlLayout)
    {
        var visualizers =
            layouts.AsParallel().Select(x => new LayoutVisualizer(x)).ToFrozenDictionary(x => x.LayoutToVisualize);

        var controlVisualizer = new LayoutVisualizer(controlLayout);
        //visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());

        Stopwatch stopwatch = new();


        foreach (var layout in layouts)
            layout.SetStimulus(input);

        controlLayout.SetStimulus(input);

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

        bool useAllEntriesPerGeneration = entriesPerGeneration == ranges.Count;
        for (int i = 0; i < generationCount; i++)
        {
            Console.WriteLine(
                $"\n\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\nGeneration {i + 1}\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            stopwatch.Start();
            
            List<Range> thisRange = useAllEntriesPerGeneration
                ? ranges
                : GetRangeForThisGeneration(entriesPerGeneration, ranges, i);

            layouts.AsParallel().ForAll(layout => layout.Evaluate(thisRange));
            
            stopwatch.Stop();
            Console.WriteLine(
                $"Took {stopwatch.ElapsedMilliseconds}ms to process {entriesPerGeneration} entries for {layouts.Length} layouts\n" +
                $"{stopwatch.ElapsedMilliseconds / (double)layouts.Length}ms per layout for {(long)entriesPerGeneration * layouts.Length} calculations total\n");
            stopwatch.Reset();

            if (!useAllEntriesPerGeneration)
            {
                Console.WriteLine($"Calculating control layout fitness for this generation's entries");
                controlLayout.ResetFitness();
                controlLayout.Evaluate(thisRange);
                controlFitness = controlLayout.Fitness;
            }

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
                $"Average fitness ({averageFitness:f4}) {(averageFitness > controlFitness ? ">" : "<")} than control  ({controlFitness:f4})\n" +
                $"Average fitness ({averageFitness:f4}) {(averageFitness > previousAverageFitness ? ">" : "<")} than previous ({previousAverageFitness:f4})\n");
            previousAverageFitness = averageFitness;

            var bestFitness = layouts[0].Fitness;
            Console.WriteLine(
                $"Best fitness:   ({bestFitness:f4}) {(bestFitness > controlFitness ? ">" : "<")} than control  ({controlFitness:f4})\n" +
                $"Best fitness:   ({bestFitness:f4}) {(bestFitness > previousBestFitness ? ">" : "<")} than previous ({previousBestFitness:f4})\n" +
                $"Improvement over control:  {((bestFitness - controlFitness)/controlFitness * 100):f6} %\n" +
                $"Improvement over previous: {((bestFitness - previousBestFitness)/previousBestFitness * 100):f6} %");
            
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
            $"Average fitness reproductive:     {averageFitnessReproductivePopulation:f4}\n" +
            $"AVerage fitness non-reproductive: {averageFitnessNonReproductivePopulation:f4}");
        Console.WriteLine($"Delta: {delta:f4} is {(delta > _previousDelta ? '>' : '<')} {_previousDelta:f4}");
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

                if (MathF.Abs(MutationExponent - 1f) > 0.0001f)
                    multiplicationFactor = MathF.Pow(multiplicationFactor, MutationExponent);
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

                for (int i = 0; i < swipeDirectionPreferences.Length; i++)
                    positionBasedSwipePreferences[(SwipeDirection)i] = swipeDirectionPreferences[i];

                positionBasedSwipePreferences[SwipeDirection.Center] *= (1 + _keysTowardsCenterWeight);

                if (isLeftEdge && isTopEdge) // Top-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isRightEdge && isTopEdge) // Top-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isLeftEdge && isBottomEdge) // Bottom-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isRightEdge && isBottomEdge) // Bottom-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isLeftEdge) // Left edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isRightEdge) // Right edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isTopEdge) // Top edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + _keysTowardsCenterWeight);
                }
                else if (isBottomEdge) // Bottom edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + _keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + _keysTowardsCenterWeight);
                }

                var preferenceArray = new float[swipeDirectionPreferences.Length];
                for (int i = 0; i < preferenceArray.Length; i++)
                    preferenceArray[i] = positionBasedSwipePreferences[(SwipeDirection)i];

                keySpecificSwipeDirections.Set(new(x, y), preferenceArray);
            }
        }

        return keySpecificSwipeDirections;
    }

    static Weights FitnessWeights;
    static float _keysTowardsCenterWeight = 0.1f; //prefer swiping towards the center of the keyboard

    public static float
        CardinalPreference = 0.4f; //weight of the hard-coded swipe types defined below (cardinal, diagonal, center)

    public static float DiagonalPreference = 0f;
    public static float CenterPreference = 1f;
    static bool UseStandardSpaceBar = true;
    static bool UseRandomMutation = true;
    static float MutationFactor = 0.1f;
    static float MutationExponent = 1f;
    static float ReproductionRatio = 0.1f;
    static string CharacterSetString = "abcdefghijklmnopqrstuvwxyz'";
    public static bool AllowCardinalDiagonalSwaps = true;
    public static bool UseKeySpecificSwipeDirectionPreferences = true;
    public static bool RedistributeKeyCharactersBasedOnFrequency = false;
    public static bool PreferSwipesInOppositeDirectionOfNextKey = true;
    public static Dictionary<Array2DCoords, float[,]> PositionPreferences;

    static void ApplySettings(TrainerSettings settings)
    {
        FitnessWeights = new(settings.FitnessWeights);
        _keysTowardsCenterWeight = settings.KeysTowardsCenterWeight;
        CardinalPreference = settings.CardinalPreference;
        DiagonalPreference = settings.DiagonalPreference;
        CenterPreference = settings.CenterPreference;
        UseStandardSpaceBar = settings.UseStandardSpaceBar;
        UseRandomMutation = settings.UseRandomMutation;
        MutationFactor = settings.MutationFactor;
        MutationExponent = settings.MutationExponent;
        ReproductionRatio = settings.ReproductionRatio;
        UseKeySpecificSwipeDirectionPreferences = settings.UseKeySpecificSwipeDirectionPreferences;
        RedistributeKeyCharactersBasedOnFrequency = settings.RedistributeKeyCharactersBasedOnFrequency;
        CharacterSetString = settings.CharacterSetString;
        AllowCardinalDiagonalSwaps = settings.AllowCardinalDiagonalSwaps;
        PositionPreferences = settings.GetPositionPreferences();
        PreferSwipesInOppositeDirectionOfNextKey = settings.PreferSwipesInOppositeDirectionOfNextKey;
    }
}