using System.Numerics;

namespace Core.Util;

public static class AngleUtils
{
    const double TwoPi = 2 * Math.PI;

    /// <summary>
    /// Returns a normalized double for distance between angles.
    /// </summary>
    /// <returns></returns>
    public static double NormalizedAngleDifference(double angle1, double angle2)
    {
        // Normalize angles to [0, TWO_PI) range
        // this seems redundant but it allows us to account for angles of all sizes without branching or looping
        angle1 = (angle1 % TwoPi + TwoPi) % TwoPi;
        angle2 = (angle2 % TwoPi + TwoPi) % TwoPi;

        // Calculate the absolute difference between the two angles
        double diff = Math.Abs(angle1 - angle2);

        // Account for wrap-around by taking the smaller of the two distances
        if (diff > Math.PI)
        {
            diff = TwoPi - diff;
        }

        // Divide the difference by TWO_PI to get the closeness value between 0 and 1
        return diff / Math.PI;
    }

    public static double AngleFromVector2(Vector2 vector)
    {
        return Math.Atan2(vector.Y, vector.X);
    }
    
    public static double AngleFromVector2(Vector2Int vector)
    {
        return Math.Atan2(vector.Y, vector.X);
    }
}