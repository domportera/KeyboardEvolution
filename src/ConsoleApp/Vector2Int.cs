using System.Numerics;

namespace ConsoleApp;

struct Vector2Int
{
    public int X, Y;

    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator Vector2Int((int x, int y) valueTuple) => new(valueTuple.x, valueTuple.y);
    public static Vector2Int operator -(Vector2Int one, Vector2Int two) => new(one.X - two.X, one.Y - two.Y);
    public static Vector2Int operator +(Vector2Int one, Vector2Int two) => new(one.X + two.X, one.Y + two.Y);
    public static bool operator !=(Vector2Int one, Vector2Int two) => one.X != two.X || one.Y != two.Y;
    public static bool operator ==(Vector2Int one, Vector2Int two) => !(one != two);
    public static implicit operator Vector2(Vector2Int intVec) => new(intVec.X, intVec.Y);
}