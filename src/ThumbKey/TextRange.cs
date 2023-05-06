using System.Diagnostics;

namespace ThumbKey;

public record class TextRange(string Text, Range Range)
{
    public string Text = Text;
    public Range Range = Range;
}

// simple object pool class for TextRange
public class TextRangePool
{
    readonly Stack<TextRange> _pool;
    
    public TextRangePool(int capacity, int prespawnCount)
    {
        Debug.Assert(capacity >= prespawnCount, "Prespawn count should be less than or equal to capacity");
        _pool = new Stack<TextRange>(capacity);
        
        for (int i = 0; i < prespawnCount; i++)
        {
            _pool.Push(new TextRange(string.Empty, default));
        }
    }

    public TextRange Get(string text, Range range)
    {
        if (_pool.Count > 0)
        {
            var textRange = _pool.Pop();
            textRange.Text = text;
            textRange.Range = range;
            return textRange;
        }

        return new TextRange(text, range);
    }

    public void Return(TextRange textRange)
    {
        _pool.Push(textRange);
    }
}