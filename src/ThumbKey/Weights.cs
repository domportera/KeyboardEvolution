using System.Numerics;

namespace ThumbKey;

public readonly record struct Weights
{
    /// <summary>
    /// Prefer keys that are closer to the previous inputted key
    /// </summary>
    readonly float _distance;
    
    /// <summary>
    /// Prefer keys that are in the direction of the previous swipe
    /// </summary>
    readonly float _trajectory;
    
    /// <summary>
    /// Prefer keys that are on the opposite hand of the previous key
    /// </summary>
    readonly float _handAlternation;

    // for odd-numbered column counts where fingers share a column
    readonly float _handCollisionAvoidance; 

    // for if a specific key position is preferred for ergonomic reasons
    readonly float _positionalPreference;

    /// <summary>
    /// Prefer keys based on the direction of the swipe (e.g. prefer a button press over a diagonal swipe)
    /// </summary>
    readonly float _swipeDirectionPreference;

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

    public Weights(float distance, float trajectory, float handAlternation, float handCollisionAvoidance, float positionalPreference, float swipeDirectionPreference)
    {
        _distance = distance;
        _trajectory = trajectory;
        _handAlternation = handAlternation;
        _handCollisionAvoidance = handCollisionAvoidance;
        _positionalPreference = positionalPreference;
        _swipeDirectionPreference = swipeDirectionPreference;
        _totalWeightDivider = 1f/(_distance + _trajectory + _handAlternation + _handCollisionAvoidance + _positionalPreference + _swipeDirectionPreference);
        
        float[] weights = new float[Vector<float>.Count];
        weights[DistanceIndex] = _distance;
        weights[TrajectoryIndex] = _trajectory;
        weights[HandAlternationIndex] = _handAlternation;
        weights[HandCollisionAvoidanceIndex] = _handCollisionAvoidance;
        weights[PositionalPreferenceIndex] = _positionalPreference;
        weights[SwipeDirectionPreferenceIndex] = _swipeDirectionPreference;
        _weights = new Vector<float>(weights);
    }

    readonly Vector<float> _weights;
    
    public float CalculateScore(float[] results01)
    {
        var scoreVec = Vector.Multiply(new Vector<float>(results01), _weights);
        
        // the dot product of a vector with one is the sum of its elements
        return Vector.Dot(scoreVec, Vector<float>.One) * _totalWeightDivider;
    }
    
    public float CalculateScore(float closeness01, float trajectory01, float handAlternation01,
        float handCollisionAvoidance01, float positionalPreference01, float swipeDirectionPreference01)
    {
        var score =  (closeness01 * _distance +
                      trajectory01 * _trajectory +
                      handAlternation01 * _handAlternation +
                      handCollisionAvoidance01 * _handCollisionAvoidance +
                      positionalPreference01 * _positionalPreference +
                      swipeDirectionPreference01 * _swipeDirectionPreference) * _totalWeightDivider; 
            
        return score;
    }
}