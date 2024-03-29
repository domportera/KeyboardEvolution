namespace ThumbKey;

// used as array indices
public enum SwipeDirection
{
    UpLeft = 0,
    Up = 1,
    UpRight = 2,
    Left = 3,
    Center = 4,
    Right = 5,
    DownLeft = 6,
    Down = 7,
    DownRight = 8,
}

internal enum Thumb // todo: further abstraction : hand? finger type? what of one-handed users?
{
    // these are 0 and 1 so that they can be used as array indices
    Left = 0,
    Right = 1
}
