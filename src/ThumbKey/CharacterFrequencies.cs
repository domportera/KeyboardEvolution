namespace ThumbKey;

public static class CharacterFrequencies
{
    const long PrecalculatedFrequencyStrength = 100000;

    // initialize CharacterFrequencies dictionary with english-language character frequencies
    // https://en.wikipedia.org/wiki/Letter_frequency
    public static IReadOnlyDictionary<char, long> Frequencies => _frequencies;

    static readonly Dictionary<char, long> _frequencies = new()
    {
        { 'a', (long)(8.2 * PrecalculatedFrequencyStrength) },
        { 'b', (long)(1.5 * PrecalculatedFrequencyStrength) },
        { 'c', (long)(2.8 * PrecalculatedFrequencyStrength) },
        { 'd', (long)(4.3 * PrecalculatedFrequencyStrength) },
        { 'e', (long)(13.0 * PrecalculatedFrequencyStrength) },
        { 'f', (long)(2.2 * PrecalculatedFrequencyStrength) },
        { 'g', (long)(2.0 * PrecalculatedFrequencyStrength) },
        { 'h', (long)(6.1 * PrecalculatedFrequencyStrength) },
        { 'i', (long)(7.0 * PrecalculatedFrequencyStrength) },
        { 'j', (long)(0.15 * PrecalculatedFrequencyStrength) },
        { 'k', (long)(0.77 * PrecalculatedFrequencyStrength) },
        { 'l', (long)(4.0 * PrecalculatedFrequencyStrength) },
        { 'm', (long)(2.4 * PrecalculatedFrequencyStrength) },
        { 'n', (long)(6.7 * PrecalculatedFrequencyStrength) },
        { 'o', (long)(7.5 * PrecalculatedFrequencyStrength) },
        { 'p', (long)(1.9 * PrecalculatedFrequencyStrength) },
        { 'q', (long)(0.095 * PrecalculatedFrequencyStrength) },
        { 'r', (long)(6.0 * PrecalculatedFrequencyStrength) },
        { 's', (long)(6.3 * PrecalculatedFrequencyStrength) },
        { 't', (long)(9.1 * PrecalculatedFrequencyStrength) },
        { 'u', (long)(2.8 * PrecalculatedFrequencyStrength) },
        { 'v', (long)(0.98 * PrecalculatedFrequencyStrength) },
        { 'w', (long)(2.4 * PrecalculatedFrequencyStrength) },
        { 'x', (long)(0.15 * PrecalculatedFrequencyStrength) },
        { 'y', (long)(2.0 * PrecalculatedFrequencyStrength) },
        { 'z', (long)(0.074 * PrecalculatedFrequencyStrength) },
    };

    public static void AddCharacterIfNotIncluded(char character)
    {
        if (!Frequencies.ContainsKey(character))
        {
            _frequencies.Add(character, 0);
        }
    }
}