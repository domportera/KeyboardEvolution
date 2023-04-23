using System.Collections.Immutable;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public class ThumbKeyboard : IEvolver<string, KeyboardLayout>
{
    KeyboardLayout _layout; 

    const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    // todo: all punctuation in alpnabet?
    public static readonly ImmutableHashSet<char> LetterSet;
    static readonly ImmutableDictionary<SwipeDirection, double> Directions;
    readonly Random _random;

    // Coordinates are determined with (X = 0, Y = 0) being top-left
    static ThumbKeyboard()
    {
        LetterSet = Alphabet.ToImmutableHashSet();
    }

    public ThumbKeyboard(int width, int height, int seed)
    {
    }

    public static ThumbKeyboard[] Reproduce (ThumbKeyboard parent1, ThumbKeyboard parent2, int quantity)
    {
        throw new NotImplementedException();
    }


    // todo: alternating thumbs. determine which thumb previous vs current
    public void Mutate(float percentage)
    {
        throw new NotImplementedException();
    }
}