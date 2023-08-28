namespace ThumbKey;

[Serializable]
public record CharacterReplacement(string Original, char Replacement)
{
    public readonly string Original = Original;
    public readonly char Replacement = Replacement;
    public readonly int Count = Original.Length;
}