namespace ThumbKey;

[Serializable]
public record CharacterReplacement(char Original, char Replacement)
{
    public readonly char Original = Original;
    public readonly char Replacement = Replacement;
}