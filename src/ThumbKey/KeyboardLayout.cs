using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public class KeyboardLayout : IEvolvable<TextRange, Key[,]>
{
    public Key[,] Traits { get; }
    FrozenDictionary<char, InputPositionInfo> _charPositionDict;

    record struct InputPositionInfo
    {
        public InputPositionInfo(int column, int row, SwipeDirection swipeDirection, Key key)
        {
            Column = column;
            Row = row;
            SwipeDirection = swipeDirection;
            Key = key;
        }

        internal int Column { get; }
        internal int Row { get; }
        internal SwipeDirection SwipeDirection { get; }
        internal Key Key { get; }
    }

    public Vector2Int Dimensions { get; }

    public double Fitness { get; private set; }

    readonly bool _separateStandardSpaceBar;
    readonly double _maxDistancePossible;
    readonly double _maxDistancePossibleStandardSpacebar;
    readonly double[,] _positionPreferences;

    readonly Weights _fitnessWeights;
    readonly FrozenDictionary<SwipeDirection, double> _swipeDirectionPreferences;
    readonly FrozenDictionary<SwipeDirection, double>[,] _keySpecificSwipeDirectionPreferences;
    readonly Random _random;
    readonly InputAction[] _previousInputs = new InputAction[2];

    InputAction _previousInputAction;

    readonly FrozenSet<char> _characterSet;
    Dictionary<char, long> _characterFrequencies;

    // Coordinates are determined with (X = 0, Y = 0) being top-left
    public KeyboardLayout(
        Vector2Int dimensions,
        FrozenSet<char> characterSet,
        int seed,
        bool separateStandardSpaceBar,
        double[,] positionPreferences,
        in Weights weights,
        double keySpecificSwipeDirectionWeight,
        FrozenDictionary<SwipeDirection, double> swipeDirectionPreferences,
        Key[,]? startingLayout)
    {
        _characterSet = characterSet;
        _characterFrequencies = new(CharacterFrequencies.Frequencies);
        _fitnessWeights = weights;
        _swipeDirectionPreferences = swipeDirectionPreferences;
        _positionPreferences = positionPreferences;
        var random = new Random(seed);
        _random = random;

        char[] allCharacters = characterSet.ToArray();

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

        Traits = new Key[dimensions.Y, dimensions.X];
        Dimensions = dimensions;
        _keySpecificSwipeDirectionPreferences =
            GenerateKeySpecificSwipeDirections(keySpecificSwipeDirectionWeight, swipeDirectionPreferences);

        Debug.Assert((int)Thumb.Left == 0 && (int)Thumb.Right == 1);
        _previousInputs[(int)Thumb.Left] = new(0, dimensions.Y / 2, SwipeDirection.Center, Thumb.Left);
        _previousInputs[(int)Thumb.Right] = new(dimensions.X - 1, dimensions.Y / 2, SwipeDirection.Center, Thumb.Right);
        _previousInputAction =
            _previousInputs[(int)Thumb.Right]; // default key - assumes user opens text field with right thumb

        _maxDistancePossible = Vector2.Distance(Vector2.One, dimensions);

        if (startingLayout is not null)
        {
            for (int y = 0; y < dimensions.Y; y++)
            for (int x = 0; x < dimensions.X; x++)
            {
                Traits[y, x] = new(startingLayout[y, x]);
            }

            _charPositionDict = GenerateCharacterPositionDictionary(Traits);
        }
        else
        {
            DistributeRandomKeyboardLayout(Traits, allCharacters, random, out _charPositionDict);
        }
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

    static void DistributeRandomKeyboardLayout(Key[,] keys, char[] characterSet, Random random,
        out FrozenDictionary<char, InputPositionInfo> charPositionDict)
    {
        Vector2Int layoutDimensions = (keys.GetLength(1), keys.GetLength(1));
        int index = 0;
        int charactersPerKey = characterSet.Length / keys.Length;

        EnsureAllKeysWillHaveALetter();

        ReadOnlySpan<char> characterSpan = characterSet.AsSpan();
        for (int y = 0; y < layoutDimensions.Y; y++)
        for (int x = 0; x < layoutDimensions.X; x++)
        {
            var lastIndex = Math.Clamp(index + charactersPerKey, 0, characterSet.Length - 1);
            var thisKeysCharacters = characterSpan.Slice(index, lastIndex - index);
            keys[y, x] = new Key(thisKeysCharacters, random);
            index = lastIndex;
        }

        var remainingKeys = characterSet.Length - charactersPerKey * keys.Length;
        for (int i = 0; i < remainingKeys; i++)
        {
            var key = keys[random.Next(layoutDimensions.Y), random.Next(layoutDimensions.X)];
            bool added = key.TryAddCharacter(characterSet[index + i], random);
            if (!added) i--;
        }

        Debug.Assert(index + remainingKeys == characterSet.Length);

        void EnsureAllKeysWillHaveALetter()
        {
            bool allCharactersWillHaveLetters = false;
            do
            {
                random.Shuffle(characterSet);
                allCharactersWillHaveLetters = true;
                for (int i = 0; i < characterSet.Length - charactersPerKey; i += charactersPerKey)
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

        charPositionDict = GenerateCharacterPositionDictionary(keys);
    }

    static FrozenDictionary<char, InputPositionInfo> GenerateCharacterPositionDictionary(Key[,] keys)
    {
        var dict = new Dictionary<char, InputPositionInfo>();
        for (int y = 0; y < keys.GetLength(0); y++)
        for (int x = 0; x < keys.GetLength(1); x++)
        {
            var key = keys[y, x];
            for (int i = 0; i < Key.MaxCharacterCount; i++)
            {
                if (key[i] == default)
                    continue;

                dict.Add(key[i], new InputPositionInfo(x, y, (SwipeDirection)i, key));
            }
        }

        return dict.ToFrozenDictionary();
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

                Fitness += CalculateTravelScoreStandardSpaceBar(
                    in previousInputActionOfThisThumb,
                    out InputAction spaceKeyAction,
                    _maxDistancePossibleStandardSpacebar);
                _previousInputAction = spaceKeyAction;
                continue;
            }

            // todo : implement different shift schemes: shift once then auto-lower, caps lock, one shift swipe, shift swipe on each side, etc
            // bool isUpperCase = char.IsUpper(rawChar);
            char c = char.ToLowerInvariant(rawChar); // lowercase only - ignore case

            if (!_characterSet.Contains(c))
                continue;

            _characterFrequencies[c]++;

            var inputPositionInfo = _charPositionDict[c];
            var thumb = GetWhichThumb(in _previousInputAction, inputPositionInfo.Column, Dimensions);
            InputAction currentInput = new(inputPositionInfo.Column, inputPositionInfo.Row,
                inputPositionInfo.SwipeDirection, thumb);

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

        // iterate through keys 
        for (int y = 0; y < Dimensions.Y; y++)
        for (int x = 0; x < Dimensions.X; x++)
        {
            Traits[y, x].RedistributeKeysOptimally(_characterFrequencies, _keySpecificSwipeDirectionPreferences[y, x]);
        }

        _charPositionDict = GenerateCharacterPositionDictionary(Traits);
    }

    FrozenDictionary<SwipeDirection, double>[,] GenerateKeySpecificSwipeDirections(double keysTowardsCenterWeight,
        IReadOnlyDictionary<SwipeDirection, double> swipeDirectionPreferences)
    {
        var keySpecificSwipeDirections = new FrozenDictionary<SwipeDirection, double>[Dimensions.Y, Dimensions.X];
        for (int y = 0; y < Dimensions.Y; y++)
        {
            for (int x = 0; x < Dimensions.X; x++)
            {
                // Identifying the keys position on the keyboard.
                bool isLeftEdge = x == 0;
                bool isRightEdge = x == Dimensions.X - 1;
                bool isTopEdge = y == 0;
                bool isBottomEdge = y == Dimensions.Y - 1;

                // Initialize a new dictionary to store swipe preferences based on position.
                var positionBasedSwipePreferences = new Dictionary<SwipeDirection, double>(swipeDirectionPreferences);

                positionBasedSwipePreferences[SwipeDirection.Center] *= (1 + keysTowardsCenterWeight);

                if (isLeftEdge && isTopEdge) // Top-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge && isTopEdge) // Top-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isLeftEdge && isBottomEdge) // Bottom-left corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge && isBottomEdge) // Bottom-right corner
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isLeftEdge) // Left edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Right] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                }
                else if (isRightEdge) // Right edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Left] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isTopEdge) // Top edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Down] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.DownLeft] *= (1 + keysTowardsCenterWeight);
                }
                else if (isBottomEdge) // Bottom edge
                {
                    positionBasedSwipePreferences[SwipeDirection.Up] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpRight] *= (1 + keysTowardsCenterWeight);
                    positionBasedSwipePreferences[SwipeDirection.UpLeft] *= (1 + keysTowardsCenterWeight);
                }

                keySpecificSwipeDirections[y, x] =
                    positionBasedSwipePreferences.ToFrozenDictionary(x => x.Key, x => x.Value);
            }
        }

        return keySpecificSwipeDirections;
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
        double swipeDirectionPreference01 = _swipeDirectionPreferences[currentTypedKey.SwipeDirection];

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
                swipeDirectionPreference01: swipeDirectionPreference01
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
            swipeDirectionPreference01: swipeDirectionPreference01
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
            var x = (previousThumbPosition.X + Dimensions.X * 0.5f) *
                    0.5f; // closer to center - avg of center + prev position
            var y = Dimensions.Y; // below other keys
            return new Vector2(x, y);
        }
    }

    static Thumb GetWhichThumb(in InputAction previousTypedKey, int xPosition, in Vector2Int keyboardDimensions)
    {
        Debug.Assert(xPosition >= 0);

        int threshold = (int)Math.Ceiling(keyboardDimensions.X * 0.5f);
        Debug.Assert(threshold > 0 && threshold <= keyboardDimensions.X);

        if (xPosition == threshold)
        {
            // Middle position - use opposite thumb of the previous input
            return previousTypedKey.Thumb == Thumb.Left ? Thumb.Right : Thumb.Left;
        }
        else
        {
            // Use left thumb if xPosition is less than the threshold, otherwise use right thumb
            return xPosition < threshold ? Thumb.Left : Thumb.Right;
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