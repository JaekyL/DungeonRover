using DungeonGeneration.Core;

namespace DungeonGeneration.History
{
    /// <summary>
    /// Interface for agents that simulate dungeon history.
    /// Each agent modifies the dungeon over simulated time steps.
    /// </summary>
    public interface IHistoryAgent
    {
        string AgentName { get; }
        int Priority { get; }
        void Initialize(GenerationContext context, float intensity, SeededRandom rng);
        void SimulateStep(int step, GenerationContext context, SeededRandom rng);
    }
}
