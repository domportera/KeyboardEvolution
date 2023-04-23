using System.Diagnostics;
using Core.Util;

namespace ThumbKey;

class Key
{
    const int MaxCharacterCount = 9;
    readonly char[] _characters = new char[MaxCharacterCount];
    readonly List<int> _indexesContainingCharacter = new(capacity: MaxCharacterCount);

    public Key(ReadOnlySpan<char> characters, Random random)
    {
        RandomlyDistributeCharacters(characters, random);
    }

    void PopulateIndexesContainingCharacter()
    {
        _indexesContainingCharacter.Clear();
        for (int i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] != default)
                _indexesContainingCharacter.Add(i);
        }
    }

    public void RandomlyDistributeCharacters(ReadOnlySpan<char> characters, Random random)
    {
        Debug.Assert(characters.Length <= _characters.Length);
        Debug.Assert(characters.Length > 0);
        Clear();
        for (int i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            _characters[i] = character;
        }

        random.Shuffle(_characters);
        PopulateIndexesContainingCharacter();
        EnsureCenterHasCharacter(random, _indexesContainingCharacter);

        Debug.Assert(_characters[(int)SwipeDirection.Center] != default);
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
        indexesContainingCharacter[index] = centerIndex;
    }

    public void Clear()
    {
        for (int i = 0; i < _characters.Length; i++)
            _characters[i] = default;
        
        _indexesContainingCharacter.Clear();
    }

    internal bool Contains(char c, out SwipeDirection direction)
    {
        Debug.Assert((int)SwipeDirection.None == -1);
        
        var index = _characters.AsSpan().IndexOf(c);
        direction = (SwipeDirection)index;
        return index != -1;
    }

    public static void SwapRandomCharacters(Key key1, Key key2, Random random)
    {
        Debug.Assert(key1._indexesContainingCharacter.Count > 0 && key2._indexesContainingCharacter.Count > 0);
        
        char char1, char2;
        int index1, index2;

        do
        {
            index1 = random.Next(0, MaxCharacterCount);
            index2 = random.Next(0, MaxCharacterCount);
            char1 = key1[index1];
            char2 = key2[index2];
        } while (char1 == char2); 
        // loop if both are `default` or if the implementation changes and both characters can be identical

        key1._characters[index1] = char2;
        key2._characters[index2] = char1;
    }

    public char this[SwipeDirection direction] => _characters[(int)direction];
    public char this[Vector2Int position] => _characters[position.X + position.X * position.Y + position.Y];
    public char this[int position] => _characters[position];
}