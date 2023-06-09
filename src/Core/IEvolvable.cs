namespace Core;

public interface IEvolvable<in T, TEvolvableTraits>
{
    public TEvolvableTraits Traits { get; }
    public double Fitness { get; }
    public void SetStimulus(T input);
    public void Kill();
    public void ResetFitness();
    public void Mutate(double percentageOfCharactersToMutate);
    public void OverwriteTraits(TEvolvableTraits newTraits);
}

public interface IEvolverAsexual<TData, TEvolvable, TEvolvableTraits> where TEvolvable : IEvolvable<TData, TEvolvableTraits>
{
    public static abstract void Reproduce(TEvolvable parent1, Span<TEvolvable> childrenToOverwrite);
}