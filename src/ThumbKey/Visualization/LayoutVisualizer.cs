using System.Text;
using Core.Util;

namespace ThumbKey.Visualization;

public class LayoutVisualizer
{
    readonly KeyboardLayout _layoutToVisualize;
    KeyVisualizer[,] _keyVisualizers;

    readonly Vector2Int _layoutDimensions;
    readonly Vector2Int _keyDimensions;
    readonly char[,] _visualizations;

    static int _incrementingId = 0;
    readonly int _id = _incrementingId++;

    const string Separator = "\n------------------------------\n";
    readonly string _header;

    // todo: visualization back to key layout? could be a nice proof of concept for an intuitive layout editor
    public LayoutVisualizer(KeyboardLayout layoutToVisualize)
    {
        _layoutToVisualize = layoutToVisualize;
        Key[,] keys = _layoutToVisualize.Traits;

        _layoutDimensions = layoutToVisualize.Dimensions;

        _keyVisualizers = new KeyVisualizer[_layoutDimensions.Y, _layoutDimensions.X];
        for (int y = 0; y < layoutToVisualize.Dimensions.Y; y++)
        for (int x = 0; x < layoutToVisualize.Dimensions.X; x++)
        {
            _keyVisualizers[y, x] = new KeyVisualizer(keys[y, x]);
            if (_keyDimensions == default)
            {
                _keyDimensions = _keyVisualizers[y, x].Dimensions;
            }
        }

        _visualizations = new char[_layoutDimensions.Y * _keyDimensions.Y, _layoutDimensions.X * _keyDimensions.X + 1];

        // make newline characters at the end of each row
        for (int y = 0; y < _visualizations.GetLength(0); y++)
        {
            _visualizations[y, _visualizations.GetLength(1) - 1] = '\n';
        }

        _header = $"{Separator}Layout {_id.ToString()}: {_layoutDimensions}\n";
    }

    public void Visualize()
    {
        Console.Write(GetNewVisualization());
    }

    string GetNewVisualization()
    {
        StringBuilder builder = new();

        builder.Append(_header);
        builder.Append("Fitness: ");
        builder.Append(_layoutToVisualize.Fitness);
        builder.Append('\n');

        for (int y = 0; y < _layoutDimensions.Y; y++)
        {
            for (int x = 0; x < _layoutDimensions.X; x++)
            {
                KeyVisualizer keyVisualizer = _keyVisualizers[y, x];
                keyVisualizer.RefreshVisualization();

                Vector2Int position = new(x * _keyDimensions.X, y * _keyDimensions.Y);

                for (int yKey = 0; yKey < _keyDimensions.Y; yKey++)
                {
                    for (int xKey = 0; xKey < _keyDimensions.X; xKey++)
                    {
                        _visualizations[position.Y + yKey, position.X + xKey] = keyVisualizer.VisualizedKey[yKey, xKey];
                    }
                }
            }
        }

        // iterate through the visualizations array and add each character to the string builder
        for (int y = 0; y < _visualizations.GetLength(0); y++)
        {
            for (int x = 0; x < _visualizations.GetLength(1); x++)
            {
                builder.Append(_visualizations[y, x]);
            }
        }

        builder.Append('\n');
        return builder.ToString();
    }
}