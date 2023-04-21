using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

class KeyLayout : IEvolvable<string>
{
    Key[,] _keys;

    readonly char[] _letters;
    public Vector2Int Dimensions { get; }
    Random _random;

    public double Fitness { get; private set; }

    public KeyLayout(int width, int height, string characterSet, int seed)
    {
        _random = new Random(seed);
        _letters = characterSet.ToCharArray();
        _random.Shuffle(_letters);
        _keys = new Key[height, width];
        Dimensions = new Vector2Int(_keys.GetLength(0), _keys.GetLength(1));
        DistributeRandomKeyLayout(_letters);
    }

    public Key GetKey(Vector2Int position) => GetKey(position.X, position.Y);

    public Key GetKey(int x, int y)
    {
        return _keys[y, x];
    }

    void DistributeRandomKeyLayout(char[] characterSet)
    {
        int index = 0;
        int lettersPerKey = _letters.Length / _keys.Length;
        ReadOnlySpan<char> letterSpan = _letters;
        foreach (Key key in _keys)
        {
            var lastIndex = Math.Clamp(index + lettersPerKey, 0, _keys.Length - 1);
            key.RandomlyDistributeCharacters(letterSpan.Slice(index, lastIndex - index), _random);
        }
    }


    Vector2 GetSpaceBarPressPosition(in Vector2Int previousThumbPosition)
    {
        throw new NotImplementedException();
    }

    IReadOnlyList<List<InputAction>> _inputActions = new List<List<InputAction>>()
    {
        new(capacity: 1000),
        new(capacity: 1000),
    };

    public void AddStimulus(string text)
    {
        var input = text.AsSpan();

        InputAction previousTypedKey = default;

        foreach (char c in input)
        {
            if (c == ' ')
            {
                Fitness += CalculateTravelScoreSpaceBar(previousTypedKey, out var spaceKeyAction);
                previousTypedKey = spaceKeyAction;
                continue;
            }

            if (!ThumbKeyboard.LetterSet.Contains(c))
                continue;

            InputAction currentInput = default;

            // todo: is there a better way to search?
            for (var column = 0; column < Dimensions.X; column++)
            for (var row = 0; row < Dimensions.Y; row++)
            {
                Key key = _keys[column, row];
                var contains = key.Contains(c, out var foundDirection);
                if (!contains) continue;
                var thumb = GetWhichThumb(in previousTypedKey, column, Dimensions);
                currentInput = new(key, row, column, foundDirection, thumb);
                break;
            }

            Debug.Assert(currentInput != default);

            int fingerIndex = (int)currentInput.Thumb;
            var previousTypedByThisThumb = _inputActions[fingerIndex][^1];
            Fitness += CalculateTravelScore(in previousTypedKey, in currentInput, in previousTypedByThisThumb);
            previousTypedKey = currentInput;
            _inputActions[fingerIndex].Add(currentInput);
        }
    }

    static double CalculateTravelScore(in InputAction previousInput, in InputAction currentTypedKey, in InputAction previousInputOfThumb)
    {
        var travel = currentTypedKey.KeyPosition - previousInputOfThumb.KeyPosition;
        var travelAngleRadians = Math.Atan2(travel.Y, travel.X) + Math.PI; // 0 - 2PI
        var distanceTraveled = Vector2.Distance(previousInputOfThumb.KeyPosition, currentTypedKey.KeyPosition);
        bool alternatingThumbs = previousInput.Thumb != currentTypedKey.Thumb;
        
        // todo: consider the following: when an input is on the same key as a previous one, i.e. both inputs are on the center key
        // of thumb-key, is it more optimal to use the same finger or to use alternating fingers?
        bool fingerCollision = previousInput.KeyPosition == currentTypedKey.KeyPosition;
       // var generalSwipeDirectionScore = SwipeDirectionWeights[charInput.SwipeDirection]; 
        throw new NotImplementedException();
    }

    double CalculateTravelScoreSpaceBar(InputAction previousTypedKeyOfThumb, out InputAction spaceKeyAction)
    {
        var spaceBarPosition = GetSpaceBarPressPosition(in previousTypedKeyOfThumb.KeyPosition);
        spaceBarPosition.X = (spaceBarPosition.X + previousTypedKeyOfThumb.KeyPosition.X) / 2;

        var travel = spaceBarPosition - previousTypedKeyOfThumb.KeyPosition;
        var travelAngleRadians =
            Math.Atan2(travel.Y, travel.X) + Math.PI; // 0 - 2PI //imperfect bc space bar is wider...
        var distanceTraveled = Math.Abs(spaceBarPosition.Y - previousTypedKeyOfThumb.KeyPosition.Y);
        throw new NotImplementedException();
    }

    Thumb GetWhichThumb(in InputAction previousTypedKey, int xPosition, Vector2Int keyboardDimensions)
    {
        throw new NotImplementedException();
    }
}