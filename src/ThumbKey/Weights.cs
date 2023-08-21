using System.Numerics;

namespace ThumbKey;

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

    public Weights(float distance, float trajectory, float handAlternation, float handCollisionAvoidance, float positionalPreference, float swipeDirectionPreference)
    {
        _weights = new float[Vector<float>.Count];
        _weights[DistanceIndex] = distance;
        _weights[TrajectoryIndex] = trajectory;
        _weights[HandAlternationIndex] = handAlternation;
        _weights[HandCollisionAvoidanceIndex] = handCollisionAvoidance;
        _weights[PositionalPreferenceIndex] = positionalPreference;
        _weights[SwipeDirectionPreferenceIndex] = swipeDirectionPreference;
        
        var totalWeight = Vector.Dot(new(_weights), Vector<float>.One);
        _totalWeightDivider = 1f/totalWeight;
    }

    
    public float CalculateScore(float[] results01)
    {
        // the dot product of a vector with one is the sum of its elements
        return Vector.Dot(
            left: Vector.Multiply(new Vector<float>(results01), new Vector<float>(_weights)), 
            right: Vector<float>.One) * _totalWeightDivider;
    }
}