using System.Numerics;

namespace Core.Util;

public static class AngleUtils
{
    const float TwoPi = (float)(2 * Math.PI);
    const float Pi = (float)Math.PI;
    const float OneOverPi = (float)(1 / Math.PI);

    /// <summary>
    /// Returns a normalized double for distance between angles.
    /// </summary>
    /// <returns></returns>
    public static float NormalizedAngleDifference(float angle1, float angle2)
    {
        // Normalize angles to [0, TWO_PI) range
        // this seems redundant but it allows us to account for angles of all sizes without branching or looping
        angle1 = (angle1 % TwoPi + TwoPi) % TwoPi;
        angle2 = (angle2 % TwoPi + TwoPi) % TwoPi;

        // Calculate the absolute difference between the two angles
        float diff = Math.Abs(angle1 - angle2);

        // Account for wrap-around by taking the smaller of the two distances
        if (diff > Math.PI)
        {
            diff = TwoPi - diff;
        }

        // Divide the difference by TWO_PI to get the closeness value between 0 and 1
        return diff * OneOverPi;
    }

    public static float AngleFromVector2(Vector2 vector)
    {
        return (float)Math.Atan2(vector.Y, vector.X);
    }
    
    public static float AngleFromVector2(Vector2Int vector)
    {
        return (float)Math.Atan2(vector.Y, vector.X);
    }
}