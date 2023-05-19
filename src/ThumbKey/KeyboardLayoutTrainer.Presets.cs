namespace ThumbKey;

public enum PresetType
{
    ThumbKeyEngV4
}

public class LayoutPresets
{
    public Key[,] this[PresetType index] => Presets[index];

    public static LayoutPresets Instance => _layoutPresets ??= new();

    static readonly IReadOnlyDictionary<PresetType, Key[,]> Presets = new Dictionary<PresetType, Key[,]>()
    {
        {
            PresetType.ThumbKeyEngV4,
            new Key[,]
            {
                {
                    new Key("\0\0\0\0s\0\0\0w"),
                    new Key("\0\0\0\0r\0\0g\0"),
                    new Key("\0\0\0\0o\0u\0\0")
                },
                {
                    new Key("\0\0\0\0nm\0\0\0"),
                    new Key("jqbkhpvxy"),
                    new Key("\0\0\0la\0\0\0\0")
                },
                {
                    new Key("\0\0c\0t\0\0\0\0"),
                    new Key("\0f'\0iz*.-"),
                    new Key("d\0\0\0e\0\0\0\0")
                },
            }
        }
    };

    static LayoutPresets? _layoutPresets;
}