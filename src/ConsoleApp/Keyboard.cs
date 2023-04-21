using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;

namespace ConsoleApp;

public partial class Keyboard : IEvolvable<string>
{
    Key[,] _keys;

    static Key[,] KeyLayout3Row3Col => new Key[3, 3]
    {
        { new(), new(), new() },
        { new(), new(), new() },
        { new(), new(), new() },
    };

    static Key[,] KeyLayout3Row4Col => new Key[3, 4]
    {
        { new(), new(), new(), new() },
        { new(), new(), new(), new() },
        { new(), new(), new(), new() },
    };

    public enum KeyboardType
    {
        Standard,
        Long4x3
    }

    const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    // todo: all punctuation in alpnabet?
    readonly char[] _letters;
    static readonly ImmutableHashSet<char> LetterSet;
    static readonly ImmutableDictionary<SwipeDirection, double> Directions;
    readonly Random _random;

    static Keyboard()
    {
        LetterSet = Alphabet.ToImmutableHashSet();

        var dict = new Dictionary<SwipeDirection, double>()
        {
            { SwipeDirection.Up, Math.PI/2 },
            { SwipeDirection.UpRight, Math.PI/4 },
            { SwipeDirection.Right, 0 },
            { SwipeDirection.DownRight, 7*Math.PI/4 },
            { SwipeDirection.Down, 3*Math.PI/2 },
            { SwipeDirection.DownLeft, 5*Math.PI/4 },
            { SwipeDirection.Left, Math.PI },
            { SwipeDirection.UpLeft, 3*Math.PI/4 },
        };

        Directions = dict
            .ToImmutableDictionary(
                x => x.Key,
                x => x.Value);
    }

    public Keyboard(KeyboardType type, int seed)
    {
        _random = new Random(seed);
        _letters = Alphabet.ToCharArray();
        _random.Shuffle(_letters);
        _keys = type == KeyboardType.Standard ? KeyLayout3Row3Col : KeyLayout3Row4Col;
        DistributeRandomKeyLayout();

    }

    public Keyboard(Keyboard parent1, Keyboard parent2)
    {
    }

    void DistributeRandomKeyLayout()
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

    public double Fitness { get; private set; }


    enum Thumb {Left, Right}
    readonly struct InputAction
    {
        public readonly Key KeyPressed;
        public readonly Vector2Int KeyPosition;
        public readonly SwipeDirection SwipeDirection;
        public readonly Thumb Thumb;
        
        public InputAction(Key keyPressed, int row, int column, SwipeDirection swipeDirection, Thumb thumb)
        {
            KeyPressed = keyPressed;
            SwipeDirection = swipeDirection;
            Thumb = thumb;
            KeyPosition = new(column, row);
        }

        public static bool operator ==(InputAction one, InputAction two)
        {
            return !(one != two);
        }

        public static bool operator !=(InputAction one, InputAction two)
        {
            return one.KeyPressed != two.KeyPressed
                   || one.KeyPosition != two.KeyPosition
                   || one.SwipeDirection != two.SwipeDirection;
        }
    }

    Vector2Int SpaceBarPosition => new(_keys.GetLength(1) + 1, _keys.GetLength(0) / 2);
    public void AddStimulus(string text)
    {
        var input = text.AsSpan();

        InputAction previousTypedKey = default;
        foreach (char c in input)
        {
            if (c == ' ')
            {
                Fitness += CalculateTravelScoreSpaceBar(previousTypedKey);
                previousTypedKey = _spaceBarAction;
                continue;
            }
                
            if (!LetterSet.Contains(c))
                continue;

            InputAction charInput = default;
            for (var column = 0; column < _keys.GetLength(0); column++)
            for (var row = 0; row < _keys.GetLength(1); row++)
            {
                Key key = _keys[column, row];
                var contains = key.Contains(c, out var foundDirection);
                if (!contains) continue;
                
                charInput = new(key, row, column, foundDirection);
                break;
            }

            Debug.Assert(charInput != default);

            Fitness += CalculateTravelScore(charInput, previousTypedKey);
            previousTypedKey = charInput;
        }
    }

    // todo: alternating thumbs. determine which thumb previous vs current
    static double CalculateTravelScore(InputAction charInput, InputAction previousTypedKey)
    {
        var travel = charInput.KeyPosition - previousTypedKey.KeyPosition;
        var travelAngleRadians = Math.Atan2(travel.Y, travel.X) + Math.PI; // 0 - 2PI
        var distanceTraveled = Vector2.Distance(previousTypedKey.KeyPosition, charInput.KeyPosition);
        throw new NotImplementedException();
    }

    double CalculateTravelScoreSpaceBar(InputAction previousTypedKey)
    {
        var spaceBarPosition = SpaceBarPosition;
        spaceBarPosition.X = (spaceBarPosition.X + previousTypedKey.KeyPosition.X) / 2;
        
        var travel = spaceBarPosition - previousTypedKey.KeyPosition;
        var travelAngleRadians = Math.Atan2(travel.Y, travel.X) + Math.PI; // 0 - 2PI //imperfect bc space bar is wider...
        var distanceTraveled = Math.Abs(spaceBarPosition.Y - previousTypedKey.KeyPosition.Y);
        throw new NotImplementedException();
    }
    
    public void Mutate(float percentage)
    {
        throw new NotImplementedException();
    }

    public void ResetFitness()
    {
        Fitness = 0;
    }


    enum SwipeDirection
    {
        UpLeft = 0,
        Up = 1,
        UpRight = 2,
        Left = 3,
        Center = 4,
        Right = 5,
        DownLeft = 6,
        Down = 7,
        DownRight = 8,
        None = -1
    }
}