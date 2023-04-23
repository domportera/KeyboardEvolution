using System.Collections.Immutable;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public class KeyboardLayoutTrainer : IEvolver<string, KeyboardLayout>
{
    KeyboardLayout _layout; 

    const string CharacterSet = "abcdefghijklmnopqrstuvwxyz,.;*-_!?@$%&():'\"";

    // todo: all punctuation in alphabet?
    public static readonly ImmutableHashSet<char> CharacterSetDict;
    readonly Random _random;

    // Coordinates are determined with (X = 0, Y = 0) being top-left
    static KeyboardLayoutTrainer()
    {
        CharacterSetDict = CharacterSet.ToImmutableHashSet();
    }

    public KeyboardLayoutTrainer(int width, int height, int seed)
    {
        _random = new Random(seed);
      //  _layout = new KeyboardLayout(width, height, CharacterSet, seed);
    }

    public static KeyboardLayoutTrainer[] Reproduce (KeyboardLayoutTrainer parent1, KeyboardLayoutTrainer parent2, int quantity)
    {
        throw new NotImplementedException();
    }


    // todo: alternating thumbs. determine which thumb previous vs current
    public void Mutate(float percentage)
    {
        throw new NotImplementedException();
    }
    
}