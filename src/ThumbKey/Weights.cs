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
    readonly float _swipeDirection;

    /// <summary>
    /// Used to normalize the weights so that they roughly add up to 0-1
    /// </summary>
    readonly float _totalWeightDivider;

    public Weights(float distance, float trajectory, float handAlternation, float handCollisionAvoidance, float positionalPreference, float swipeDirection)
    {
        _distance = distance;
        _trajectory = trajectory;
        _handAlternation = handAlternation;
        _handCollisionAvoidance = handCollisionAvoidance;
        _positionalPreference = positionalPreference;
        _swipeDirection = swipeDirection;
        _totalWeightDivider = 1f/(_distance + _trajectory + _handAlternation + _handCollisionAvoidance + _positionalPreference + _swipeDirection);
    }
    
    public float CalculateScore(float closeness01, float trajectory01, float handAlternation01,
        float handCollisionAvoidance01, float positionalPreference01, float swipeDirectionPreference01)
    {
        var score =  (closeness01 * _distance +
                      trajectory01 * _trajectory +
                      handAlternation01 * _handAlternation +
                      handCollisionAvoidance01 * _handCollisionAvoidance +
                      positionalPreference01 * _positionalPreference +
                      swipeDirectionPreference01 * _swipeDirection) * _totalWeightDivider; 
            
        return score;
    }
}