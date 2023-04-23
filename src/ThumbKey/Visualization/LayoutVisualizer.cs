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
        Key[,] keys = _layoutToVisualize.Keys;
        
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


    public string GetNewVisualization()
    {
        for(int y = 0; y < _layoutDimensions.Y; y++)
        for (int x = 0; x < _layoutDimensions.X; x++)
        {
            _keyVisualizers[y, x].RefreshVisualization();
        }

        // append keys into larger whole
        StringBuilder builder = new();
        
        for(int y = 0; y < _layoutDimensions.Y; y++)
        for (int x = 0; x < _layoutDimensions.X; x++)
        {
            KeyVisualizer keyVisualizer = _keyVisualizers[y, x];
            string visualizedKey = keyVisualizer.VisualizedKey;
            string modifiedKey = visualizedKey;
        }
        
        throw new NotImplementedException();
    }
}