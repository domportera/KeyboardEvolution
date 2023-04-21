namespace Core;

public interface IEvolvable<T>
{
    public double Fitness { get; }
    public void AddStimulus(T input);
}

public interface IEvolver<TData, TEvolvable> where TEvolvable : IEvolvable<TData>
{
    
    public void Mutate(float percentage);
}