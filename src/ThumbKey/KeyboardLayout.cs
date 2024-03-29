using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Core;
using Core.Util;

namespace ThumbKey;

public class KeyboardLayout : IEvolvable<char[], Key[,]>
{
    public Key[,] Traits { get; }
    Array2DCoords _dimensions;
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

    public double Fitness { get; private set; }
    public Random Random => _random;

    readonly bool _separateStandardSpaceBar;
    readonly float _maxDistancePossible;
    readonly float _maxDistancePossibleStandardSpacebar;
    readonly float[,] _positionPreferences;

    readonly Weights _fitnessWeights;
    readonly float[] _swipeDirectionPreferences;
    readonly float[,][] _keySpecificSwipeDirectionPreferences;
    readonly Random _random;
    readonly InputAction[] _previousInputs = new InputAction[2];

    InputAction _previousInputAction;

    readonly char[] _characterSet;
    readonly Dictionary<char, long> _characterFrequencies;
    static int _seedIterator = 0;

    // Coordinates are determined with (X = 0, Y = 0) being top-left
    public KeyboardLayout(Array2DCoords dimensions,
        FrozenSet<char> characterSet,
        int seed,
        bool separateStandardSpaceBar,
        float[,] positionPreferences,
        in Weights weights,
        float[,][] keySpecificSwipeDirectionPreferences,
        float[] swipeDirectionPreferences,
        Key[,]? startingLayout)
    {
        _characterSet = characterSet.ToArray();
        _characterFrequencies = new(CharacterFrequencies.Frequencies);
        _fitnessWeights = weights;
        _swipeDirectionPreferences = swipeDirectionPreferences;
        _positionPreferences = positionPreferences;
        var iterator = Interlocked.Increment(ref _seedIterator);
        var random = new Random(seed + iterator);
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
            _maxDistancePossibleStandardSpacebar = Vector2.Distance(Vector2.One, dimensions + (0, 1));
        }

        // assert that character set contains no duplicates
        Debug.Assert(allCharacters.Length == allCharacters.Distinct().Count());

        Traits = new Key[dimensions.RowY, dimensions.ColumnX];
        _keySpecificSwipeDirectionPreferences = keySpecificSwipeDirectionPreferences;


        _previousInputs[(int)Thumb.Left] = new(0, dimensions.RowY / 2, SwipeDirection.Center, Thumb.Left);
        _previousInputs[(int)Thumb.Right] =
            new(dimensions.ColumnX - 1, dimensions.RowY / 2, SwipeDirection.Center, Thumb.Right);
        _previousInputAction =
            _previousInputs[(int)Thumb.Right]; // default key - assumes user opens text field with right thumb

        _maxDistancePossible = Vector2.Distance(Vector2.One, dimensions);

        if (startingLayout is not null)
        {
            for (int y = 0; y < dimensions.RowY; y++)
            for (int x = 0; x < dimensions.ColumnX; x++)
            {
                var index2d = new Array2DCoords(x, y);
                Traits.Set(index2d, startingLayout.Get(index2d));
            }
        }
        else
        {
            DistributeRandomKeyboardLayout(Traits, allCharacters, random, _characterFrequencies);
        }

        _charPositionDict = GenerateCharacterPositionDictionary(Traits);
        _dimensions = Traits.GetDimensions();
    }

    // deprecated for use of the array for more efficient access
    static readonly IReadOnlyDictionary<SwipeDirection, float> SwipeAngles = new Dictionary<SwipeDirection, float>()
    {
        { SwipeDirection.Left, (float)Math.PI },
        { SwipeDirection.UpLeft, (float)(3 * Math.PI / 4) },
        { SwipeDirection.Up, (float)(Math.PI / 2) },
        { SwipeDirection.UpRight, (float)(Math.PI / 4) },
        { SwipeDirection.Right, 0 },
        { SwipeDirection.DownRight, (float)(-Math.PI / 4) },
        { SwipeDirection.Down, (float)(-Math.PI / 2) },
        { SwipeDirection.DownLeft, (float)(-3 * Math.PI / 4) },
        { SwipeDirection.Center, float.NaN }
    }.ToFrozenDictionary();

    // compliant with the values of SwipeDirection enum
    static readonly float[] SwipeAnglesArray;

    static KeyboardLayout()
    {
        SwipeAnglesArray = new float[SwipeAngles.Count];
        foreach (var (swipeDirection, angle) in SwipeAngles)
        {
            SwipeAnglesArray[(int)swipeDirection] = angle;
        }
    }



    Key GetKey(int x, int y)
    {
        var index2d = new Array2DCoords(x, y);
        return Traits.Get(index2d);
    }

    public Key GetKey(Array2DCoords position) => GetKey(position.ColumnX, position.RowY);
    public Key this[Array2DCoords index] => GetKey(index.ColumnX, index.RowY);

    static void DistributeRandomKeyboardLayout(Key[,] keys, char[] characterSet, Random random,
        Dictionary<char, long> characterFrequencies)
    {
        random.Shuffle(characterSet);
        Array.Sort(characterSet, (a, b) =>
        {
            characterFrequencies.TryGetValue(a, out long aFrequency);
            characterFrequencies.TryGetValue(b, out long bFrequency);

            return bFrequency.CompareTo(aFrequency);
        });

        int cardinalPerKey = 4;
        int diagonalPerKey = 4;

        int centerCharCount = keys.Length;
        int cardinalCharCount = Math.Clamp(cardinalPerKey * keys.Length, 0, characterSet.Length - centerCharCount);
        int diagonalCharCount = Math.Clamp(diagonalPerKey * keys.Length, 0,
            characterSet.Length - centerCharCount - cardinalCharCount);

        Span<char> centerCharacters = characterSet.AsSpan(0, centerCharCount);
        Span<char> cardinalCharacters = characterSet.AsSpan(centerCharCount, cardinalCharCount);
        Span<char> diagonalCharacters = characterSet.AsSpan(centerCharCount + cardinalCharCount, diagonalCharCount);

        cardinalPerKey = cardinalCharacters.Length / keys.Length;
        diagonalPerKey = diagonalCharacters.Length / keys.Length;
        int remainingCardinal = cardinalCharacters.Length % keys.Length;
        int remainingDiagonal = diagonalCharacters.Length % keys.Length;

        int cardinalInterval = (remainingCardinal > 0)
            ? Math.Max(1, keys.Length / remainingCardinal)
            : 0;

        int diagonalInterval = (remainingDiagonal > 0)
            ? Math.Max(1, keys.Length / remainingDiagonal)
            : 0;

        int centerIndex = 0;
        int nextCardinalPos = 0;
        int nextDiagonalPos = 0;
        int keyIndex = 0;

        Array2DCoords layoutCoords = new Array2DCoords(columnX: keys.GetLength(1), rowY: keys.GetLength(0));
        for (int y = 0; y < layoutCoords.RowY; y++)
        {
            for (int x = 0; x < layoutCoords.ColumnX; x++, keyIndex++)
            {
                char centerChar = centerCharacters[centerIndex++];
                var cardinalChars = cardinalCharacters.Slice(0, cardinalPerKey);
                var diagonalChars = diagonalCharacters.Slice(0, diagonalPerKey);

                if (remainingCardinal > 0
                    && keyIndex == nextCardinalPos
                    && cardinalCharacters.Length > cardinalPerKey)
                {
                    cardinalChars = cardinalCharacters.Slice(0, cardinalPerKey + 1);
                    cardinalCharacters = cardinalCharacters[(cardinalPerKey + 1)..];
                    remainingCardinal--;
                    nextCardinalPos += cardinalInterval;
                }
                else
                {
                    cardinalCharacters = cardinalCharacters[cardinalPerKey..];
                }

                if (remainingDiagonal > 0
                    && keyIndex == nextDiagonalPos
                    && diagonalCharacters.Length > diagonalPerKey)
                {
                    diagonalChars = diagonalCharacters.Slice(0, diagonalPerKey + 1);
                    diagonalCharacters = diagonalCharacters[(diagonalPerKey + 1)..];
                    remainingDiagonal--;
                    nextDiagonalPos += diagonalInterval;
                }
                else
                {
                    diagonalCharacters = diagonalCharacters[diagonalPerKey..];
                }

                var index2d = new Array2DCoords(columnX: x, rowY: y);
                var key = new Key(centerChar, cardinalChars, diagonalChars, new Random(random.Next()));
                keys.Set(index2d, key);
            }
        }

        if (remainingCardinal > 0 || remainingDiagonal > 0)
            throw new Exception("Invalid character distribution - missing characters");
    }

    static FrozenDictionary<char, InputPositionInfo> GenerateCharacterPositionDictionary(Key[,] keys)
    {
        var dict = new Dictionary<char, InputPositionInfo>();
        for (int y = 0; y < keys.GetLength(0); y++)
        for (int x = 0; x < keys.GetLength(1); x++)
        {
            var index2d = new Array2DCoords(columnX: x, rowY: y);
            var key = keys.Get(index2d);
            for (int i = 0; i < Key.MaxCharacterCount; i++)
            {
                char c = key[i];
                if (c == default)
                    continue;

                dict.Add(c, new InputPositionInfo(x, y, (SwipeDirection)i, key));
            }
        }

        return dict.ToFrozenDictionary();
    }

    char[] _currentStimulus;

    public void Evaluate(List<Range> ranges)
    {
        double fitness = 0;
        int charactersTestedCount = 0;
        var maxDistancePossibleInv = 1f / _maxDistancePossible;
        var maxDistancePossibleStandardSpacebarInv = 1f / _maxDistancePossibleStandardSpacebar;

        var text = _currentStimulus;
        foreach (var range in ranges)
        {
            ReadOnlySpan<char> input = text.AsSpan(range);
            double score = 0;
            for (int i = 0; i < input.Length; i++)
            {
                var rawChar = input[i];

                if (_separateStandardSpaceBar && rawChar == ' ')
                {
                    // thumbs to spacebar can always alternate
                    int thisThumb = (int)(_previousInputAction.Thumb == Thumb.Left ? Thumb.Right : Thumb.Left);

                    score += CalculateTravelScoreStandardSpaceBar(
                        previousTypedKeyOfThumb: in _previousInputs[thisThumb],
                        out InputAction spaceKeyAction,
                        maxDistancePossibleStandardSpacebarInv);

                    ++charactersTestedCount;

                    _previousInputAction = spaceKeyAction;
                    continue;
                }

                // todo : implement different shift schemes: shift once then auto-lower, caps lock, one shift swipe, shift swipe on each side, etc
                // bool isUpperCase = char.IsUpper(rawChar);
                char c = char.ToLowerInvariant(rawChar); // lowercase only - ignore case

                var gotInput = _charPositionDict.TryGetValue(c, out var inputPositionInfo);

                if (!gotInput)
                    continue;

                _characterFrequencies[c]++;

                var thumb = GetWhichThumb(in _previousInputAction, inputPositionInfo.Column, _dimensions);
                InputAction currentInput = new(inputPositionInfo.Column, inputPositionInfo.Row,
                    inputPositionInfo.SwipeDirection, thumb);

                int fingerIndex = (int)currentInput.Thumb;
                var previousTypedByThisThumb = _previousInputs[fingerIndex];
                // todo: handle first key press, where there is no previous typed key
                score += CalculateTravelScore(
                    currentTypedKey: in currentInput,
                    previousInputOfThumb: in previousTypedByThisThumb,
                    previousInput: in _previousInputAction,
                    maxDistancePossibleInv);
                _previousInputAction = currentInput;
                _previousInputs[fingerIndex] = currentInput;
                ++charactersTestedCount;
            }

            fitness += score;
        }

        Fitness = fitness / charactersTestedCount;
    }

    public void SetStimulus(char[] rangeInfo) => _currentStimulus = rangeInfo;

    public void OverwriteTraits(Key[,] newKeys)
    {
        var dimensions = Traits.GetDimensions();
        for (int y = 0; y < dimensions.RowY; y++)
        for (int x = 0; x < dimensions.ColumnX; x++)
        {
            var index2d = new Array2DCoords(columnX: x, rowY: y);
            var myKey = Traits.Get(index2d);
            var otherKey = newKeys.Get(index2d);
            myKey.OverwriteKeysWith(otherKey);
        }
    }

    public void ResetFitness() => Fitness = 0;


    public void Mutate(double percentageOfCharactersToMutate)
    {
        var dimensions = _dimensions;
        // shuffle % of key characters with each other
        Key[] allKeys = new Key[dimensions.ColumnX * dimensions.RowY];
        for (int y = 0; y < dimensions.RowY; y++)
        for (int x = 0; x < dimensions.ColumnX; x++)
        {
            var index2d = new Array2DCoords(columnX: x, rowY: y);
            allKeys[y * dimensions.ColumnX + x] = Traits.Get(index2d);
        }

        _random.Shuffle(allKeys);

        int characterCount = allKeys.Length * Key.MaxCharacterCount;
        double quantityToShuffle = characterCount * percentageOfCharactersToMutate;
        int shuffleAmount = Math.Max(1, (int)Math.Round(quantityToShuffle / 2d));

        for (int i = 0; i < shuffleAmount; i++)
        {
            int randomIndex1 = _random.Next(0, allKeys.Length);
            int randomIndex2 = randomIndex1;
            while (randomIndex1 == randomIndex2)
            {
                randomIndex2 = _random.Next(0, allKeys.Length);
            }

            var key1 = allKeys[randomIndex1];
            var key2 = allKeys[randomIndex2];
            Key.SwapRandomCharacterFromEach(key1, key2, _random);
        }

        if (KeyboardLayoutTrainer.RedistributeKeyCharactersBasedOnFrequency)
        {
            for (int y = 0; y < dimensions.RowY; y++)
            for (int x = 0; x < dimensions.ColumnX; x++)
            {
                var index2d = new Array2DCoords(columnX: x, rowY: y);
                Traits
                    .Get(index2d)
                    .RedistributeKeysOptimally(_characterFrequencies,
                        _keySpecificSwipeDirectionPreferences.Get(index2d));
            }
        }

        _charPositionDict = GenerateCharacterPositionDictionary(Traits);
    }

    public void Kill()
    {
    }

    readonly float[] _travelScoreValues = new float[Vector<float>.Count];

    float CalculateTravelScore(in InputAction currentTypedKey, in InputAction previousInputOfThumb,
        in InputAction previousInput, float maxDistancePossibleInv)
    {
        bool sameKeyAndSwipe = previousInput.KeyPosition == currentTypedKey.KeyPosition
                               && previousInput.SwipeDirection == currentTypedKey.SwipeDirection;

        float[] swipeDirectionPreferences = KeyboardLayoutTrainer.UseKeySpecificSwipeDirectionPreferences
            ? _keySpecificSwipeDirectionPreferences.Get(currentTypedKey.KeyPosition)
            : _swipeDirectionPreferences;
        var swipeDirectionPreference01 = swipeDirectionPreferences[(int)currentTypedKey.SwipeDirection];

        if (sameKeyAndSwipe)
        {
            _travelScoreValues[Weights.DistanceIndex] = 1;
            _travelScoreValues[Weights.TrajectoryIndex] =
                currentTypedKey.SwipeDirection switch // repeated swipes on the same key are cumbersome
                {
                    SwipeDirection.Center => KeyboardLayoutTrainer.CenterPreference,
                    SwipeDirection.Left
                        or SwipeDirection.Right
                        or SwipeDirection.Up
                        or SwipeDirection.Down => KeyboardLayoutTrainer.CardinalPreference,
                    _ => KeyboardLayoutTrainer.DiagonalPreference
                };
            _travelScoreValues[Weights.HandAlternationIndex] = 1;
            _travelScoreValues[Weights.HandCollisionAvoidanceIndex] = 1;
            _travelScoreValues[Weights.PositionalPreferenceIndex] = 1;
            _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] = swipeDirectionPreference01;
            return _fitnessWeights.CalculateScore(_travelScoreValues);
        }

        var previousKeyPosition = previousInputOfThumb.KeyPosition;
        var currentKeyPosition = currentTypedKey.KeyPosition;
        var a = previousKeyPosition.ColumnX - currentKeyPosition.ColumnX;
        var b = previousKeyPosition.RowY - currentKeyPosition.RowY;
        float distanceTraveled = MathF.Sqrt((float)(a * a + b * b));


        _travelScoreValues[Weights.DistanceIndex] = 1f - distanceTraveled * maxDistancePossibleInv;

        if (previousInputOfThumb.SwipeDirection == SwipeDirection.Center)
        {
            _travelScoreValues[Weights.TrajectoryIndex] = 1;
        }
        else
        {
            float angleDifference =
                DifferenceBetweenSwipeAndTravelDirections(previousInputOfThumb, currentKeyPosition);
            if (KeyboardLayoutTrainer.PreferSwipesInOppositeDirectionOfNextKey)
                _travelScoreValues[Weights.TrajectoryIndex] = angleDifference;
            else
                _travelScoreValues[Weights.TrajectoryIndex] = 1 - angleDifference;
        }

        _travelScoreValues[Weights.HandAlternationIndex] =
            previousInputOfThumb.Thumb != currentTypedKey.Thumb ? 1 : 0;
        _travelScoreValues[Weights.HandCollisionAvoidanceIndex] =
            previousInput.KeyPosition.ColumnX == currentTypedKey.KeyPosition.ColumnX ? 0 : 1;

        _travelScoreValues[Weights.PositionalPreferenceIndex] = _positionPreferences.Get(currentTypedKey.KeyPosition);
        _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] = swipeDirectionPreference01;

        return _fitnessWeights.CalculateScore(_travelScoreValues);
    }

    static float DifferenceBetweenSwipeAndTravelDirections(in InputAction previousInputOfThumb,
        in Array2DCoords currentKeyPosition)
    {
        var deltaPosition = GetPositionDeltaForAngleCalculation(previousInputOfThumb, currentKeyPosition);
        return DifferenceBetweenSwipeAndTravelDirections(previousInputOfThumb, deltaPosition);
    }

    static float DifferenceBetweenSwipeAndTravelDirections(in InputAction previousInputOfThumb,
        in Vector2 deltaPosition)
    {
        var travelAngleRadians = AngleUtils.AngleFromVector2(deltaPosition);
        var previousSwipeAngleRadians = SwipeAnglesArray[(int)previousInputOfThumb.SwipeDirection];
        return AngleUtils.NormalizedAngleDifference(travelAngleRadians, previousSwipeAngleRadians);
    }


    float CalculateTravelScoreStandardSpaceBar(in InputAction previousTypedKeyOfThumb, out InputAction spaceKeyAction,
        float maxDistancePossibleInverse)
    {
        Vector2 spaceBarPosition = GetSpaceBarPressPosition(in previousTypedKeyOfThumb.KeyPosition);

        spaceKeyAction = new InputAction(
            column: (int)Math.Round(spaceBarPosition.X),
            row: (int)Math.Round(spaceBarPosition.Y),
            swipeDirection: SwipeDirection.Center,
            thumb: previousTypedKeyOfThumb.Thumb);

        SwipeDirection previousSwipeDirection = previousTypedKeyOfThumb.SwipeDirection;
        var deltaPosition = GetPositionDeltaForAngleCalculation(previousTypedKeyOfThumb, spaceBarPosition);
        if (previousSwipeDirection == SwipeDirection.Center)
        {
            _travelScoreValues[Weights.TrajectoryIndex] = 1;
        }
        else
        {
            float angleDifference = DifferenceBetweenSwipeAndTravelDirections(previousTypedKeyOfThumb, deltaPosition);
            if (KeyboardLayoutTrainer.PreferSwipesInOppositeDirectionOfNextKey)
                _travelScoreValues[Weights.TrajectoryIndex] = angleDifference;
            else
                _travelScoreValues[Weights.TrajectoryIndex] = 1 - angleDifference;
        }

        float distanceTraveled = Math.Abs(deltaPosition.Y);
        Debug.Assert(distanceTraveled >= 0);

        _travelScoreValues[Weights.DistanceIndex] = 1 - (distanceTraveled * maxDistancePossibleInverse);

        // spacebar can always use opposite hand
        _travelScoreValues[Weights.HandAlternationIndex] = 1;

        ; // spacebar is wide enough to never worry about overlap
        _travelScoreValues[Weights.HandCollisionAvoidanceIndex] = 1;

        // spacebar position is relatively standardized, todo: allow non-standard space position? 3x4 layout?
        _travelScoreValues[Weights.PositionalPreferenceIndex] = 1f;

        _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] =
            _swipeDirectionPreferences[(int)spaceKeyAction.SwipeDirection];

        return _fitnessWeights.CalculateScore(_travelScoreValues);

        Vector2 GetSpaceBarPressPosition(in Array2DCoords previousThumbPosition)
        {
            // closer to center - avg of center + prev position
            var x = (previousThumbPosition.ColumnX + _dimensions.ColumnX * 0.5f) * 0.5f;
            var y = _dimensions.RowY; // below other keys
            return new Vector2(x, y);
        }
    }

    static Vector2 GetPositionDeltaForAngleCalculation(InputAction previousTypedKeyOfThumb, Vector2 currentKeyPosition)
    {
        var delta = currentKeyPosition - previousTypedKeyOfThumb.KeyPosition;
        delta.Y *= -1; //compensate for y-axis being inverted in our coordinate system vs the standard for angles
        return delta;
    }

    static Thumb GetWhichThumb(in InputAction previousTypedKey, int xPosition, in Array2DCoords keyboardDimensions)
    {
        float threshold = (keyboardDimensions.ColumnX - 1) * 0.5f; // -1 to account for 0-indexing

        if (Math.Abs(xPosition - threshold) < .01f) // floating point equality
        {
            // Middle position - use opposite thumb of the previous input
            return previousTypedKey.Thumb == Thumb.Left ? Thumb.Right : Thumb.Left;
        }

        // Use left thumb if xPosition is less than the threshold, otherwise use right thumb
        return xPosition < threshold ? Thumb.Left : Thumb.Right;
    }
}

public static class ExtensionMethods
{
    public static Array2DCoords GetDimensions(this Key[,] keyLayout) =>
        new(keyLayout.GetLength(1), keyLayout.GetLength(0));
}