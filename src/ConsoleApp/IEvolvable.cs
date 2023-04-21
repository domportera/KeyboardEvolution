namespace ConsoleApp;

public interface IEvolvable<T>
{
    public double Fitness { get; }
    public void AddStimulus(T input);
    public void Mutate(float percentage);
    public void ResetFitness();
}