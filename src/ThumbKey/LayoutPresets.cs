namespace ThumbKey;

public enum PresetType
{
    ThumbKeyEngV4,
    ThumbKeyEngV4NoSymbols,
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
            PresetType.ThumbKeyEngV4NoSymbols,
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
                    new("\0f\0\0iz\0\0\0\0"),
                    new("d\0\0\0e\0\0\0\0")
                },
            }
        },
        {
            PresetType.FourColumn,
            new Key[,]
            {
                {
                    new("\0\0\0\0dk\0\0\0"),
                    new("\0\0\0\0i\0\0\0\0"),
                    new("\0\0\0\0u\0\0\0\0"),
                    new("\0\0\0ql\0\0\0\0")
                },
                {
                    new("\0'\0\0am\0\0\0"),
                    new("\0y\0gep\0x\0"),
                    new("\0v\0\0h\0\0\0\0"),
                    new("\0w\0ctf\0b\0")
                },
                {
                    new("\0j\0\0s\0\0\0\0"),
                    new("\0\0\0\0o\0\0\0\0"),
                    new("\0\0\0\0r\0\0\0\0"),
                    new("\0z\0\0n\0\0\0\0")
                },
            }
        }
    };
}