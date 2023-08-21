using System.Collections.Frozen;
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

    public Array2DCoords Dimensions { get; }

    public float Fitness { get; private set; }
    public Random Random => _random;

    readonly bool _separateStandardSpaceBar;
    readonly float _maxDistancePossible;
    readonly float _maxDistancePossibleStandardSpacebar;
    readonly float[,] _positionPreferences;

    readonly Weights _fitnessWeights;
    readonly float[] _spacebarSwipeDirectionPreferences;
    readonly float[,][] _keySpecificSwipeDirectionPreferences;
    readonly Random _random;
    readonly InputAction[] _previousInputs = new InputAction[2];

    InputAction _previousInputAction;

    readonly FrozenSet<char> _characterSet;
    readonly Dictionary<char, long> _characterFrequencies;

    // Coordinates are determined with (X = 0, Y = 0) being top-left
    public KeyboardLayout(
        Array2DCoords dimensions,
        FrozenSet<char> characterSet,
        int seed,
        bool separateStandardSpaceBar,
        float[,] positionPreferences,
        in Weights weights,
        float[,][] keySpecificSwipeDirectionPreferences,
        float[] swipeDirectionPreferences,
        Key[,]? startingLayout)
    {
        _characterSet = characterSet;
        _characterFrequencies = new(CharacterFrequencies.Frequencies);
        _fitnessWeights = weights;
        _spacebarSwipeDirectionPreferences = swipeDirectionPreferences;
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

        // assert that character set contains no duplicates
        Debug.Assert(allCharacters.Length == allCharacters.Distinct().Count());

        Traits = new Key[dimensions.RowY, dimensions.ColumnX];
        Dimensions = dimensions;
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
    }

    static readonly FrozenDictionary<SwipeDirection, float> SwipeAngles = new Dictionary<SwipeDirection, float>()
    {
        { SwipeDirection.Left, (float)Math.PI },
        { SwipeDirection.UpLeft, (float)(3 * Math.PI / 4) },
        { SwipeDirection.Up, (float)(Math.PI / 2) },
        { SwipeDirection.UpRight, (float)(Math.PI / 4) },
        { SwipeDirection.Right, 0 },
        { SwipeDirection.DownRight, (float)(-Math.PI / 4) },
        { SwipeDirection.Down, (float)(-Math.PI / 2) },
        { SwipeDirection.DownLeft, (float)(-3 * Math.PI / 4) },
    }.ToFrozenDictionary();


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

    TextRange? _currentStimulus;

    public void Evaluate(List<Range> ranges)
    {
        float fitness = 0;
        int charactersTestedCount = 0;
        var maxDistancePossibleInv = 1f / _maxDistancePossible;
        var maxDistancePossibleStandardSpacebarInv = 1f / _maxDistancePossibleStandardSpacebar;

        string text = _currentStimulus!.Text;
        foreach (var range in ranges)
        {
            ReadOnlySpan<char> input = text.AsSpan(range);
            float score = 0;
            foreach (char rawChar in input)
            {
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

    public void SetStimulus(TextRange rangeInfo) => _currentStimulus = rangeInfo;

    public void OverwriteTraits(Key[,] newKeys)
    {
        Debug.Assert(newKeys.GetLength(0) == Dimensions.RowY &&
                     newKeys.GetLength(1) == Dimensions.ColumnX);

        for (int y = 0; y < Dimensions.RowY; y++)
        for (int x = 0; x < Dimensions.ColumnX; x++)
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
        // shuffle % of key characters with each other
        Key[] allKeys = new Key[Dimensions.ColumnX * Dimensions.RowY];
        for (int y = 0; y < Dimensions.RowY; y++)
        for (int x = 0; x < Dimensions.ColumnX; x++)
        {
            var index2d = new Array2DCoords(columnX: x, rowY: y);
            allKeys[y * Dimensions.ColumnX + x] = Traits.Get(index2d);
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
            var key1 = allKeys[0];
            var key2 = allKeys[^1];
            for (int i = 0; i < quantityPerSwap; i++)
            {
                Key.SwapRandomCharacterFromEach(key1, key2, _random);
            }
        }

        // iterate through keys 
        for (int y = 0; y < Dimensions.RowY; y++)
        for (int x = 0; x < Dimensions.ColumnX; x++)
        {
            var index2d = new Array2DCoords(columnX: x, rowY: y);
            Traits
                .Get(index2d)
                .RedistributeKeysOptimally(_characterFrequencies, _keySpecificSwipeDirectionPreferences.Get(index2d));
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

        var swipeDirectionPreference01 = _keySpecificSwipeDirectionPreferences
            .Get(currentTypedKey.KeyPosition)[(int)currentTypedKey.SwipeDirection];

        if (sameKeyAndSwipe)
        {
            _travelScoreValues[Weights.DistanceIndex] = 1;
            _travelScoreValues[Weights.TrajectoryIndex] =
                currentTypedKey.SwipeDirection switch // repeated swipes on the same key are cumbersome
                {
                    SwipeDirection.Center => 1f,
                    SwipeDirection.Left
                        or SwipeDirection.Right
                        or SwipeDirection.Up
                        or SwipeDirection.Down => 0.35f,
                    _ => 0, // diagonals are the worst for this
                };
            _travelScoreValues[Weights.HandAlternationIndex] = 1;
            _travelScoreValues[Weights.HandCollisionAvoidanceIndex] = 1;
            _travelScoreValues[Weights.PositionalPreferenceIndex] = 1;
            _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] = swipeDirectionPreference01;
            return _fitnessWeights.CalculateScore(_travelScoreValues);
        }

        float distanceTraveled = Vector2.Distance(previousInputOfThumb.KeyPosition, currentTypedKey.KeyPosition);

        _travelScoreValues[Weights.DistanceIndex] = 1f - distanceTraveled * maxDistancePossibleInv;
        _travelScoreValues[Weights.TrajectoryIndex] = previousInputOfThumb.SwipeDirection == SwipeDirection.Center
            ? 1
            : 1 - AngleUtils.NormalizedAngleDifference(
                AngleUtils.AngleFromVector2(currentTypedKey.KeyPosition - previousInputOfThumb.KeyPosition),
                SwipeAngles[previousInputOfThumb.SwipeDirection]);
        _travelScoreValues[Weights.HandAlternationIndex] =
            previousInputOfThumb.Thumb != currentTypedKey.Thumb ? 1 : 0;
        _travelScoreValues[Weights.HandCollisionAvoidanceIndex] =
            previousInput.KeyPosition.ColumnX == currentTypedKey.KeyPosition.ColumnX ? 0 : 1;
            
        _travelScoreValues[Weights.PositionalPreferenceIndex] = _positionPreferences.Get(currentTypedKey.KeyPosition);
        _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] = swipeDirectionPreference01;

        return _fitnessWeights.CalculateScore(_travelScoreValues);
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

        float trajectoryCorrectness;
        SwipeDirection previousSwipeDirection = previousTypedKeyOfThumb.SwipeDirection;
        Debug.Assert(previousTypedKeyOfThumb.SwipeDirection != SwipeDirection.None);
        if (previousSwipeDirection == SwipeDirection.Center)
        {
            trajectoryCorrectness = 1;
        }
        else
        {
            Vector2 travel = spaceBarPosition - previousTypedKeyOfThumb.KeyPosition;
            var travelAngleRadians = AngleUtils.AngleFromVector2(travel); // 0 - 2PI 
            trajectoryCorrectness =
                1 - AngleUtils.NormalizedAngleDifference(travelAngleRadians, SwipeAngles[previousSwipeDirection]);
        }

        float distanceTraveled = spaceBarPosition.Y - previousTypedKeyOfThumb.KeyPosition.RowY;
        Debug.Assert(distanceTraveled >= 0);

        _travelScoreValues[Weights.DistanceIndex] = 1 - (distanceTraveled * maxDistancePossibleInverse);
        _travelScoreValues[Weights.TrajectoryIndex] = trajectoryCorrectness;
        _travelScoreValues[Weights.HandAlternationIndex] = 1; // spacebar can always use opposite hand
        _travelScoreValues[Weights.HandCollisionAvoidanceIndex] =
            1; // spacebar is wide enough to never worry about overlap
        _travelScoreValues[Weights.PositionalPreferenceIndex] =
            0.5f; // spacebar position is relatively standardized, todo: allow non-standard space position? 3x4 layout?
        _travelScoreValues[Weights.SwipeDirectionPreferenceIndex] =
            _spacebarSwipeDirectionPreferences[(int)spaceKeyAction.SwipeDirection];

        return _fitnessWeights.CalculateScore(_travelScoreValues);

        Vector2 GetSpaceBarPressPosition(in Array2DCoords previousThumbPosition)
        {
            // closer to center - avg of center + prev position
            var x = (previousThumbPosition.ColumnX + Dimensions.ColumnX * 0.5f) * 0.5f;
            var y = Dimensions.RowY; // below other keys
            return new Vector2(x, y);
        }
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