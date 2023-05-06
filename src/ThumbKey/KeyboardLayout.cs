using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public class KeyboardLayout : IEvolvable<TextRange, Key[,]>
{
    public Key[,] Traits { get; private set; }

    public Vector2Int Dimensions { get; }

    public double Fitness { get; private set; }

    readonly bool _separateStandardSpaceBar;
    readonly double _maxDistancePossible;
    readonly double _maxDistancePossibleStandardSpacebar;
    readonly double[,] _positionPreferences;

    readonly Weights _fitnessWeights;
    readonly FrozenDictionary<SwipeDirection, double> _swipeDirectionPreferences;
    Random _random;
    readonly InputAction[] _previousInputs = new InputAction[2];

    InputAction _previousInputAction;

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
        Debug.Assert(positionPreferences.GetLength(0) == dimensions.Y &&
                     positionPreferences.GetLength(1) == dimensions.X);
        var random = new Random(seed);
        _random = random;
        char[] allCharacters = characterSet.ToCharArray();

        _separateStandardSpaceBar = separateStandardSpaceBar;
        if (!separateStandardSpaceBar)
        {
            allCharacters = allCharacters.Append(' ').ToArray();
        }
        else
        {
            // the space bar is always in the bottom row, so we can calculate the max distance possible
            _maxDistancePossibleStandardSpacebar = Vector2.Distance(Vector2.Zero, dimensions + (0, 1));
        }

        Debug.Assert((int)Thumb.Left == 0 && (int)Thumb.Right == 1);
        _previousInputs[(int)Thumb.Left] = new(0, Dimensions.Y / 2, SwipeDirection.Center, Thumb.Left);
        _previousInputs[(int)Thumb.Right] = new(Dimensions.X - 1, Dimensions.Y / 2, SwipeDirection.Center, Thumb.Right);
        _previousInputAction =
            _previousInputs[(int)Thumb.Right]; // default key - assumes user opens text field with right thumb

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
        Vector2Int keyDimensions = (keys.GetLength(1), keys.GetLength(1));
        int index = 0;
        int charactersPerKey = characterSet.Length / keys.Length;

        EnsureAllKeysWillHaveALetter();

        ReadOnlySpan<char> characterSpan = characterSet.AsSpan();
        for (int y = 0; y < keyDimensions.Y; y++)
        for (int x = 0; x < keyDimensions.X; x++)
        {
            var lastIndex = Math.Clamp(index + charactersPerKey, 0, characterSet.Length - 1);
            var thisKeysCharacters = characterSpan.Slice(index, lastIndex - index);
            keys[y, x] = new Key(thisKeysCharacters, random);
            index = lastIndex;
        }

        void EnsureAllKeysWillHaveALetter()
        {
            bool allCharactersWillHaveLetters = false;
            do
            {
                random.Shuffle(characterSet);
                allCharactersWillHaveLetters = true;
                for (int i = 0; i < characterSet.Length - charactersPerKey; i++)
                {
                    var hasLetters = false;
                    for (int j = 0; j < charactersPerKey; j++)
                    {
                        hasLetters |= char.IsLetter(characterSet[i + j]);
                    }

                    if (hasLetters) continue;
                    allCharactersWillHaveLetters = false;
                    break;
                }
            } while (!allCharactersWillHaveLetters);
        }
    }

    public void ResetFitness() => Fitness = 0;

    TextRange? _currentStimulus;

    public void Evaluate()
    {
        ReadOnlySpan<char> input = _currentStimulus!.Text.AsSpan(_currentStimulus.Range);
        foreach (char rawChar in input)
        {
            if (_separateStandardSpaceBar && rawChar == ' ')
            {
                // thumbs to spacebar can always alternate
                InputAction previousInputActionOfThisThumb = _previousInputAction.Thumb == Thumb.Left
                    ? _previousInputs[(int)Thumb.Right]
                    : _previousInputs[(int)Thumb.Left];

                Fitness += CalculateTravelScoreStandardSpaceBar(in previousInputActionOfThisThumb,
                    out InputAction spaceKeyAction,
                    _maxDistancePossibleStandardSpacebar);
                _previousInputAction = spaceKeyAction;
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

                var thumb = GetWhichThumb(in _previousInputAction, column, Dimensions);
                currentInput = new InputAction(column, row, foundDirection, thumb);
                break;
            }

            Debug.Assert(currentInput != default);

            int fingerIndex = (int)currentInput.Thumb;
            var previousTypedByThisThumb = _previousInputs[fingerIndex];
            // todo: handle first key press, where there is no previous typed key
            Fitness += CalculateTravelScore(
                currentTypedKey: in currentInput,
                previousInputOfThumb: in previousTypedByThisThumb,
                previousInput: in _previousInputAction,
                _maxDistancePossible);
            _previousInputAction = currentInput;
            _previousInputs[fingerIndex] = currentInput;
        }
    }

    public void SetStimulus(TextRange rangeInfo)
    {
        _currentStimulus = rangeInfo;
    }

    public void OverwriteTraits(Key[,] newKeys)
    {
        Debug.Assert(newKeys.GetLength(0) == Dimensions.Y &&
                     newKeys.GetLength(1) == Dimensions.X);

        for (int y = 0; y < Dimensions.Y; y++)
        for (int x = 0; x < Dimensions.X; x++)
        {
            Traits[y, x].OverwriteKeysWith(newKeys[y, x]);
        }
    }

    public void Mutate(double percentageOfCharactersToMutate)
    {
        // shuffle % of key characters with each other
        Key[] allKeys = new Key[Dimensions.X * Dimensions.Y];
        for (int y = 0; y < Dimensions.Y; y++)
        for (int x = 0; x < Dimensions.X; x++)
        {
            allKeys[y * Dimensions.X + x] = Traits[y, x];
        }

        _random.Shuffle(allKeys);

        // todo: this is ugly af

        double characterCount = allKeys.Length * Key.MaxCharacterCount;
        double quantityToShuffle = characterCount * percentageOfCharactersToMutate;
        double quantityPerKey = quantityToShuffle / allKeys.Length;
        int quantityPerSwap = (int)Math.Round(quantityPerKey / 2);

        bool singleSwapOnly = quantityPerSwap == 0;
        int iterator = 1;

        if (singleSwapOnly)
        {
            iterator = 2;
            quantityPerSwap = 1;
        }

        // we use "iterator" here to determine if we've moving through the array one at a time or two at a time.
        // we only move two at a time if we the quantityPerSwap rounds down to zero,
        // so we at least ensure that every pair is swapped once.
        // otherwise we iterate one at a time, so every pair is swapped twice - once with each of its neighbors.
        // that is why above
        for (int i = 0; i < allKeys.Length - 1; i += iterator)
        for (int j = 0; j < quantityPerSwap; j++)
        {
            Key.SwapRandomCharacterFromEach(allKeys[i], allKeys[i + 1], _random);
        }

        // the above loop doesn't wrap around the array so the first and last elements are swapped, so we do that here. 
        // awkward yes, but more performant and readable than a ternary statement in the above loop.
        if (!singleSwapOnly)
        {
            for (int i = 0; i < quantityPerSwap; i++)
                Key.SwapRandomCharacterFromEach(allKeys[0], allKeys[^1], _random);
        }
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

        double trajectoryCorrectness = 1;

        if (previousInputOfThumb.SwipeDirection != SwipeDirection.Center)
        {
            var travel = currentTypedKey.KeyPosition - previousInputOfThumb.KeyPosition;
            var travelAngleRadians = AngleUtils.AngleFromVector2(travel); // 0 - 2PI 
            trajectoryCorrectness =
                1 - AngleUtils.NormalizedAngleDifference(
                    travelAngleRadians,
                    SwipeAngles[previousInputOfThumb.SwipeDirection]);
        }

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

    double CalculateTravelScoreStandardSpaceBar(in InputAction previousTypedKeyOfThumb, out InputAction spaceKeyAction,
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