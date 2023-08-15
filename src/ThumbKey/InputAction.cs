using Core.Util;

namespace ThumbKey;

readonly struct InputAction
{
    public bool Equals(InputAction other)
    {
        return KeyPosition == other.KeyPosition && SwipeDirection == other.SwipeDirection && Thumb == other.Thumb;
    }

    public override bool Equals(object? obj)
    {
        return obj is InputAction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(KeyPosition, (int)SwipeDirection, (int)Thumb);
    }

    public readonly Array2DCoords KeyPosition;
    public readonly SwipeDirection SwipeDirection;
    public readonly Thumb Thumb;

    public InputAction(int column, int row, SwipeDirection swipeDirection, Thumb thumb)
    {
        SwipeDirection = swipeDirection;
        Thumb = thumb;
        KeyPosition = new Array2DCoords(column, row);
    }

    public static bool operator ==(InputAction one, InputAction two)
    {
        return !(one != two);
    }

    public static bool operator !=(InputAction one, InputAction two)
    {
        return one.KeyPosition != two.KeyPosition
               || one.SwipeDirection != two.SwipeDirection
               || one.Thumb != two.Thumb;
    }
}