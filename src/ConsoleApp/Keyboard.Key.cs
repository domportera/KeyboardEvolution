using System.Diagnostics;

namespace ConsoleApp;

public partial class Keyboard
{
    class Key
    {
        readonly char[] _characters = new char[9];

        public Key(ReadOnlySpan<char> characters, Random random)
        {
            RandomlyDistributeCharacters(characters, random);
        }

        public void RandomlyDistributeCharacters(ReadOnlySpan<char> characters, Random random)
        {
            Debug.Assert(characters.Length <= _characters.Length);
            Clear();
            for (int i = 0; i < characters.Length; i++)
            {
                _characters[i] = characters[i];
            }

            random.Shuffle(_characters);

            Debug.Assert(_characters[(int)SwipeDirection.Center] !=
                         default); // todo : center should always have a letter
            throw new NotImplementedException();
        }

        public void SetCharacter(int index, char character)
        {
            _characters[index] = character;
        }

        public void SetCharacter(SwipeDirection index, char character)
        {
            _characters[(int)index] = character;
        }

        public void Clear()
        {
            for (int i = 0; i < _characters.Length; i++)
                _characters[i] = default;
        }

        internal bool Contains(char c, out SwipeDirection direction)
        {
            var index = _characters.AsSpan().IndexOf(c);
            direction = (SwipeDirection)index;
            return direction != SwipeDirection.None;
        }

        public char this[SwipeDirection direction] => _characters[(int)direction];
        public char this[Vector2Int position] => _characters[position.X + position.X * position.Y + position.Y];
        public char this[int position] => _characters[position];
    }
}