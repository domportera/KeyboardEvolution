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

    public readonly Vector2Int Dimensions;
    public readonly char[,] VisualizedKey;

    internal KeyVisualizer(Key key)
    {
        _key = key;

        double sideLengthDouble = Math.Sqrt(_key.Length);
        Debug.Assert(sideLengthDouble % 1 == 0); // length must be a perfect square
        _sideLength = (int)sideLengthDouble;
        Dimensions = (_sideLength * 3 + 1, _sideLength + 2); // Update dimensions calculation
        VisualizedKey = new char[Dimensions.Y, Dimensions.X];
        InitializeEmptyKey();
    }

    void InitializeEmptyKey()
    {
        // horizontal borders
        for (int i = 0; i < Dimensions.X; i++)
        {
            VisualizedKey[0, i] = TopBottomBorderCharacter;
            VisualizedKey[Dimensions.Y - 1, i] = TopBottomBorderCharacter;
        }

        // vertical borders
        for (int i = 1; i < Dimensions.Y - 1; i++)
        {
            VisualizedKey[i, 0] = SideBorderCharacter;
            VisualizedKey[i, Dimensions.X - 1] = SideBorderCharacter;
        }

        // initialize all characters to space
        for (int i = 1; i < Dimensions.Y - 1; i++)
        {
            for (int j = 1; j < Dimensions.X - 1; j++)
            {
                VisualizedKey[i, j] = EmptyCharacter;
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
            VisualizedKey[y, x] = visualizationCharacter;
        }
    }
}