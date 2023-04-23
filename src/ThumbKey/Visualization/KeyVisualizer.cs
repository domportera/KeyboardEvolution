using System.Diagnostics;
using System.Text;
using Core.Util;

namespace ThumbKey;

public class KeyVisualizer
{
    readonly Key _key;

    // alt codes from https://www.rapidtables.com/code/text/alt-codes.html
    const char TopBottomBorderCharacter = '─';  // alt + 196
    const char SideBorderCharacter = '│';
    const char SpaceCharacter = '█'; // alt + 219
    const char TabCharacter = '»'; // alt + 170
    const char EmptyCharacter = ' ';
    readonly StringBuilder _stringVizBuilder = new();

    public Vector2Int Dimensions => _key.Dimensions;
    public string VisualizedKey => _stringVizBuilder.ToString();

    internal KeyVisualizer(Key key)
    {
        _key = key;
    }
    public void RefreshVisualization()
    {
        _stringVizBuilder.Clear();

        double sideLengthDouble = Math.Sqrt(_key.Length);
        Debug.Assert(sideLengthDouble % 1 == 0); // length must be a perfect square
        
        var sideLength = (int)sideLengthDouble;

        // top -----
        for (int i = 0; i < sideLength; i++)
            _stringVizBuilder.Append('─'); // alt + 196
        
        _stringVizBuilder.Append('\n');
        
        for (int i = 0; i < _key.Length; i++)
        {
            // left side
            if (i % sideLength == 0)
            {
                _stringVizBuilder.Append(TopBottomBorderCharacter);
                _stringVizBuilder.Append(' ');
            }

            // character
            char visualizationCharacter = _key[i];
            visualizationCharacter = visualizationCharacter switch
            {
                default(char) => EmptyCharacter,
                ' ' => SpaceCharacter,
                '\t' => TabCharacter,
                _ => visualizationCharacter
            };
            
            _stringVizBuilder.Append(' ');
            _stringVizBuilder.Append(visualizationCharacter);
            _stringVizBuilder.Append(' ');

            // right side
            if (i % sideLength == 2)
            {
                _stringVizBuilder.Append(' ');
                _stringVizBuilder.Append(SideBorderCharacter);
                _stringVizBuilder.Append('\n');
            }
        }
        
        // bottom -----
        for (int i = 0; i < sideLength; i++)
            _stringVizBuilder.Append(TopBottomBorderCharacter);
        
        _stringVizBuilder.Append('\n');
    }

}