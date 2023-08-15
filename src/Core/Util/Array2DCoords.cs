using System.Diagnostics;
using System.Numerics;

namespace Core.Util;

public readonly struct Array2DCoords : IEquatable<Array2DCoords>
{
    public readonly int ColumnX;
    public readonly int RowY;

    public Array2DCoords(int columnX, int rowY)
    {
        ColumnX = columnX;
        RowY = rowY;
    }
    
    public static readonly Array2DCoords Zero = new(0, 0);
    public static readonly Array2DCoords One = new(1, 1);
    public static readonly Array2DCoords MinValue = new(int.MinValue, int.MinValue);
    public static readonly Array2DCoords MaxValue = new(int.MaxValue, int.MaxValue);

    public static implicit operator Array2DCoords((int x, int y) valueTuple) => new(valueTuple.x, valueTuple.y);
    public static implicit operator Vector2(Array2DCoords coordVec) => new(coordVec.ColumnX, coordVec.RowY);
    
    public static Array2DCoords operator -(Array2DCoords one, Array2DCoords two) => new(one.ColumnX - two.ColumnX, one.RowY - two.RowY);
    public static Array2DCoords operator +(Array2DCoords one, Array2DCoords two) => new(one.ColumnX + two.ColumnX, one.RowY + two.RowY);
    public static Array2DCoords operator *(Array2DCoords one, Array2DCoords two) => new(one.ColumnX * two.ColumnX, one.RowY * two.RowY);
    public static Array2DCoords operator /(Array2DCoords one, Array2DCoords two) => new(one.ColumnX / two.ColumnX, one.RowY / two.RowY);
    public static Array2DCoords operator %(Array2DCoords one, Array2DCoords two) => new(one.ColumnX % two.ColumnX, one.RowY % two.RowY);
    
    public override string ToString() => $"({ColumnX}, {RowY})";
    
    public float DistanceTo(Array2DCoords other) => Vector2.Distance(this, other);
    public float DistanceSquaredTo(Array2DCoords other) => Vector2.DistanceSquared(this, other);
    
    #region Equality
    public static bool operator !=(Array2DCoords one, Array2DCoords two) => one.ColumnX != two.ColumnX || one.RowY != two.RowY;
    public static bool operator ==(Array2DCoords one, Array2DCoords two) => !(one != two);
    
    public override int GetHashCode() => ColumnX.GetHashCode() ^ RowY.GetHashCode();
    
    public bool Equals(Array2DCoords other)
    {
        return ColumnX == other.ColumnX && RowY == other.RowY;
    }

    public override bool Equals(object? obj)
    {
        return obj is Array2DCoords other && Equals(other);
    }
    
    #endregion Equality
    
}