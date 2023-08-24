using System.Collections.Frozen;
using System.Diagnostics;
using Core.Util;

namespace ThumbKey;

public class Key
{
    public static readonly int MaxCharacterCount = Enum.GetValues<SwipeDirection>().Length - 1;
    readonly char[] _characters = new char[MaxCharacterCount];

    internal Key(ReadOnlySpan<char> characters) : this()
    {
        _characters = characters.ToArray();
    }

    internal Key(char center, ReadOnlySpan<char> cardinal, ReadOnlySpan<char> diagonal, Random random) : this()
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
    readonly List<CharSwipeDirection> _pairs = new();

    readonly struct CharSwipeDirection
    {
        public readonly char Char;
        public readonly SwipeDirection Swipe;

        public CharSwipeDirection(char @char, SwipeDirection swipe)
        {
            Char = @char;
            Swipe = swipe;
        }
    }

    readonly Dictionary<SwipeDirection, float> _swipeDirectionPreferencesDict = new();

    internal void RedistributeKeysOptimally(IReadOnlyDictionary<char, long> characterAppearances,
        float[] swipeDirectionPreferences)
    {
        _frequenciesOfMyCharacters.Clear();
        // redistribute keys so that the most common characters are in the center
        // and that cardinal directions are filled by the characters in the corners
        // and that the least common characters are in the corners if cardinals are filled

        foreach (var c in _characters)
        {
            if (c == default) continue;
            _frequenciesOfMyCharacters[c] = characterAppearances[c];
        }

        var charsSortedByFrequency = _frequenciesOfMyCharacters
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToArray();

        // pair up the most common characters with the most preferred swipe directions
        // based on this keys position in the layout

        for (int i = 0; i < swipeDirectionPreferences.Length; i++)
        {
            _swipeDirectionPreferencesDict[(SwipeDirection)i] = swipeDirectionPreferences[i];
        }

        var swipeDirectionPreferencesSorted = _swipeDirectionPreferencesDict
            .OrderByDescending(x => x.Value) // order by descending swipe preference
            .Select(x => x.Key)
            .ToArray();

        _pairs.Clear();
        for (int i = 0; i < charsSortedByFrequency.Length; i++)
        {
            var character = charsSortedByFrequency[i];
            var swipeDirection = swipeDirectionPreferencesSorted[i];
            _pairs.Add(new(character, swipeDirection));
        }

        // clear char array
        Array.Clear(_characters, 0, _characters.Length);

        // assign chars to their new swipe directions
        foreach (var pair in _pairs)
        {
            var index = (int)pair.Swipe;
            _characters[index] = pair.Char;
        }
    }

    enum SwipeType
    {
        Center,
        Cardinal,
        Diagonal
    }

    static readonly SwipeDirection[] CardinalDirections =
        { SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right };

    static readonly SwipeDirection[] DiagonalDirections =
        { SwipeDirection.UpLeft, SwipeDirection.UpRight, SwipeDirection.DownLeft, SwipeDirection.DownRight };

    internal static void SwapRandomCharacterFromEach(Key key1, Key key2, Random random)
    {
        Debug.Assert(key1.GetValidCharacterCount() > 0 && key2.GetValidCharacterCount() > 0);

        char char1, char2;
        var shouldLoop = true;
        SwipeDirection swipeDirection1;
        SwipeDirection swipeDirection2;

        do
        {
            SwipeType swipeType1 = (SwipeType)random.Next(0, 3);
            SwipeType swipeType2 = swipeType1;

            if (KeyboardLayoutTrainer.AllowCardinalDiagonalSwaps && swipeType1 != SwipeType.Center)
            {
                swipeType2 = (SwipeType)random.Next(1, 3);
            }

            swipeDirection1 = GetRandomSwipeDirection(swipeType1, random);
            swipeDirection2 = GetRandomSwipeDirection(swipeType2, random);


            char1 = key1[swipeDirection1];
            char2 = key2[swipeDirection2];

            shouldLoop = char1 == char2;
        } while (shouldLoop);

        key1[swipeDirection1] = char2;
        key2[swipeDirection2] = char1;

        static SwipeDirection GetRandomSwipeDirection(SwipeType swipeType, Random random)
        {
            SwipeDirection direction = SwipeDirection.Center;
            switch (swipeType)
            {
                case SwipeType.Center:
                    direction = SwipeDirection.Center;
                    break;
                case SwipeType.Cardinal:
                    direction = CardinalDirections[random.Next(0, 4)];
                    break;
                case SwipeType.Diagonal:
                    direction = DiagonalDirections[random.Next(0, 4)];
                    break;
            }

            return direction;
        }
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

    public char this[SwipeDirection direction]
    {
        get => _characters[(int)direction];
        private set => _characters[(int)direction] = value;
    }

    public char this[Array2DCoords position] =>
        _characters[position.ColumnX + position.ColumnX * position.RowY + position.RowY];

    public char this[int position] => _characters[position];
    public int Length => _characters.Length;
    public IReadOnlyList<char> Characters => _characters;

    readonly List<int> _freeIndices = new(MaxCharacterCount);

    public bool TryAddCharacter(char character, Random random)
    {
        _freeIndices.Clear();
        for (int i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] == default)
                _freeIndices.Add(i);
        }

        if (_freeIndices.Count == 0)
            return false;

        int randomIndex = random.Next(0, _freeIndices.Count);
        _characters[_freeIndices[randomIndex]] = character;
        return true;
    }

    public bool Contains(char c) => _characters.Contains(c);
}