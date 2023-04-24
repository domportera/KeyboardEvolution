using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public class KeyboardLayout : IEvolvable<string, Key[,]>
{
    public Key[,] Traits { get; private set; }

    public Vector2Int Dimensions { get; }

    public double Fitness { get; private set; }

    readonly bool _separateStandardSpaceBar;
    readonly double _maxDistancePossible;
    readonly double[,] _positionPreferences;

    readonly Weights _fitnessWeights;
    readonly FrozenDictionary<SwipeDirection, double> _swipeDirectionPreferences;
    
    // Coordinates are determined with (X = 0, Y = 0) being top-left
    public KeyboardLayout(
        Vector2Int dimensions,
        string characterSet, 
        int seed, 
        bool separateStandardSpaceBar,
        double[,] positionPreferences, 
        in Weights weights, 
        FrozenDictionary<SwipeDirection, double> swipeDirectionPreferences)
    {
        _fitnessWeights = weights;
        _swipeDirectionPreferences = swipeDirectionPreferences;
        _positionPreferences = positionPreferences;
        Debug.Assert(positionPreferences.GetLength(0) == dimensions.Y && positionPreferences.GetLength(1) == dimensions.X);
        var random = new Random(seed);
        char[] allCharacters = characterSet.ToCharArray();

        _separateStandardSpaceBar = separateStandardSpaceBar;
        if (!separateStandardSpaceBar)
        {
            allCharacters = allCharacters.Append(' ').ToArray();
        }

        random.Shuffle(allCharacters);
        Traits = new Key[dimensions.Y, dimensions.X];
        Dimensions = dimensions;
        _maxDistancePossible = Vector2.Distance(Vector2.One, dimensions);
        DistributeRandomKeyboardLayout(Traits, allCharacters, random);
    }

    static readonly FrozenDictionary<SwipeDirection, double> SwipeAngles = new Dictionary<SwipeDirection, double>()
    {
        { SwipeDirection.Left, Math.PI },
        { SwipeDirection.UpLeft, 3 * Math.PI / 4 },
        { SwipeDirection.Up, Math.PI / 2 },
        { SwipeDirection.UpRight, Math.PI / 4 },
        { SwipeDirection.Right, 0 },
        { SwipeDirection.DownRight, -Math.PI / 4 },
        { SwipeDirection.Down, -Math.PI / 2 },
        { SwipeDirection.DownLeft, -3 * Math.PI / 4 },
    }.ToFrozenDictionary();


    Key GetKey(int x, int y)
    {
        return Traits[y, x];
    }

    public Key GetKey(Vector2Int position) => GetKey(position.X, position.Y);
    public Key this[Vector2Int index] => GetKey(index.X, index.Y);

    static void DistributeRandomKeyboardLayout(Key[,] keys, char[] characterSet, Random random)
    {
        int index = 0;
        int lettersPerKey = characterSet.Length / keys.Length;
        ReadOnlySpan<char> letterSpan = characterSet.AsSpan();
        foreach (Key key in keys)
        {
            var lastIndex = Math.Clamp(index + lettersPerKey, 0, keys.Length - 1);
            key.RandomlyDistributeCharacters(letterSpan.Slice(index, lastIndex - index), random);
            index = lastIndex;
        }
    }

    readonly IReadOnlyList<List<InputAction>> _inputActions = new List<List<InputAction>>()
    {
        new(capacity: 1000),
        new(capacity: 1000),
    };
    
    public void ResetFitness() => Fitness = 0;

    public void AddStimulus(string text)
    {
        var input = text.AsSpan();

        InputAction previousTypedKey = default;

        foreach (char rawChar in input)
        {
            if (_separateStandardSpaceBar && rawChar == ' ')
            {
                // todo: handle first key press, where there is no previous typed key
                Fitness += CalculateTravelScoreStandardSpaceBar(previousTypedKey, out InputAction spaceKeyAction,
                    _maxDistancePossible);
                previousTypedKey = spaceKeyAction;
                continue;
            }

            // todo : implement different shift schemes: shift once then auto-lower, caps lock, one shift swipe, shift swipe on each side, etc
            // bool isUpperCase = char.IsUpper(rawChar);
            char c = char.ToLowerInvariant(rawChar); // lowercase only - ignore case

            if (!KeyboardLayoutTrainer.CharacterSetDict.Contains(c))
                continue;

            InputAction currentInput = default;

            // todo: is there a better way to search?
            for (var column = 0; column < Dimensions.X; column++)
            for (var row = 0; row < Dimensions.Y; row++)
            {
                Key key = Traits[row, column];
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
            Fitness += CalculateTravelScore(in previousTypedKey, in currentInput, in previousTypedByThisThumb,
                _maxDistancePossible);
            previousTypedKey = currentInput;
            _inputActions[fingerIndex].Add(currentInput);
        }
    }
    
    public void OverwriteTraits(Key[,] newKeys)
    {        
        Debug.Assert(newKeys.GetLength(0) == Dimensions.Y && 
                     newKeys.GetLength(1) == Dimensions.X);

        for(int y = 0; y < Dimensions.Y; y++)
        for (int x = 0; x < Dimensions.X; x++)
        {
            Traits[y,x].OverwriteKeysWith(newKeys[y,x]);
        }
    }

    public void Mutate(double amount)
    {
        ResetFitness();
        
        // shuffle % of key characters with each other
        // key.swaprandomcharacter
        throw new NotImplementedException();
    }

    public void Kill()
    {
        ResetFitness();
    }

    double CalculateTravelScore(in InputAction currentTypedKey, in InputAction previousInputOfThumb,
        in InputAction previousInput, double maxDistancePossible)
    {
        bool sameKey = previousInput.KeyPosition == currentTypedKey.KeyPosition;
        bool sameKeyAndSwipe = sameKey && previousInput.SwipeDirection == currentTypedKey.SwipeDirection;

        if (sameKeyAndSwipe)
        {
            return _fitnessWeights.CalculateScore(
                closeness01: 1,
                trajectory01: currentTypedKey.SwipeDirection switch // repeated swipes on the same key are cumbersome
                {
                    SwipeDirection.Center => 1,
                    SwipeDirection.Left
                        or SwipeDirection.Right
                        or SwipeDirection.Up
                        or SwipeDirection.Down => 0.35,
                    _ => 0, // diagonals are the worst for this
                },
                handAlternation01: 1, // not technically hand alternation, but there's no reason to penalize double-letters
                handCollisionAvoidance01: 1,
                positionalPreference01: GetPreferredPositionScore(currentTypedKey.KeyPosition),
                swipeDirectionPreference01: _swipeDirectionPreferences[currentTypedKey.SwipeDirection]
            );
        }

        var travel = currentTypedKey.KeyPosition - previousInputOfThumb.KeyPosition;
        var travelAngleRadians = AngleUtils.AngleFromVector2(travel); // 0 - 2PI 
        var trajectoryCorrectness =
            1 - AngleUtils.NormalizedAngleDifference(
                travelAngleRadians,
                SwipeAngles[previousInputOfThumb.SwipeDirection]);

        float distanceTraveled = Vector2.Distance(previousInputOfThumb.KeyPosition, currentTypedKey.KeyPosition);
        double distanceEffectiveness = 1 - distanceTraveled / maxDistancePossible;

        bool alternatingThumbs = previousInput.Thumb != currentTypedKey.Thumb;

        return _fitnessWeights.CalculateScore(
            closeness01: distanceEffectiveness,
            trajectory01: trajectoryCorrectness,
            handAlternation01: alternatingThumbs
                ? 1
                : 0, // not technically hand alternation, but there's no reason to penalize double-letters
            handCollisionAvoidance01: previousInput.KeyPosition.X == currentTypedKey.KeyPosition.X ? 0 : 1,
            positionalPreference01: GetPreferredPositionScore(currentTypedKey.KeyPosition),
            swipeDirectionPreference01: _swipeDirectionPreferences[currentTypedKey.SwipeDirection]
        );
    }

    double GetPreferredPositionScore(in Vector2Int position) => _positionPreferences[position.Y, position.X];

    double CalculateTravelScoreStandardSpaceBar(InputAction previousTypedKeyOfThumb, out InputAction spaceKeyAction,
        double maxDistancePossible)
    {
        Vector2 spaceBarPosition = GetSpaceBarPressPosition(in previousTypedKeyOfThumb.KeyPosition);
        Vector2 travel = spaceBarPosition - previousTypedKeyOfThumb.KeyPosition;
        SwipeDirection previousSwipeDirection = previousTypedKeyOfThumb.SwipeDirection;

        double trajectoryCorrectness;
        Debug.Assert(previousTypedKeyOfThumb.SwipeDirection != SwipeDirection.None);
        if (previousSwipeDirection == SwipeDirection.Center)
        {
            trajectoryCorrectness = 1;
        }
        else
        {
            var travelAngleRadians = AngleUtils.AngleFromVector2(travel); // 0 - 2PI 
            trajectoryCorrectness =
                1 - AngleUtils.NormalizedAngleDifference(travelAngleRadians, SwipeAngles[previousSwipeDirection]);
        }

        float distanceTraveled = Math.Abs(spaceBarPosition.Y - previousTypedKeyOfThumb.KeyPosition.Y);
        double distanceEffectiveness = 1 - distanceTraveled / maxDistancePossible;

        spaceKeyAction = new InputAction(
            column: (int)Math.Round(spaceBarPosition.X),
            row: (int)Math.Round(spaceBarPosition.Y),
            swipeDirection: SwipeDirection.Center,
            thumb: previousTypedKeyOfThumb.Thumb);

        return _fitnessWeights.CalculateScore(
            closeness01: distanceEffectiveness,
            trajectory01: trajectoryCorrectness,
            handAlternation01: 1, // spacebar can always use opposite hand
            handCollisionAvoidance01: 1, // spacebar is wide enough to never worry about overlap
            positionalPreference01: 0.5, // spacebar position is relatively standardized, todo: allow non-standard space position? 3x4 layout?
            swipeDirectionPreference01: _swipeDirectionPreferences[spaceKeyAction.SwipeDirection]
        );

        Vector2 GetSpaceBarPressPosition(in Vector2Int previousThumbPosition)
        {
            var x = (previousThumbPosition.X + Dimensions.X / 2f) /
                    2f; // closer to center - avg of center + prev position
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