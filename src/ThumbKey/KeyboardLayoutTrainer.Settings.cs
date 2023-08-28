using System.Collections.Frozen;
using Core.Util;

namespace ThumbKey;

public static partial class KeyboardLayoutTrainer
{
    const int ParentCount = 1;
    const int ChildrenPerParent = 200;
    const int TotalCount = ParentCount + ParentCount * ChildrenPerParent;

    static KeyboardLayoutTrainer()
    {
        ReproductionRatio = ParentCount / (double)TotalCount;
    }

    public static void Start(string text, List<Range> ranges)
    {
        // foreach (var range in ranges)
        // {
        //     var substring = text.Substring(range.Start.Value, range.End.Value - range.Start.Value);
        //     Console.WriteLine(substring);
        // }

        Key[,] preset = LayoutPresets.Presets[PresetType.ThumbKeyEngV4NoSymbols];
        StartTraining(text, ranges,
            count: TotalCount,
            generationCount: 100_000,
            entriesPerGeneration: ranges.Count / 12,
            seed: (int)DateTime.UtcNow.TimeOfDay.TotalSeconds,
            startingLayout: preset,
            dimensions: preset.GetDimensions());
    }

    const string CharacterSetString = "abcdefghijklmnopqrstuvwxyz'";
    const bool UseStandardSpaceBar = true;

    /// <summary>
    /// The ratio of the population that will be reproduced. I.e., if this is 0.1, the top 10% of the population will
    /// be copied and mutated to fill the rest of the population.
    /// </summary>
    static readonly double ReproductionRatio;

    /// <summary>
    /// The percentage of keys that will be mutated in a given key. I.e., if this is 0.3, 30% of the keys will be
    /// changed in the mutation process.
    /// </summary>
    const float MutationFactor = 0.6f;

    /// <summary>
    /// Allows for a random MutationFactor to be used instead of a constant one, with a range of [0, MutationFactor]
    /// </summary>
    const bool UseRandomMutation = true;

    /// <summary>
    /// If <see cref="UseRandomMutation"/> is true, MutationFactor * Math.Pow(random01, MutationExponent)
    /// to modify the distribution of mutation factors over a population
    /// </summary>
    const float MutationExponent = 2f;


    /// <summary>
    /// If true, uses the key-specific swipe direction preferences for calculations related to swipeDirectionPreference in Weights
    /// This is likely unnecessary due to the Trajectory calculation, but it's here if you want to use it
    /// </summary>
    public const bool UseKeySpecificSwipeDirectionPreferences = false;

    /// <summary>
    /// Governs whether characters within a key are reorganized/optimized after mutation.
    /// </summary>
    public const bool RedistributeKeyCharactersBasedOnFrequency = false;

    /// <summary>
    /// Normally, diagonal keys can only swap with diagonal keys, etc. This is useful for larger layouts, but for
    /// layouts that are 3x3 and smaller, it is recommended to set this to true.
    /// </summary>
    public const bool AllowCardinalDiagonalSwaps = true;

    /// <summary>
    /// The main weights used in the fitness function
    /// </summary>
    static readonly Weights FitnessWeights = new(
        distance: 0.3f, // prefer smaller distance between keypresses made by the same thumb
        trajectory: 0.5f, // prefer swiping in the same direction as that thumb's next key
        handAlternation: 1.5f, // prefer alternating between hands
        handCollisionAvoidance: 0.2f, // for layouts with a center column, there is a penalty for alternating hands on the same key
        positionalPreference: 0.0f, // weight of the hard-coded positional preference dictionary below
        swipeDirectionPreference: 0.4f //weight of the hard-coded swipe types defined below (cardinal, diagonal, center)
    );

    public const float CardinalPreference = 0.4f;
    public const float DiagonalPreference = 0f;
    public const float CenterPreference = 1f;

    public static readonly CharacterReplacement[] CharacterSubsitutions =
    {
        new(@"\u2019", '\'')
    };

    public record CharacterReplacement(string Original, char Replacement)
    {
        public readonly string Original = Original;
        public readonly char Replacement = Replacement;
        public readonly int Count = Original.Length;
    }

    /// <summary>
    /// The preference of swiping towards the center of the keyboard, used in generating positional preferences and calculating
    /// fitness relating to swipeDirectionPreference - rather than simply using the simple swipe direction, the fitness and mutation
    /// functions will prefer swiping towards the center of the keyboard
    /// </summary>
    const float KeysTowardsCenterWeight = 0.1f; //prefer swiping towards the center of the keyboard

    static readonly FrozenDictionary<Array2DCoords, float[,]> PositionPreferences =
        new Dictionary<Array2DCoords, float[,]>()
        {
            {
                new(columnX: 3, rowY: 3),
                new[,]
                {
                    { 0.4f, 0.0f, 0.4f },
                    { 1, 0.7f, 1f },
                    { 1, 1, 1 },
                }
            },
            {
                new(columnX: 4, rowY: 3),
                new[,]
                {
                    { 0.2f, 0.0f, 0.0f, 0.2f },
                    { 1, 0.8f, 0.8f, 1 },
                    { 1, 1, 1, 1 },
                }
            },
            {
                new(columnX: 4, rowY: 4),
                new[,]
                {
                    { 0.4f, 0.0f, 0.0f, 0.4f },
                    { 0.9f, 0.7f, 0.7f, 0.9f },
                    { 1, 1, 1, 1 },
                    { 1, 1, 1, 1 },
                }
            }
        }.ToFrozenDictionary();
}