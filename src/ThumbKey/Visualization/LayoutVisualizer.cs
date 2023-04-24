using System.Text;
using Core.Util;

namespace ThumbKey;

public class LayoutVisualizer
{
    readonly KeyboardLayout _layoutToVisualize;
    KeyVisualizer[,] _keyVisualizers;

    readonly Vector2Int _layoutDimensions;
    readonly Vector2Int _keyDimensions;

    // todo: visualization back to key layout? could be a nice proof of concept for an intuitive layout editor
    public LayoutVisualizer(KeyboardLayout layoutToVisualize)
    {
        _layoutToVisualize = layoutToVisualize;
        Key[,] keys = _layoutToVisualize.Traits;
        
        _layoutDimensions = layoutToVisualize.Dimensions;
        _keyDimensions = _layoutToVisualize[(0,0)].Dimensions;
        
        _keyVisualizers = new KeyVisualizer[_layoutDimensions.Y, _layoutDimensions.X];

        for(int y = 0; y < layoutToVisualize.Dimensions.Y; y++)
        for (int x = 0; x < layoutToVisualize.Dimensions.X; x++)
        {
            _keyVisualizers[y, x] = new KeyVisualizer(keys[y, x]);
        }
    }

    public void Visualize()
    {
        Console.Write(GetNewVisualization());
    }


    string GetNewVisualization()
    {
        StringBuilder builder = new();
        
        const string separator = "\n------------------------------\n";

        builder.Append(separator);
        builder.Append("Layout ");
        builder.Append(_layoutDimensions.ToString());

        for (int y = 0; y < _layoutDimensions.Y; y++)
        {
            builder.Append("Row ");
            builder.Append(y + 1);
            builder.Append(separator);
            
            for (int x = 0; x < _layoutDimensions.X; x++)
            {
                KeyVisualizer keyVisualizer = _keyVisualizers[y, x];

                keyVisualizer.RefreshVisualization();
                builder.Append(keyVisualizer.VisualizedKey);
            }
            builder.Append('\n');
        }
        
        
        throw new NotImplementedException();
    }
}