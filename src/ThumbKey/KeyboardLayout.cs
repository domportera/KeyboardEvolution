using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

class KeyboardLayout : IEvolvable<string>
{
    Key[,] _keys;

    readonly char[] _letters;
    public Vector2Int Dimensions { get; }
    readonly Random _random;

    public double Fitness { get; private set; }

    public KeyboardLayout(int width, int height, string characterSet, int seed)
    {
        _random = new Random(seed);
        _letters = characterSet.ToCharArray();
        _random.Shuffle(_letters);
        _keys = new Key[height, width];
        Dimensions = new Vector2Int(_keys.GetLength(0), _keys.GetLength(1));
        DistributeRandomKeyboardLayout(_keys, _letters, _random);
    }

    public Key GetKey(Vector2Int position) => GetKey(position.X, position.Y);
    public Key this[Vector2Int index] => GetKey(index.X, index.Y);

    static readonly ImmutableDictionary<SwipeDirection, double> Directions;
    static KeyboardLayout()
    {
        var dict = new Dictionary<SwipeDirection, double>()
        {
            { SwipeDirection.Left, Math.PI },
            { SwipeDirection.UpLeft, 3 * Math.PI / 4 },
            { SwipeDirection.Up, Math.PI / 2 },
            { SwipeDirection.UpRight, Math.PI / 4 },
            { SwipeDirection.Right, 0 },
            { SwipeDirection.DownRight, -Math.PI / 4 },
            { SwipeDirection.Down, -Math.PI / 2 },
            { SwipeDirection.DownLeft, -3 * Math.PI / 4 },
        };

        Directions = dict
            .ToImmutableDictionary(
                x => x.Key,
                x => x.Value);
    }

    public Key GetKey(int x, int y)
    {
        return _keys[y, x];
    }

    static void DistributeRandomKeyboardLayout(Key[,] keys, char[] characterSet, Random random)
    {
        int index = 0;
        int lettersPerKey = characterSet.Length / keys.Length;
        ReadOnlySpan<char> letterSpan = characterSet.AsSpan();
        foreach (Key key in keys)
        {
            var lastIndex = Math.Clamp(index + lettersPerKey, 0, keys.Length - 1);
            key.RandomlyDistributeCharacters(letterSpan.Slice(index, lastIndex - index), random);
        }
    }


    readonly IReadOnlyList<List<InputAction>> _inputActions = new List<List<InputAction>>()
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
                currentInput = new InputAction(column, row, foundDirection, thumb);
                break;
            }

            Debug.Assert(currentInput != default);

            int fingerIndex = (int)currentInput.Thumb;
            var previousTypedByThisThumb = _inputActions[fingerIndex][^1];
            // todo: handle first key press, where there is no previous typed key
            Fitness += CalculateTravelScore(in previousTypedKey, in currentInput, in previousTypedByThisThumb);
            previousTypedKey = currentInput;
            _inputActions[fingerIndex].Add(currentInput);
        }
    }

    static double CalculateTravelScore(in InputAction previousInput, in InputAction currentTypedKey,
        in InputAction previousInputOfThumb)
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
        Vector2 spaceBarPosition = GetSpaceBarPressPosition(in previousTypedKeyOfThumb.KeyPosition);

        Vector2 travel = spaceBarPosition - previousTypedKeyOfThumb.KeyPosition;
        
        SwipeDirection previousSwipeDirection = previousTypedKeyOfThumb.SwipeDirection;

        float travelScore;
        double angleTravelEffectiveness;
        Debug.Assert(previousTypedKeyOfThumb.SwipeDirection != SwipeDirection.None);
        if (previousSwipeDirection == SwipeDirection.Center)
        {
            angleTravelEffectiveness = 1;
        }
        else
        {
            var travelAngleRadians = Math.Atan2(travel.Y, travel.X); // 0 - 2PI 
            angleTravelEffectiveness = 1 - AngleComparison.NormalizedAngleDifference(travelAngleRadians, Directions[previousSwipeDirection]);
        }

        float maxDistancePossible = Vector2.Distance(Vector2.One, Dimensions);
        float distanceTraveled = Math.Abs(spaceBarPosition.Y - previousTypedKeyOfThumb.KeyPosition.Y);
        var distanceEffectiveness = (double)(1 - distanceTraveled / maxDistancePossible);
        
        
        throw new NotImplementedException();

        Vector2 GetSpaceBarPressPosition(in Vector2Int previousThumbPosition)
        {
            var x = previousThumbPosition.X + Dimensions.X / 2f / 2f; // closer to center
            var y = Dimensions.Y; // below other keys
            return new Vector2(x, y);
        }
    }

    // todo: unit test?
    // todo: can do these Range calculations in constructor
    static Thumb GetWhichThumb(in InputAction previousTypedKey, int xPosition, in Vector2Int keyboardDimensions)
    {
        Debug.Assert(xPosition >= 0);
        
        var leftMax = (int)Math.Floor(keyboardDimensions.X / 2f);
        var rightMin = (int)Math.Ceiling(keyboardDimensions.X / 2f);
        
        Debug.Assert(rightMin - leftMax <= 1); // ensure there are no gaps in between

        Range leftRange = new(min: 0, max: leftMax);
        Range rightRange = new(min: leftRange.Max, max: rightMin);

        bool leftContains = leftRange.Contains(xPosition);
        bool rightContains = rightRange.Contains(xPosition);

        Thumb thumb = leftContains && rightContains
            ? OppositeThumb(previousTypedKey.Thumb)
            : leftContains
                ? Thumb.Left
                : Thumb.Right;

        return thumb;

        static Thumb OppositeThumb(Thumb thumb) =>
            thumb == Thumb.Left
                ? Thumb.Right
                : Thumb.Left;

    }

    struct TravelEfficiency
    {
        public readonly double AngleAdvantage;
        public readonly double DistanceAdvantage;

        public TravelEfficiency(double angleAdvantage, double distanceAdvantage)
        {
            AngleAdvantage = angleAdvantage;
            DistanceAdvantage = distanceAdvantage;
        }
    }

    readonly struct Range
    {
        public readonly int Min;
        public readonly int Max;

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(int value) => value >= Min && value <= Max;
        public bool ContainsExclusive(int value) => value > Min && value < Max;
    }
}