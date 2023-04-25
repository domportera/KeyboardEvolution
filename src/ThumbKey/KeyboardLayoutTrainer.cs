using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Core;
using Core.Util;

namespace ThumbKey;

public partial class KeyboardLayoutTrainer : IEvolverAsexual<string, KeyboardLayout, Key[,]>
{
    // todo: all punctuation in alphabet?
    public static readonly FrozenSet<char> CharacterSetDict = CharacterSet.ToFrozenSet();

    public KeyboardLayoutTrainer(int count, int iterationCount, int seed)
    {
        double[,] positionPreferences = PositionPreferences[Dimensions];
        
        var layouts = new KeyboardLayout[count];
        for (int i = 0; i < count; i++)
        {
            layouts[i] = new KeyboardLayout(Dimensions, CharacterSet, seed + i, UseStandardSpaceBar, positionPreferences, in FitnessWeights, SwipeDirectionPreferences);
        }

        var input = File.ReadAllText(InputFilePath);
        EvolutionLoop(iterationCount, input, layouts);
    }
    
    void EvolutionLoop(int iterationCount, string input, KeyboardLayout[] layouts)
    {
        LayoutVisualizer[] visualizers = layouts.AsParallel().Select(x => new LayoutVisualizer(x)).ToArray();
        
        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
        
        for (int i = 0; i < iterationCount; i++)
        {
            if(i % 10 == 0)
                visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
            
            layouts.AsParallel().ForAll(layout => layout.AddStimulus(input));
            
            
            //  evolve
            EvolveLayouts(ref layouts);
            
        }
        
        Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<< FINAL RESULTS >>>>>>>>>>>>>>>>>>>>>>>>>");
        visualizers.AsParallel().ForAll(visualizer => visualizer.Visualize());
    }

    static void EvolveLayouts(ref KeyboardLayout[] layouts)
    {
        layouts = layouts.AsParallel().OrderByDescending(layout => layout.Fitness).ToArray();
        
        int quantityToReproduce = (int)Math.Floor(layouts.Length * ReproductionPercentage);
        quantityToReproduce = quantityToReproduce % 2 == 0 ? quantityToReproduce : quantityToReproduce + 1;
        
        int quantityToReplace = layouts.Length - quantityToReproduce;
        
        Debug.Assert(ReproductionPercentage <= 0.5);
        
        double childrenPerCouple = quantityToReplace / (double) quantityToReproduce;

        for (int i = 0; i < layouts.Length; i ++)
        {
            var parent = layouts[i];
            int childCount = (int)childrenPerCouple;

            int childStartIndex = layouts.Length - 1 - i - childCount;
            int childEndIndex = childStartIndex + childCount;
            
            childStartIndex = childStartIndex < i ? childStartIndex : i;

            if (childStartIndex > childEndIndex) // we've populated them all!
                break;
            
            // get children from end of array
            Span<KeyboardLayout> childrenToOverwrite = layouts
                .AsSpan()
                .Slice(layouts.Length - 1 - i - childCount,childCount);
            
            Reproduce(parent, childrenToOverwrite);
        }
    }

    public static void Reproduce(KeyboardLayout parent, Span<KeyboardLayout> childrenToOverwrite)
    {
        Key[,] parentKeys = parent.Traits;

        foreach (var child in childrenToOverwrite)
        {
            child.OverwriteTraits(parentKeys);
            child.Mutate(MutationFactor);
        }
    }
    
    
}