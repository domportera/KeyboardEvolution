using Core.Util;

namespace ThumbKey;

[Serializable]
public class TrainerSettings
{
    public int ParentCount = 20;
    public int ChildrenPerParent = 200;
    public int GenerationCount = 100_000;
    public int EntriesPerGeneration = 2000;
    public int Seed = -1;

    public PresetType PresetType = PresetType.ThumbKeyEngV4NoSymbols;

    public string CharacterSetString = "abcdefghijklmnopqrstuvwxyz'";
    public bool UseStandardSpaceBar = true;


    /// <summary>
    /// The percentage of keys that will be mutated in a given key. I.e., if this is 0.3, 30% of the keys will be
    /// changed in the mutation process.
    /// </summary>
    public float MutationFactor = 0.6f;

    /// <summary>
    /// Allows for a random MutationFactor to be used instead of a constant one, with a range of [0, MutationFactor]
    /// </summary>
    public bool UseRandomMutation = true;

    /// <summary>
    /// If <see cref="UseRandomMutation"/> is true, MutationFactor * Math.Pow(random01, MutationExponent)
    /// to modify the distribution of mutation factors over a population
    /// </summary>
    public float MutationExponent = 2f;


    /// <summary>
    /// If true, uses the key-specific swipe direction preferences for calculations related to swipeDirectionPreference in Weights
    /// This is likely unnecessary due to the Trajectory calculation, but it's here if you want to use it
    /// </summary>
    public bool UseKeySpecificSwipeDirectionPreferences = false;

    /// <summary>
    /// Governs whether characters within a key are reorganized/optimized after mutation.
    /// Use if you'd like to prioritize swipe direction preference over trajectory and adjust your fitness weights accordingly.
    /// </summary>
    public bool RedistributeKeyCharactersBasedOnFrequency = false;

    /// <summary>
    /// Normally, diagonal keys can only swap with diagonal keys, etc. This is useful for larger layouts, but for
    /// layouts that are 3x3 and smaller, it is recommended to set this to true.
    /// </summary>
    public bool AllowCardinalDiagonalSwaps = true;
    
    /// <summary>
    /// Shows preference for preferring swipes that are in the opposite direction of the next key.
    /// This could be useful to give fingers a bit more room to "breathe" or turn around on layouts with
    /// smaller keys.
    /// </summary>
    public bool PreferSwipesInOppositeDirectionOfNextKey = false;

    /// <summary>
    /// The main weights used in the fitness function
    /// </summary>
    public SerializableWeights FitnessWeights = new(
        Distance: 0.0f, // prefer smaller distance between keypresses made by the same thumb
        Trajectory: 0.5f, // prefer swiping in the same direction as that thumb's next key
        HandAlternation: 1.5f, // prefer alternating between hands
        HandCollisionAvoidance: 0.2f, // for layouts with a center column, there is a penalty for alternating hands on the same key
        PositionalPreference: 0.05f, // weight of the hard-coded positional preference dictionary below
        SwipeDirectionPreference: 0.1f //weight of the hard-coded swipe types defined below (cardinal, diagonal, center)
    );

    public float CardinalPreference = 0.4f;
    public float DiagonalPreference = 0f;
    public float CenterPreference = 1f;

    /// <summary>
    /// The character substitutions to be made before training begins
    /// This exists to correct for the fact that the Reddit data set uses curly apostrophes
    /// </summary>
    public CharacterReplacement[] CharacterSubsitutions =
    {
        new('\u2019', '\''),
    };

    /// <summary>
    /// The preference of swiping towards the center of the keyboard, used in generating positional preferences and calculating
    /// fitness relating to swipeDirectionPreference - rather than simply using the simple swipe direction, the fitness and mutation
    /// functions will prefer swiping towards the center of the keyboard
    /// </summary>
    public float KeysTowardsCenterWeight = 0.1f; //prefer swiping towards the center of the keyboard

    /// <summary>
    /// A serializable representation of hard-coded positional preferences for each key on the keyboard, as in
    /// where are your thumbs most comfortable pressing?
    /// Each keyboard layout is represented by a 2D array of floats, where each float represents the preference of that key
    /// on a scale of 0 to 1. This is a jagged array for the sake of serialization.
    /// </summary>
    public PositionPreferencesSerialized[] PositionPreferencesSerialized =
    {
        new()
        {
            Dimensions = new(columnX: 3, rowY: 3),
            PositionPreferences = new float[][]
            {
                new[] { 0.4f, 0.0f, 0.4f },
                new[] { 1f, 0.7f, 1f },
                new[] { 1f, 1f, 1f }
            }
        },
        new()
        {
            Dimensions = new(columnX: 4, rowY: 3),
            PositionPreferences = new[]
            {
                new[] { 0.2f, 0.0f, 0.0f, 0.2f },
                new[] { 1, 0.8f, 0.8f, 1 },
                new[] { 1f, 1, 1, 1 },
            }
        },
        new()
        {
            Dimensions = new(columnX: 4, rowY: 4),
            PositionPreferences = new[]
            {
                new[] { 0.4f, 0.0f, 0.0f, 0.4f },
                new[] { 0.9f, 0.7f, 0.7f, 0.9f },
                new[] { 1f, 1, 1, 1 },
                new[] { 1f, 1, 1, 1 },
            }
        }
    };

    public string[] IgnoredPhrases = new[]
    {
        "it has been removed for the following",
        "/rules",
        "^Please ^refer ^to ^our",
        "Thank you for your submission",
        "This submission is a banned",
        "^If",
        "^etiquette",
        "^guidelines",
        "Reddit",
        "upvote",
        "^^^",
        "bot",
        "automated"
    };

    public string JsonTag = "text";
    public string JsonPath = "./reddit_casual.json";
    public int MinCommentLength = 10;

    public int TotalCount => ParentCount + ParentCount * ChildrenPerParent;

    /// <summary>
    /// The ratio of the population that will be reproduced. I.e., if this is 0.1, the top 10% of the population will
    /// be copied and mutated to fill the rest of the population.
    /// </summary>
    public float ReproductionRatio => ParentCount / (float)TotalCount;

    [NonSerialized] Dictionary<Array2DCoords, float[,]>? _positionPreferences;

    public Dictionary<Array2DCoords, float[,]> GetPositionPreferences()
    {
        if (_positionPreferences != null)
            return _positionPreferences;

        _positionPreferences = new Dictionary<Array2DCoords, float[,]>();
        foreach (var prefs in PositionPreferencesSerialized)
        {
            float[][] preferencesJagged = prefs.PositionPreferences;
            var array2D = new float[preferencesJagged.Length, preferencesJagged[0].Length];
            for (int y = 0; y < preferencesJagged.Length; y++)
            {
                for (int x = 0; x < preferencesJagged[y].Length; x++)
                {
                    array2D[y, x] = preferencesJagged[y][x];
                }
            }

            _positionPreferences.Add(prefs.Dimensions, array2D);
        }

        return _positionPreferences;
    }
}

[Serializable]
public record PositionPreferencesSerialized
{
    public Array2DCoords Dimensions;
    public float[][] PositionPreferences;
}