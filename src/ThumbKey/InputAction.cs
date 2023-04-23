using Core.Util;

namespace ThumbKey;

readonly struct InputAction
{
    public readonly Vector2Int KeyPosition;
    public readonly SwipeDirection SwipeDirection;
    public readonly Thumb Thumb;

    public InputAction(int column, int row, SwipeDirection swipeDirection, Thumb thumb)
    {
        SwipeDirection = swipeDirection;
        Thumb = thumb;
        KeyPosition = new Vector2Int(column, row);
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