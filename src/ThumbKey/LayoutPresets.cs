namespace ThumbKey;

public enum PresetType
{
    ThumbKeyEngV4,
    FourColumn
}

public static class LayoutPresets
{
    public static readonly IReadOnlyDictionary<PresetType, Key[,]> Presets = new Dictionary<PresetType, Key[,]>()
    {
        {
            PresetType.ThumbKeyEngV4,
            new Key[,]
            {
                {
                    new("\0\0\0\0s\0\0\0w"),
                    new("\0\0\0\0r\0\0g\0"),
                    new("\0\0\0\0o\0u\0\0")
                },
                {
                    new("\0\0\0\0nm\0\0\0"),
                    new("jqbkhpvxy"),
                    new("\0\0\0la\0\0\0\0")
                },
                {
                    new("\0\0c\0t\0\0\0\0"),
                    new("\0f'\0iz*.-"),
                    new("d\0\0\0e\0\0\0\0")
                },
            }
        },
        {
            PresetType.FourColumn,
            new Key[,]
            {
                {
                    new("\0\0\0\0h\0\0\0\0"),
                    new("\0\0\0ql\0\0w\0"),
                    new("\0\0\0kt\0\0g\0"),
                    new("\0\0\0\0o\0\0\0\0")
                },
                {
                    new("\0\0\0\0nm\0\0\0"),
                    new("\0j\0vi\0\0\0\0"),
                    new("\0y\0cap\0\0\0"),
                    new("\0\0\0zs\0\0\0\0")
                },
                {
                    new("\0\0\0\0r\0\0\0\0"),
                    new("\0\0\0\0u\0\0\0\0"),
                    new("\0f\0be\0\0\0\0"),
                    new("\0\0\0xd\0\0\0\0")
                },
            }
        }
    };
}