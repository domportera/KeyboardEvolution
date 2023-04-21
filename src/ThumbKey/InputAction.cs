using Core.Util;

namespace ThumbKey;

readonly struct InputAction
{
    public readonly Key KeyPressed;
    public readonly Vector2Int KeyPosition;
    public readonly SwipeDirection SwipeDirection;
    public readonly Thumb Thumb;

    public InputAction(Key keyPressed, int row, int column, SwipeDirection swipeDirection, Thumb thumb)
    {
        KeyPressed = keyPressed;
        SwipeDirection = swipeDirection;
        Thumb = thumb;
        KeyPosition = new(column, row);
    }

    public static bool operator ==(InputAction one, InputAction two)
    {
        return !(one != two);
    }

    public static bool operator !=(InputAction one, InputAction two)
    {
        return one.KeyPressed != two.KeyPressed
               || one.KeyPosition != two.KeyPosition
               || one.SwipeDirection != two.SwipeDirection
               || one.Thumb != two.Thumb;
    }
}