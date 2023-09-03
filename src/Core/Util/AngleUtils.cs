using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Core.Util;

public static class AngleUtils
{
    public const float TwoPi = 2 * MathF.PI;
    public const float Pi = MathF.PI;
    public const float OneOverPi = 1 / MathF.PI;

    static AngleUtils()
    {
        // tests
        const float tolerance = 0.0001f;
        
        Debug.Assert(NormalizedAngleDifference(0, 0) == 0);
        Debug.Assert(NormalizedAngleDifference(-Pi, Pi) == 0);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(Pi, -Pi) - 0) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi) - 1) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi / 2) - 0.5f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi / 4) - 0.25f) < 0.001);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi / 8) - 0.125f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi / 16) - 0.0625f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(0, Pi / 32) - 0.03125f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, Pi/2) - 0.5) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, Pi/4) - 0.75f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, Pi/8) - 0.875f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, Pi/16) - 0.9375f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, 0) - 1) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, -Pi/2) - 0.5f) < tolerance);
        Debug.Assert(Math.Abs(NormalizedAngleDifference(-Pi, -Pi/4) - 0.75f) < tolerance);
        
    }

    /// <summary>
    /// Returns a normalized double for distance between angles.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NormalizedAngleDifference(float angle1, float angle2)
    {
        return (Pi - MathF.Abs((MathF.Abs(angle1 - angle2) % TwoPi) - Pi)) * OneOverPi;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleFromVector2(in Vector2 vector)
    {
        return (float)Math.Atan2(vector.Y, vector.X);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleFromVector2(in Array2DCoords vector)
    {
        return (float)Math.Atan2(vector.RowY, vector.ColumnX);
    }
}