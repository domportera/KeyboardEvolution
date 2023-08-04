using System.Diagnostics;

namespace ThumbKey;

public readonly record struct Weights
{
    public readonly float Distance;
    public readonly float Trajectory;
    public readonly float HandAlternation;

    // for odd-numbered column counts where fingers share a column
    public readonly float HandCollisionAvoidance; 

    // for if a specific key position is preferred for ergonomic reasons
    public readonly float PositionalPreference;

    public readonly float SwipeDirection;

    readonly float _totalWeightDivider;

    public Weights(float distance, float trajectory, float handAlternation, float handCollisionAvoidance, float positionalPreference, float swipeDirection)
    {
        Distance = distance;
        Trajectory = trajectory;
        HandAlternation = handAlternation;
        HandCollisionAvoidance = handCollisionAvoidance;
        PositionalPreference = positionalPreference;
        SwipeDirection = swipeDirection;
        _totalWeightDivider = 1f/(Distance + Trajectory + HandAlternation + HandCollisionAvoidance + PositionalPreference + SwipeDirection);
    }
    
    public float CalculateScore(float closeness01, float trajectory01, float handAlternation01,
        float handCollisionAvoidance01, float positionalPreference01, float swipeDirectionPreference01)
    {
        //Debug.Assert(closeness01 is >= 0 and <= 1);
        //Debug.Assert(trajectory01 is >= 0 and <= 1);
        //Debug.Assert(handAlternation01 is >= 0 and <= 1);
        //Debug.Assert(handCollisionAvoidance01 is >= 0 and <= 1);
        //Debug.Assert(positionalPreference01 is >= 0 and <= 1);
        //Debug.Assert(swipeDirectionPreference01 is >= 0 and <= 1);

        var score =  (closeness01 * Distance +
                      trajectory01 * Trajectory +
                      handAlternation01 * HandAlternation +
                      handCollisionAvoidance01 * HandCollisionAvoidance +
                      positionalPreference01 * PositionalPreference +
                      swipeDirectionPreference01 * SwipeDirection) * _totalWeightDivider; 
        // divides by total weight for normalization 0-1. optional, but more readable output
        // for character-by-character analysis
            
        //Debug.Assert(score is >= 0 and <= 1);
        return score;
    }
}