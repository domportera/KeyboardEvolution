using System.Diagnostics;

namespace ThumbKey;

public readonly record struct Weights
{
    public double Distance { get; init; }
    public double Trajectory { get; init; }
    public double HandAlternation { get; init; }

    // for odd-numbered column counts where fingers share a column
    public double HandCollisionAvoidance { get; init; } 

    // for if a specific key position is preferred for ergonomic reasons
    public double PositionalPreference { get; init; } 
        
    public double SwipeDirection { get; init; }

    double TotalWeight => Distance + Trajectory + HandAlternation + HandCollisionAvoidance + PositionalPreference + SwipeDirection;
    public double CalculateScore(double closeness01, double trajectory01, double handAlternation01,
        double handCollisionAvoidance01, double positionalPreference01, double swipeDirectionPreference01)
    {
        Debug.Assert(closeness01 is >= 0 and <= 1);
        Debug.Assert(trajectory01 is >= 0 and <= 1);
        Debug.Assert(handAlternation01 is >= 0 and <= 1);
        Debug.Assert(handCollisionAvoidance01 is >= 0 and <= 1);
        Debug.Assert(positionalPreference01 is >= 0 and <= 1);
        Debug.Assert(swipeDirectionPreference01 is >= 0 and <= 1);

        var score =  (closeness01 * Distance +
                      trajectory01 * Trajectory +
                      handAlternation01 * HandAlternation +
                      handCollisionAvoidance01 * HandCollisionAvoidance +
                      positionalPreference01 * PositionalPreference +
                      swipeDirectionPreference01 * SwipeDirection) / TotalWeight; 
        // divides by total weight for normalization 0-1. optional, but more readable output
        // for character-by-character analysis
            
        Debug.Assert(score is >= 0 and <= 1);
        return score;
    }
}