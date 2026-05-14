using TraversalAI.Core;

namespace TraversalAI.Goals
{
    /// <summary>Explore an unvisited/unexplored node.</summary>
    public class ExploreNodeGoal : BaseTraversalGoal
    {
        public override string GoalType => "ExploreNode";

        public ExploreNodeGoal(int targetNodeId, float reward = 0.5f)
        {
            TargetNodeId = targetNodeId;
            BasePriority = 0.6f;
            EstimatedReward = reward;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            return node != null && node.HasTag(NodeTag.Unexplored);
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId && context.Memory.HasVisited(TargetNodeId);
        }

        public override string GetDescription() => $"Explore unexplored node {TargetNodeId}";
    }

    /// <summary>Descend stairs to the next floor.</summary>
    public class DescendStairsGoal : BaseTraversalGoal
    {
        public override string GoalType => "DescendStairs";

        public DescendStairsGoal(int staircaseNodeId)
        {
            TargetNodeId = staircaseNodeId;
            BasePriority = 0.4f;
            EstimatedReward = 0.8f;
            EstimatedRisk = 0.3f;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            return node != null && node.HasTag(NodeTag.Staircase);
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }

        public override string GetDescription() => $"Descend stairs at node {TargetNodeId}";
    }

    /// <summary>Investigate a room of interest.</summary>
    public class InvestigateRoomGoal : BaseTraversalGoal
    {
        public override string GoalType => "InvestigateRoom";

        public InvestigateRoomGoal(int roomNodeId, float curiosityValue = 0.5f)
        {
            TargetNodeId = roomNodeId;
            BasePriority = 0.5f;
            EstimatedReward = curiosityValue;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            if (node == null) return false;
            var info = context.PerceivedState.GetNodeInfo(TargetNodeId);
            return info.VisitCount < 2;
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }

        public override string GetDescription() => $"Investigate room at node {TargetNodeId}";
    }

    /// <summary>Loot a container/treasure room.</summary>
    public class LootContainerGoal : BaseTraversalGoal
    {
        public override string GoalType => "LootContainer";

        public LootContainerGoal(int lootNodeId, float value = 0.7f)
        {
            TargetNodeId = lootNodeId;
            BasePriority = 0.7f;
            EstimatedReward = value;
        }

        public override bool IsValid(GoalContext context)
        {
            if (context.InventoryFullness >= 0.95f) return false;
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            return node != null && node.HasTag(NodeTag.Loot);
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }

        public override string GetDescription() => $"Loot container at node {TargetNodeId}";
    }

    /// <summary>Avoid a known threat by moving away.</summary>
    public class AvoidThreatGoal : BaseTraversalGoal
    {
        public int ThreatNodeId { get; set; }
        public override string GoalType => "AvoidThreat";

        public AvoidThreatGoal(int safeNodeId, int threatNodeId)
        {
            TargetNodeId = safeNodeId;
            ThreatNodeId = threatNodeId;
            BasePriority = 0.9f;
            EstimatedRisk = 0.1f;
        }

        public override bool IsValid(GoalContext context)
        {
            return context.DungeonGraph.GetNode(TargetNodeId) != null &&
                   context.CurrentHealth < context.DangerTolerance;
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }

        public override string GetDescription() => $"Avoid threat at {ThreatNodeId}, flee to {TargetNodeId}";
    }

    /// <summary>Retreat to a safe zone.</summary>
    public class RetreatGoal : BaseTraversalGoal
    {
        public override string GoalType => "Retreat";

        public RetreatGoal(int safeNodeId)
        {
            TargetNodeId = safeNodeId;
            BasePriority = 0.95f;
            EstimatedRisk = 0f;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            return node != null && node.HasTag(NodeTag.SafeZone) && context.CurrentHealth < 0.3f;
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }

        public override string GetDescription() => $"Retreat to safe zone at node {TargetNodeId}";
    }

    /// <summary>Search a dead end thoroughly.</summary>
    public class SearchDeadEndGoal : BaseTraversalGoal
    {
        public override string GoalType => "SearchDeadEnd";

        public SearchDeadEndGoal(int deadEndNodeId)
        {
            TargetNodeId = deadEndNodeId;
            BasePriority = 0.3f;
            EstimatedReward = 0.4f;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            if (node == null) return false;
            var info = context.PerceivedState.GetNodeInfo(TargetNodeId);
            return node.HasTag(NodeTag.DeadEnd) && info.VisitCount < 1;
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }
    }

    /// <summary>Regroup with allies at a meeting point.</summary>
    public class RegroupGoal : BaseTraversalGoal
    {
        public override string GoalType => "Regroup";

        public RegroupGoal(int meetingNodeId)
        {
            TargetNodeId = meetingNodeId;
            BasePriority = 0.6f;
        }

        public override bool IsValid(GoalContext context)
        {
            return context.DungeonGraph.GetNode(TargetNodeId) != null;
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }
    }

    /// <summary>Unlock a shortcut passage.</summary>
    public class UnlockShortcutGoal : BaseTraversalGoal
    {
        public override string GoalType => "UnlockShortcut";

        public UnlockShortcutGoal(int shortcutNodeId)
        {
            TargetNodeId = shortcutNodeId;
            BasePriority = 0.5f;
            EstimatedReward = 0.6f;
        }

        public override bool IsValid(GoalContext context)
        {
            var node = context.DungeonGraph.GetNode(TargetNodeId);
            return node != null && node.HasTag(NodeTag.Shortcut);
        }

        public override bool IsComplete(GoalContext context)
        {
            return context.CurrentNodeId == TargetNodeId;
        }
    }
}

