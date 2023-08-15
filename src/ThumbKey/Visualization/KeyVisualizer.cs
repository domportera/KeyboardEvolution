using System.Diagnostics;
using Core.Util;

namespace ThumbKey.Visualization;

public class KeyVisualizer
{
    readonly Key _key;

    // alt codes from https://www.rapidtables.com/code/text/alt-codes.html
    const char TopBottomBorderCharacter = '─'; // alt + 196
    const char SideBorderCharacter = '│';
    const char SpaceCharacter = '█'; // alt + 219
    const char TabCharacter = '»'; // alt + 170
    const char EmptyCharacter = ' ';
    readonly int _sideLength;

    public readonly Array2DCoords Coords;
    public readonly char[,] VisualizedKey;

    internal KeyVisualizer(Key key)
    {
        _key = key;

        double sideLengthDouble = Math.Sqrt(_key.Length);
        Debug.Assert(sideLengthDouble % 1 == 0); // length must be a perfect square
        _sideLength = (int)sideLengthDouble;
        Coords = (_sideLength * 3 + 1, _sideLength + 2); // Update dimensions calculation
        VisualizedKey = new char[Coords.RowY, Coords.ColumnX];
        InitializeEmptyKey();
    }

    void InitializeEmptyKey()
    {
        // horizontal borders
        for (int i = 0; i < Coords.ColumnX; i++)
        {
            var topIndex = new Array2DCoords(i, 0);
            VisualizedKey.Set(topIndex, TopBottomBorderCharacter);

            var bottomIndex = new Array2DCoords(i, Coords.RowY - 1);
            VisualizedKey.Set(bottomIndex, TopBottomBorderCharacter);
        }

        // vertical borders
        for (int i = 1; i < Coords.RowY - 1; i++)
        {
            var leftIndex = new Array2DCoords(0, i);
            VisualizedKey.Set(leftIndex, SideBorderCharacter);

            var rightIndex = new Array2DCoords(Coords.ColumnX - 1, i);
            VisualizedKey.Set(rightIndex, SideBorderCharacter);
        }

        // initialize all characters to space
        for (int y = 1; y < Coords.RowY - 1; y++)
        {
            for (int x = 1; x < Coords.ColumnX - 1; x++)
            {
                var index = new Array2DCoords(x, y);
                VisualizedKey.Set(index, EmptyCharacter);
            }
        }
    }

    public void RefreshVisualization()
    {
        for (int i = 0; i < _key.Length; i++)
        {
            char visualizationCharacter = _key[i];
            visualizationCharacter = visualizationCharacter switch
            {
                default(char) => EmptyCharacter,
                ' ' => SpaceCharacter,
                '\t' => TabCharacter,
                _ => visualizationCharacter
            };

            int x = (i % _sideLength) * 3 + 1;
            int y = i / _sideLength + 1;
            var index2d = new Array2DCoords(x, y);
            VisualizedKey.Set(index2d, visualizationCharacter);
        }
    }
}