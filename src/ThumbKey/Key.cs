using System.Diagnostics;
using System.Text;
using Core;
using Core.Util;

namespace ThumbKey;

public class Key
{
    public static readonly int MaxCharacterCount = Enum.GetValues<SwipeDirection>().Length - 1;
    readonly char[] _characters = new char[MaxCharacterCount];
    readonly double[] _fitness = new double[MaxCharacterCount];

    internal Key(ReadOnlySpan<char> characters)
    {
        Debug.Assert(characters.Length == MaxCharacterCount);
        double side = Math.Sqrt(MaxCharacterCount);
        Debug.Assert(side % 1 == 0); // must be square... for now....

        Dimensions = ((int)side, (int)side);
        _characters = characters.ToArray();
    }

    internal Key(ReadOnlySpan<char> characters, Random random)
    {
        Debug.Assert(characters.Length <= MaxCharacterCount);
        double side = Math.Sqrt(MaxCharacterCount);
        Debug.Assert(side % 1 == 0); // must be square... for now....

        Dimensions = new((int)side, (int)side);

        RandomlyDistributeCharacters(characters, random);
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
    
    void RandomlyDistributeCharacters(ReadOnlySpan<char> characters, Random random)
    {
        Debug.Assert(characters.Length <= _characters.Length);
        Debug.Assert(characters.Length > 0);

        for (int i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            _characters[i] = character;
        }

        random.Shuffle(_characters);
        List<int> indexesContainingCharacter = new(MaxCharacterCount);
        PopulateIndexesContainingCharacter(indexesContainingCharacter, _characters);
        EnsureCenterHasCharacter(random, indexesContainingCharacter);

        Debug.Assert(_characters[(int)SwipeDirection.Center] != default);

        static void PopulateIndexesContainingCharacter(ICollection<int> indexesContainingCharacter, char[] characters)
        {
            indexesContainingCharacter.Clear();
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != default)
                    indexesContainingCharacter.Add(i);
            }
        }
    }

    void EnsureCenterHasCharacter(Random random, IList<int> indexesContainingCharacter)
    {
        const int centerIndex = (int)SwipeDirection.Center;
        if (char.IsLetter(_characters[centerIndex])) return;

        int indexWithCharacter = indexesContainingCharacter.First(x => char.IsLetter(_characters[x]));
        char stolenCharacter = _characters[indexWithCharacter];
        char originalCharacter = _characters[centerIndex]; 
        _characters[indexWithCharacter] = originalCharacter;
        _characters[centerIndex] = stolenCharacter;
    }

    readonly Dictionary<char, long> _frequenciesOfMyCharacters = new(MaxCharacterCount);
    readonly List<(char, SwipeDirection)> _pairs = new();
    internal void RedistributeKeysOptimally(IReadOnlyDictionary<char, long> characterAppearances, IReadOnlyDictionary<SwipeDirection, double> swipeDirectionPreferences)
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

    internal static void SwapRandomCharacterFromEach(Key key1, Key key2, Random random)
    {
        Debug.Assert(key1.GetValidCharacterCount() > 0 && key2.GetValidCharacterCount() > 0);

        char char1, char2;
        int index1, index2;
        const int centerKeyIndex = (int)SwipeDirection.Center;
        var shouldLoop = true;

        do
        {
            index1 = random.Next(0, MaxCharacterCount);
            index2 = random.Next(0, MaxCharacterCount);
            char1 = key1[index1];
            char2 = key2[index2];

            // make sure a center key is not empty or not a letter
            if (index1 == centerKeyIndex)
            {
                if (char2 == default || !char.IsLetter(char2))
                    continue;
            }

            if (index2 == centerKeyIndex)
            {
                if (char1 == default || !char.IsLetter(char1))
                    continue;
            }

            shouldLoop = char1 == char2;
        } while (shouldLoop);
        // loop if both are `default` or if the implementation changes and both characters can be identical

        key1._characters[index1] = char2;
        key2._characters[index2] = char1;
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

    public char this[SwipeDirection direction] => _characters[(int)direction];
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