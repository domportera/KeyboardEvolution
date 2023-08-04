using System.Collections.Frozen;
using Core.Util;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer
{
    const string CharacterSetString = "abcdefghijklmnopqrstuvwxyz,.;*-_!?@$%&():'\"";
    const bool UseStandardSpaceBar = true;
    static readonly Vector2Int Dimensions = new(3, 3);
    const double ReproductionPercentage = 0.000001;
    const float MutationFactor = 0.1f;
    const float KeysTowardsCenterWeight = 0.1f;
    
    static readonly Dictionary<Vector2Int, float[,]> PositionPreferences = new()
    {
        {
            (3, 3),
            new[,]
            {
                { 0.4f,  0.0f,    0.4f },
                { 1,    0.7f,    1f },
                { 1,    1,      1 },
            }
        },
        {
            (4, 3),
            new[,]
            {
                { 0.2f,  0.0f,    0.0f,    0.2f },
                { 1,    0.8f,    0.8f,    1 },
                { 1,    1,      1,      1 },
            }
        },
        {
            (4, 4),
            new[,]
            {
                { 0.4f,  0.0f,    0.0f,    0.4f },
                { 0.9f,  0.7f,    0.7f,    0.9f },
                { 1,    1,      1,      1 },
                { 1,    1,      1,      1 },
            }
        }
    };

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