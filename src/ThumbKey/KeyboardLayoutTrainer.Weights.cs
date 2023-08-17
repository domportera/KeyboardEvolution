using System.Collections.Frozen;
using Core.Util;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer
{
    const string CharacterSetString = "abcdefghijklmnopqrstuvwxyz,.;*-_!?@$%&():'\"/\\`~[]{}<>";
    const bool UseStandardSpaceBar = true;
    readonly Array2DCoords _coords = new(4, 3);

    /// <summary>
    /// The ratio of the population that will be reproduced. I.e., if this is 0.1, the top 10% of the population will
    /// be copied and mutated to fill the rest of the population.
    /// </summary>
    const double ReproductionRatio = 10E-4;

    /// <summary>
    /// The percentage of keys that will be mutated in a given key. I.e., if this is 0.3, 30% of the keys will be
    /// changed in the mutation process.
    /// </summary>
    const float MutationFactor = 0.3f;
    
    /// <summary>
    /// Allows for a random MutationFactor to be used instead of a constant one, with a range of [0, MutationFactor]
    /// </summary>
    const bool UseRandomMutation = true;

    /// <summary>
    /// The main weights used in the fitness function
    /// </summary>
    static readonly Weights FitnessWeights = new(
        distance: 0.5f,
        trajectory: 0.3f,
        handAlternation: 2f,
        handCollisionAvoidance: 0.2f,
        positionalPreference: 0.0f,
        swipeDirection: 1f
    );

    const float CardinalPreference = 0.4f;
    const float DiagonalPreference = 0f;
    const float CenterPreference = 1f;


    /// <summary>
    /// The preference of swiping towards the center of the keyboard, used in generating positional preferences
    /// </summary>
    const float KeysTowardsCenterWeight = 0.1f; //prefer swiping towards the center of the keyboard

    static readonly Dictionary<Array2DCoords, float[,]> PositionPreferences = new()
    {
        {
            new(columnX: 3, rowY: 3),
            new[,]
            {
                { 0.4f,  0.0f,    0.4f },
                { 1,    0.7f,    1f },
                { 1,    1,      1 },
            }
        },
        {
            new (columnX: 4, rowY: 3),
            new[,]
            {
                { 0.2f,  0.0f,    0.0f,    0.2f },
                { 1,    0.8f,    0.8f,    1 },
                { 1,    1,      1,      1 },
            }
        },
        {
            new (columnX: 4, rowY: 4),
            new[,]
            {
                { 0.4f,  0.0f,    0.0f,    0.4f },
                { 0.9f,  0.7f,    0.7f,    0.9f },
                { 1,    1,      1,      1 },
                { 1,    1,      1,      1 },
            }
        }
    };

    static readonly FrozenDictionary<SwipeDirection, float> SwipeDirectionPreferences =
        new Dictionary<SwipeDirection, float>()
        {
            { SwipeDirection.Left, CardinalPreference },
            { SwipeDirection.UpLeft, DiagonalPreference },
            { SwipeDirection.Up, CardinalPreference },
            { SwipeDirection.UpRight, DiagonalPreference },
            { SwipeDirection.Right, CardinalPreference },
            { SwipeDirection.DownRight, DiagonalPreference },
            { SwipeDirection.Down, CardinalPreference },
            { SwipeDirection.DownLeft, DiagonalPreference },
            { SwipeDirection.Center, CenterPreference },
        }.ToFrozenDictionary();
}