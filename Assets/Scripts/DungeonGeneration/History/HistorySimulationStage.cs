using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.History
{
    /// <summary>
    /// Stage 3: Simulates dungeon history through agent-based modification.
    /// </summary>
    public class HistorySimulationStage : IGenerationStage
    {
        public string StageName => "History Simulation";
        public int Priority => 300;
        private readonly Dictionary<HistoryAgentType, System.Func<IHistoryAgent>> _agentFactories;
        public HistorySimulationStage()
        {
            _agentFactories = new Dictionary<HistoryAgentType, System.Func<IHistoryAgent>>
            {
                { HistoryAgentType.Cultists, () => new Agents.CultistAgent() },
                { HistoryAgentType.Flooding, () => new Agents.FloodingAgent() },
                { HistoryAgentType.Monsters, () => new Agents.MonsterAgent() },
                { HistoryAgentType.Fire, () => new Agents.FireAgent() },
                { HistoryAgentType.Invaders, () => new Agents.InvaderAgent() },
                { HistoryAgentType.Corruption, () => new Agents.CorruptionAgent() },
            };
        }
        public void Execute(GenerationContext context)
        {
            var rng = context.Random.Fork("history");
            var log = new HistoryLog();
            context.HistoryLog = log;
            var activeAgents = new List<IHistoryAgent>();
            foreach (var agentConfig in context.Config.historyAgents.OrderBy(a => a.priority))
            {
                if (_agentFactories.TryGetValue(agentConfig.agentType, out var factory))
                {
                    var agent = factory();
                    agent.Initialize(context, agentConfig.intensity, rng.Fork(agent.AgentName));
                    activeAgents.Add(agent);
                }
            }
            // Simulate time steps
            for (int step = 0; step < context.Config.historySteps; step++)
            {
                foreach (var agent in activeAgents)
                {
                    agent.SimulateStep(step, context, rng.Fork($"{agent.AgentName}_{step}"));
                }
            }
        }
    }
}
