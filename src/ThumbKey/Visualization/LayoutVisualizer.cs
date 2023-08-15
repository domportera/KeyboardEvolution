using System.Text;
using Core.Util;

namespace ThumbKey.Visualization;

public class LayoutVisualizer
{
    public readonly KeyboardLayout LayoutToVisualize;
    readonly KeyVisualizer[,] _keyVisualizers;

    readonly Array2DCoords _layoutCoords;
    readonly Array2DCoords _keyCoords;
    readonly char[,] _visualizations;

    static int _incrementingId = 0;
    readonly int _id = _incrementingId++;

    const string Separator = "\n------------------------------\n";
    readonly string _header;

    // todo: visualization back to key layout? could be a nice proof of concept for an intuitive layout editor
    public LayoutVisualizer(KeyboardLayout layoutToVisualize)
    {
        LayoutToVisualize = layoutToVisualize;
        Key[,] keys = LayoutToVisualize.Traits;

        _layoutCoords = layoutToVisualize.Coords;

        _keyVisualizers = new KeyVisualizer[_layoutCoords.RowY, _layoutCoords.ColumnX];
        for (int y = 0; y < layoutToVisualize.Coords.RowY; y++)
        for (int x = 0; x < layoutToVisualize.Coords.ColumnX; x++)
        {
            var keyIndex2d = new Array2DCoords(x, y);
            _keyVisualizers.Set(keyIndex2d, new KeyVisualizer(keys.Get(keyIndex2d)));
            if (_keyCoords == default)
            {
                var vizIndex2d = new Array2DCoords(x, y);
                _keyCoords = _keyVisualizers.Get(vizIndex2d).Coords;
            }
        }

        _visualizations = new char[_layoutCoords.RowY * _keyCoords.RowY, _layoutCoords.ColumnX * _keyCoords.ColumnX + 1];

        var lastX = _visualizations.GetLength(1) - 1;
        // make newline characters at the end of each row
        for (int y = 0; y < _visualizations.GetLength(0); y++)
        {
            var vizIndex2d = new Array2DCoords(lastX, y);
            _visualizations.Set(vizIndex2d, '\n');
        }

        _header = $"{Separator}Layout {_id.ToString()}: {_layoutCoords}\n";
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
        builder.Append(LayoutToVisualize.Fitness);
        builder.Append('\n');

        for (int y = 0; y < _layoutCoords.RowY; y++)
        {
            for (int x = 0; x < _layoutCoords.ColumnX; x++)
            {
                var layoutIndex2d = new Array2DCoords(x, y);
                KeyVisualizer keyVisualizer = _keyVisualizers.Get(layoutIndex2d);
                keyVisualizer.RefreshVisualization();

                Array2DCoords position = new(x * _keyCoords.ColumnX, y * _keyCoords.RowY);

                for (int yKey = 0; yKey < _keyCoords.RowY; yKey++)
                {
                    for (int xKey = 0; xKey < _keyCoords.ColumnX; xKey++)
                    {
                        var visualizationIndex2d = new Array2DCoords(position.ColumnX + xKey, position.RowY + yKey);
                        var visualizedKeyIndex2d = new Array2DCoords(xKey, yKey);
                        var visualizedKey = keyVisualizer.VisualizedKey.Get(visualizedKeyIndex2d);
                        _visualizations.Set(visualizationIndex2d, visualizedKey);
                    }
                }
            }
        }

        // iterate through the visualizations array and add each character to the string builder
        for (int y = 0; y < _visualizations.GetLength(0); y++)
        {
            for (int x = 0; x < _visualizations.GetLength(1); x++)
            {
                var visualizationIndex2d = new Array2DCoords(x, y);
                builder.Append(_visualizations.Get(visualizationIndex2d));
            }
        }

        builder.Append('\n');
        return builder.ToString();
    }
}