using System.Collections.Frozen;
using Core.Util;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer
{
    const string CharacterSetString = "abcdefghijklmnopqrstuvwxyz,.;*-_!?@$%&():'\"";
    const bool UseStandardSpaceBar = true;
    static readonly Vector2Int Dimensions = new(3, 3);
    const double ReproductionPercentage = 0.001;
    const double MutationFactor = 0.3;
    const double KeysTowardsCenterWeight = 0.1;
    
    static readonly Dictionary<Vector2Int, double[,]> PositionPreferences = new()
    {
        {
            (3, 3),
            new[,]
            {
                { 0.4,  0.0,    0.4 },
                { 1,    0.7,    1 },
                { 1,    1,      1 },
            }
        },
        {
            (4, 3),
            new[,]
            {
                { 0.2,  0.0,    0.0,    0.2 },
                { 1,    0.8,    0.8,    1 },
                { 1,    1,      1,      1 },
            }
        },
        {
            (4, 4),
            new[,]
            {
                { 0.4,  0.0,    0.0,    0.4 },
                { 0.9,  0.7,    0.7,    0.9 },
                { 1,    1,      1,      1 },
                { 1,    1,      1,      1 },
            }
        }
    };

    static readonly Weights FitnessWeights = new()
    {
        Distance = 0.5,
        Trajectory = 0.3,
        HandAlternation = 1.5,
        HandCollisionAvoidance = 0.2,
        PositionalPreference = 0.4,
        SwipeDirection = 1
    };

    const double CardinalPreference = 0.4;
    const double DiagonalPreference = 0;
    const double CenterPreference = 1;

    static readonly FrozenDictionary<SwipeDirection, double> SwipeDirectionPreferences =
        new Dictionary<SwipeDirection, double>()
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