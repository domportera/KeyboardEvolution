using System.Numerics;

namespace ThumbKey;

/// <summary>
/// Contains the amount of impact each factor has on the fitness score of a layout
/// </summary>
public record Weights
{
    /// <summary>
    /// Used to normalize the weights so that they roughly add up to 0-1
    /// </summary>
    readonly float _totalWeightDivider;

    public const int DistanceIndex = 0;
    public const int TrajectoryIndex = 1;
    public const int HandAlternationIndex = 2;
    public const int HandCollisionAvoidanceIndex = 3;
    public const int PositionalPreferenceIndex = 4;
    public const int SwipeDirectionPreferenceIndex = 5;
    public const int FieldCount = 6;

    readonly float[] _weights;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="distance">Prefer smaller distance between keypresses made by the same thumb</param>
    /// <param name="trajectory">Prefer swiping in the same direction as that thumb's next key</param>
    /// <param name="handAlternation">Prefer alternating between hands</param>
    /// <param name="handCollisionAvoidance">For layouts with a center column, there is a penalty for alternating hands on the same key</param>
    /// <param name="positionalPreference">Weight of the hard-coded positional preference dictionary in settings file</param>
    /// <param name="swipeDirectionPreference">Weight of the hard-coded swipe types defined in settings file (cardinal, diagonal, center)</param>
    public Weights(float distance, float trajectory, float handAlternation, float handCollisionAvoidance,
        float positionalPreference, float swipeDirectionPreference)
    {
        _weights = new float[Vector<float>.Count];
        _weights[DistanceIndex] = distance;
        _weights[TrajectoryIndex] = trajectory;
        _weights[HandAlternationIndex] = handAlternation;
        _weights[HandCollisionAvoidanceIndex] = handCollisionAvoidance;
        _weights[PositionalPreferenceIndex] = positionalPreference;
        _weights[SwipeDirectionPreferenceIndex] = swipeDirectionPreference;

        var totalWeight = Vector.Dot(new(_weights), Vector<float>.One);
        _totalWeightDivider = 1f / totalWeight;
    }

    public Weights(SerializableWeights weights) : this(
        distance: weights.Distance,
        trajectory: weights.Trajectory,
        handAlternation: weights.HandAlternation,
        handCollisionAvoidance: weights.HandCollisionAvoidance,
        positionalPreference: weights.PositionalPreference,
        swipeDirectionPreference: weights.SwipeDirectionPreference)
    {
    }

    /// <summary>
    /// Returns a fitness score from 0-1 based on the given results, though the actual score
    /// is likely to never quite reach 1 without a layout with duplicate keys
    /// </summary>
    /// <param name="results01">A SIMD-accessed <see cref="Vector"/> array populated using the indexes
    /// defined in this type</param>
    /// <returns></returns>
    public float CalculateScore(float[] results01)
    {
        // the dot product of a vector with one is the sum of its elements
        return Vector.Dot(
            left: Vector.Multiply(new Vector<float>(results01), new Vector<float>(_weights)),
            right: Vector<float>.One) * _totalWeightDivider;
    }
}

[Serializable]
public record SerializableWeights(float Distance, float Trajectory, float HandAlternation, float HandCollisionAvoidance,
    float PositionalPreference, float SwipeDirectionPreference)
{
    public readonly float Distance = Distance,
        Trajectory = Trajectory,
        HandAlternation = HandAlternation,
        HandCollisionAvoidance = HandCollisionAvoidance,
        PositionalPreference = PositionalPreference,
        SwipeDirectionPreference = SwipeDirectionPreference;
}