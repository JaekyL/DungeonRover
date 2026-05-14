using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using TraversalAI.Perception;

namespace TraversalAI.Goals
{
    /// <summary>
    /// Generates candidate traversal goals based on the AI's current perception and memory.
    /// Extensible via pluggable IGoalProvider components.
    /// </summary>
    public class GoalGenerator
    {
        private readonly List<IGoalProvider> _providers = new List<IGoalProvider>();

        public GoalGenerator()
        {
            _providers.Add(new ExplorationGoalProvider());
            _providers.Add(new LootGoalProvider());
            _providers.Add(new SurvivalGoalProvider());
            _providers.Add(new ProgressionGoalProvider());
        }

        /// <summary>Register a custom goal provider for extensibility.</summary>
        public void RegisterProvider(IGoalProvider provider)
        {
            _providers.Add(provider);
        }

        /// <summary>Generate all candidate goals given current context.</summary>
        public List<ITraversalGoal> GenerateGoals(GoalContext context)
        {
            var goals = new List<ITraversalGoal>();
            foreach (var provider in _providers)
            {
                goals.AddRange(provider.GenerateGoals(context));
            }
            return goals;
        }
    }

    /// <summary>
    /// Interface for pluggable goal providers. Implement this to add new goal types.
    /// </summary>
    public interface IGoalProvider
    {
        List<ITraversalGoal> GenerateGoals(GoalContext context);
    }

    /// <summary>Generates exploration goals for unexplored and unvisited nodes.</summary>
    public class ExplorationGoalProvider : IGoalProvider
    {
        public List<ITraversalGoal> GenerateGoals(GoalContext context)
        {
            var goals = new List<ITraversalGoal>();
            var knownNodes = context.PerceivedState.GetKnownNodeIds();

            foreach (var nodeId in knownNodes)
            {
                var node = context.DungeonGraph.GetNode(nodeId);
                if (node == null) continue;

                var info = context.PerceivedState.GetNodeInfo(nodeId);

                if (node.HasTag(NodeTag.Unexplored) && info.Visibility != VisibilityState.Unknown)
                {
                    goals.Add(new ExploreNodeGoal(nodeId));
                }

                if (info.VisitCount > 0 && info.VisitCount < 2 &&
                    (node.HasTag(NodeTag.HubRoom) || node.HasTag(NodeTag.Intersection)))
                {
                    goals.Add(new InvestigateRoomGoal(nodeId));
                }

                if (node.HasTag(NodeTag.DeadEnd) && info.VisitCount < 1)
                {
                    goals.Add(new SearchDeadEndGoal(nodeId));
                }
            }

            return goals;
        }
    }

    /// <summary>Generates loot-related goals.</summary>
    public class LootGoalProvider : IGoalProvider
    {
        public List<ITraversalGoal> GenerateGoals(GoalContext context)
        {
            var goals = new List<ITraversalGoal>();
            if (context.InventoryFullness >= 0.95f) return goals;

            var knownNodes = context.PerceivedState.GetKnownNodeIds();
            foreach (var nodeId in knownNodes)
            {
                var node = context.DungeonGraph.GetNode(nodeId);
                if (node == null) continue;

                if (node.HasTag(NodeTag.Loot))
                    goals.Add(new LootContainerGoal(nodeId, node.BaseLootValue));

                if (node.HasTag(NodeTag.Shortcut))
                    goals.Add(new UnlockShortcutGoal(nodeId));
            }

            return goals;
        }
    }

    /// <summary>Generates survival-related goals (retreat, avoid threat).</summary>
    public class SurvivalGoalProvider : IGoalProvider
    {
        public List<ITraversalGoal> GenerateGoals(GoalContext context)
        {
            var goals = new List<ITraversalGoal>();

            if (context.CurrentHealth < 0.3f)
            {
                var safeNodes = context.DungeonGraph.GetNodesWithTag(NodeTag.SafeZone);
                foreach (var safeNode in safeNodes)
                {
                    if (context.PerceivedState.GetNodeInfo(safeNode.Id).Visibility != VisibilityState.Unknown)
                        goals.Add(new RetreatGoal(safeNode.Id));
                }
            }

            if (context.CurrentHealth < context.DangerTolerance)
            {
                var neighbors = context.DungeonGraph.GetNeighbors(context.CurrentNodeId);
                var dangerousNeighbors = neighbors.Where(n =>
                    n.HasTag(NodeTag.Dangerous) || n.HasTag(NodeTag.EnemyPresence));

                foreach (var threat in dangerousNeighbors)
                {
                    var safeNeighbors = neighbors.Where(n =>
                        !n.HasTag(NodeTag.Dangerous) && !n.HasTag(NodeTag.EnemyPresence));
                    foreach (var safe in safeNeighbors)
                        goals.Add(new AvoidThreatGoal(safe.Id, threat.Id));
                }
            }

            return goals;
        }
    }

    /// <summary>Generates progression goals (stairs, deeper exploration).</summary>
    public class ProgressionGoalProvider : IGoalProvider
    {
        public List<ITraversalGoal> GenerateGoals(GoalContext context)
        {
            var goals = new List<ITraversalGoal>();
            var staircaseNodes = context.DungeonGraph.GetNodesWithTag(NodeTag.Staircase);

            foreach (var stairNode in staircaseNodes)
            {
                var info = context.PerceivedState.GetNodeInfo(stairNode.Id);
                if (info.Visibility != VisibilityState.Unknown)
                    goals.Add(new DescendStairsGoal(stairNode.Id));
            }

            return goals;
        }
    }
}

