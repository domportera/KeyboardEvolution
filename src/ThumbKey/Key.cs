using System.Collections.Frozen;
using System.Diagnostics;
using Core.Util;

namespace ThumbKey;

public class Key
{
    public static readonly int MaxCharacterCount = Enum.GetValues<SwipeDirection>().Length - 1;
    readonly char[] _characters = new char[MaxCharacterCount];
    readonly double[] _fitness = new double[MaxCharacterCount];

    internal Key(ReadOnlySpan<char> characters): this()
    {
        _characters = characters.ToArray();
    }

    internal Key(char center, ReadOnlySpan <char> cardinal, ReadOnlySpan<char> diagonal, Random random): this()
    {
        char[] cardinalCharacters = new char[4];
        cardinal.CopyTo(cardinalCharacters);
        char[] diagonalCharacters = new char[4];
        diagonal.CopyTo(diagonalCharacters);
        
        random.Shuffle(cardinalCharacters);
        random.Shuffle(diagonalCharacters);
        
        this[SwipeDirection.Center] = center;
        this[SwipeDirection.Up] = cardinalCharacters[0];
        this[SwipeDirection.Down] = cardinalCharacters[1];
        this[SwipeDirection.Left] = cardinalCharacters[2];
        this[SwipeDirection.Right] = cardinalCharacters[3];
        this[SwipeDirection.UpLeft] = diagonalCharacters[0];
        this[SwipeDirection.UpRight] = diagonalCharacters[1];
        this[SwipeDirection.DownLeft] = diagonalCharacters[2];
        this[SwipeDirection.DownRight] = diagonalCharacters[3];
    }

    Key()
    {
        double side = Math.Sqrt(MaxCharacterCount);
        Debug.Assert(side % 1 == 0); // must be square... for now....

        Dimensions = new((int)side, (int)side);   
    }

    Key(char[] characters)
    {
        _characters = characters;
    }

    public Key(Key keyToCopy)
    {
        _characters = keyToCopy._characters.ToArray();
    }

    public Key Duplicate() => new Key(_characters.ToArray());

    public void OverwriteKeysWith(Key other)
    {
        other._characters.CopyTo(_characters.AsSpan());
    }
    
    readonly Dictionary<char, long> _frequenciesOfMyCharacters = new(MaxCharacterCount);
    readonly List<(char, SwipeDirection)> _pairs = new();
    internal void RedistributeKeysOptimally(IReadOnlyDictionary<char, long> characterAppearances, IReadOnlyDictionary<SwipeDirection, float> swipeDirectionPreferences)
    {
        _frequenciesOfMyCharacters.Clear();
        // redistribute keys so that the most common characters are in the center
        // and that cardinal directions are filled by the characters in the corners
        // and that the least common characters are in the corners if cardinals are filled

        foreach (var c in _characters)
        {
            if (c == default) continue;
            _frequenciesOfMyCharacters.Add(c, characterAppearances[c]);
        }
        
        var charsSortedByFrequency = _frequenciesOfMyCharacters
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToArray();
        
        // pair up the most common characters with the most preferred swipe directions
        // based on this keys position in the layout
        
        var swipeDirectionPreferencesSorted = swipeDirectionPreferences
            .OrderByDescending(x => x.Value) // order by descending swipe preference
            .Select(x => x.Key)
            .ToArray();
        
        _pairs.Clear();
        for (int i = 0; i < charsSortedByFrequency.Length; i++)
        {
            var character = charsSortedByFrequency[i];
            var swipeDirection = swipeDirectionPreferencesSorted[i];
            _pairs.Add((character, swipeDirection));
        }
        
        // clear char array
        Array.Clear(_characters, 0, _characters.Length);
        
        // assign chars to their new swipe directions
        foreach (var (character, swipeDirection) in _pairs)
        {
            var index = (int)swipeDirection;
            _characters[index] = character;
        }
    }

    enum SwipeType { Center, Cardinal, Diagonal }
    static readonly SwipeDirection[] CardinalDirections = { SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right };
    static readonly SwipeDirection[] DiagonalDirections = { SwipeDirection.UpLeft, SwipeDirection.UpRight, SwipeDirection.DownLeft, SwipeDirection.DownRight };
    
    internal static void SwapRandomCharacterFromEach(Key key1, Key key2, Random random)
    {
        Debug.Assert(key1.GetValidCharacterCount() > 0 && key2.GetValidCharacterCount() > 0);

        char char1, char2;
        var shouldLoop = true;
        var position1 = SwipeDirection.Center;
        var position2 = SwipeDirection.Center;
        
        do
        {
            SwipeType swipeType = (SwipeType)random.Next(0, 3);
            switch (swipeType)
            {
                case SwipeType.Center:
                    position1 = SwipeDirection.Center;
                    position2 = SwipeDirection.Center;
                    break;
                case SwipeType.Cardinal:
                    position1 = CardinalDirections[random.Next(0, 4)];
                    position2 = CardinalDirections[random.Next(0, 4)];
                    break;
                case SwipeType.Diagonal:
                    position1 = DiagonalDirections[random.Next(0, 4)];
                    position2 = DiagonalDirections[random.Next(0, 4)];
                    break;
            }
            
            char1 = key1[position1];
            char2 = key2[position2];

            shouldLoop = char1 == char2;
        } while (shouldLoop);

        key1[position1] = char2;
        key2[position2] = char1;
    }

    int GetValidCharacterCount()
    {
        int count = 0;
        foreach (var c in _characters)
        {
            if (c != default)
                count++;
        }

        return count;
    }


    public char[] GetAllCharacters() => _characters.ToArray();

    public char this[SwipeDirection direction]
    {
       get => _characters[(int)direction]; 
       private set => _characters[(int)direction] = value;
    } 
    
    public char this[Vector2Int position] => _characters[position.X + position.X * position.Y + position.Y];
    public char this[int position] => _characters[position];
    public int Length => _characters.Length;
    public IReadOnlyList<char> Characters => _characters;

    public readonly Vector2Int Dimensions;

    public bool TryAddCharacter(char character, Random random)
    {
        List<int> freeIndices = new(MaxCharacterCount);
        for (int i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] == default)
                freeIndices.Add(i);
        }

        if (freeIndices.Count == 0)
            return false;
        
        int randomIndex = random.Next(0, freeIndices.Count); 
        _characters[freeIndices[randomIndex]] = character;
        return true;
    }

    public bool Contains(char c) => _characters.Contains(c);
}