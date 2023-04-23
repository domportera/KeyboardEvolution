namespace ThumbKey;

internal enum SwipeDirection
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
    None = -1
}

internal enum Thumb // todo: further abstraction : hand? finger type? what of one-handed users?
{
    Left = 0,
    Right = 1
}
