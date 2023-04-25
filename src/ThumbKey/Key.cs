using System.Diagnostics;
using System.Text;
using Core.Util;

namespace ThumbKey;

public class Key
{
    public const int MaxCharacterCount = 9;
    readonly char[] _characters;

    private Key(Vector2Int dimensions)
    {
        throw new NotImplementedException();
        // disallow default construction
    }

    internal Key(ReadOnlySpan<char> characters, Random random)
    {
        _characters = new char[MaxCharacterCount];
        double side = Math.Sqrt(MaxCharacterCount);
        Debug.Assert(side % 1 == 0); // must be square... for now....
        
        Dimensions = new((int)side, (int)side);
        
        RandomlyDistributeCharacters(characters, random);
    }

    Key(char[] characters)
    {
        _characters = characters;
    }

    public Key Duplicate() => new Key(_characters.ToArray());

    public void OverwriteKeysWith(Key other)
    {
        other._characters.CopyTo(_characters.AsSpan());
    }

    public void RandomlyDistributeCharacters(ReadOnlySpan<char> characters, Random random)
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
        if (_characters[centerIndex] != default) return;

        int index = random.Next(indexesContainingCharacter.Count);
        int charIndexToStealFrom = indexesContainingCharacter[index];
        char stolenCharacter = _characters[charIndexToStealFrom];

        _characters[centerIndex] = stolenCharacter;
        _characters[charIndexToStealFrom] = default;
    }

    internal bool Contains(char c, out SwipeDirection direction)
    {
        Debug.Assert((int)SwipeDirection.None == -1);
        
        var index = _characters.AsSpan().IndexOf(c);
        direction = (SwipeDirection)index;
        return index != -1;
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
    public readonly Vector2Int Dimensions;
}