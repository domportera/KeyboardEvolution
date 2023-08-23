using System.Collections.Frozen;
using Core.Util;

namespace ThumbKey;

public static partial class KeyboardLayoutTrainer
{
    const int ParentCount = 10;
    const int ChildrenPerParent = 100;
    const int TotalCount = ParentCount + ParentCount * ChildrenPerParent;

    static KeyboardLayoutTrainer()
    {
        ReproductionRatio = ParentCount / (double)TotalCount;
    }

    public static void Start(string text, List<Range> ranges)
    {
        Key[,] preset = LayoutPresets.Presets[PresetType.FourColumn];
        StartTraining(text, ranges,
            count: TotalCount,
            generationCount: 19_220,
            entriesPerGeneration: -1,
            seed: 100,
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
    const float MutationFactor = 1f;

    /// <summary>
    /// Allows for a random MutationFactor to be used instead of a constant one, with a range of [0, MutationFactor]
    /// </summary>
    const bool UseRandomMutation = true;

    /// <summary>
    /// If true, the mutation factor be MutationFactor * sqrt(Random.Range(0, 1)) to lean towards your specified mutation factor
    /// </summary>
    const bool SqrtRandomMutation = false;


    /// <summary>
    /// If true, uses the key-specific swipe direction preferences for calculations related to swipeDirectionPreference in Weights
    /// This is likely unnecessary due to the Trajectory calculation, but it's here if you want to use it
    /// </summary>
    public const bool UseKeySpecificSwipeDirectionPreferences = false;

    /// <summary>
    /// The main weights used in the fitness function
    /// </summary>
    static readonly Weights FitnessWeights = new(
        distance: 0.5f,
        trajectory: 0.3f,
        handAlternation: 3.5f,
        handCollisionAvoidance: 0.2f,
        positionalPreference: 0.0f,
        swipeDirectionPreference: 1f
    );

    const float CardinalPreference = 0.4f;
    const float DiagonalPreference = 0f;
    const float CenterPreference = 1f;


    /// <summary>
    /// The preference of swiping towards the center of the keyboard, used in generating positional preferences
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