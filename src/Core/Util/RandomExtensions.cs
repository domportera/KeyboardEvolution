namespace Core.Util;

public static class RandomExtensions
{
    // courtesy of https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
    public static void Shuffle<T> (this Random rng, IList<T> array)
    {
        int n = array.Count;
        while (n > 1) 
        {
            int k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }
}


public static class ArrayExtensions
{
    public static T Get<T>(this T[,] array, Array2DCoords coords)
    {
        return array[coords.RowY, coords.ColumnX];
    }
    
    public static void Set<T>(this T[,] array, Array2DCoords coords, T value)
    {
        array[coords.RowY, coords.ColumnX] = value;
    }
}