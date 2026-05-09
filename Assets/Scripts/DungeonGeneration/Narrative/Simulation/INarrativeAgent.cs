using DungeonGeneration.Core;
using DungeonGeneration.Narrative.WorldState;

namespace DungeonGeneration.Narrative.Simulation
{
    /// <summary>
    /// Interface for narrative agents that simulate faction behavior during historical simulation.
    /// Unlike the existing IHistoryAgent, narrative agents operate on NarrativeWorldState
    /// and respond to authored faction definitions, motivations, and constraints.
    /// </summary>
    public interface INarrativeAgent
    {
        string AgentId { get; }
        string FactionId { get; }
        int Priority { get; }

        void Initialize(NarrativeWorldState worldState, GenerationContext context, SeededRandom rng);
        void SimulateStep(int step, NarrativeWorldState worldState, GenerationContext context, SeededRandom rng);
        void OnEventTriggered(string eventType, int roomId, NarrativeWorldState worldState);
        float GetDesperation();
    }
}

