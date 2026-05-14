using System.Linq;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.StateMachine.States
{
    /// <summary>Default exploration state: generate goals, evaluate, move to best target.</summary>
    public class ExploringState : ITraversalState
    {
        public string StateName => "Exploring";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Exploring state");
        }

        public void Update(TraversalStateContext context)
        {
            // Goal and movement are handled by TraversalAIController.
            // State just orchestrates the systems.
            if (context.CurrentGoal != null && context.CurrentGoal.TargetNodeId >= 0)
            {
                context.RequestMoveTo?.Invoke(context.CurrentGoal.TargetNodeId);
            }
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentHealth < 0.2f)
                return TraversalStateType.Retreating;

            if (context.CurrentGoal != null && context.CurrentGoal.GoalType == "DescendStairs")
                return TraversalStateType.Descending;

            if (context.CurrentGoal != null && context.CurrentGoal.GoalType == "AvoidThreat")
                return TraversalStateType.AvoidingThreat;

            return null;
        }
    }

    /// <summary>Searching state: systematically checking nearby areas.</summary>
    public class SearchingState : ITraversalState
    {
        public string StateName => "Searching";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Searching state");
        }

        public void Update(TraversalStateContext context)
        {
            if (context.CurrentGoal != null && context.CurrentGoal.TargetNodeId >= 0)
                context.RequestMoveTo?.Invoke(context.CurrentGoal.TargetNodeId);
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentHealth < 0.2f)
                return TraversalStateType.Retreating;

            // If no more search-type goals, go back to exploring
            if (context.CurrentGoal == null ||
                (context.CurrentGoal.GoalType != "SearchDeadEnd" &&
                 context.CurrentGoal.GoalType != "InvestigateRoom"))
                return TraversalStateType.Exploring;

            return null;
        }
    }

    /// <summary>Descending state: heading towards stairs.</summary>
    public class DescendingState : ITraversalState
    {
        public string StateName => "Descending";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Descending state");
        }

        public void Update(TraversalStateContext context)
        {
            if (context.CurrentGoal != null && context.CurrentGoal.TargetNodeId >= 0)
                context.RequestMoveTo?.Invoke(context.CurrentGoal.TargetNodeId);
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentGoal == null || context.CurrentGoal.GoalType != "DescendStairs")
                return TraversalStateType.Exploring;
            return null;
        }
    }

    /// <summary>Retreating state: fleeing to a safe zone.</summary>
    public class RetreatingState : ITraversalState
    {
        public string StateName => "Retreating";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Retreating state - Health critical!");
        }

        public void Update(TraversalStateContext context)
        {
            // Find nearest safe zone
            var safeNodes = context.Graph.GetNodesWithTag(NodeTag.SafeZone);
            if (safeNodes.Count > 0)
            {
                var currentNode = context.Graph.GetNode(context.CurrentNodeId);
                var nearest = safeNodes.OrderBy(n =>
                    Vector3.Distance(n.WorldPosition, currentNode?.WorldPosition ?? Vector3.zero)
                ).First();
                context.RequestMoveTo?.Invoke(nearest.Id);
            }
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            var currentNode = context.Graph.GetNode(context.CurrentNodeId);
            if (currentNode != null && currentNode.HasTag(NodeTag.SafeZone))
                return TraversalStateType.Resting;
            if (context.CurrentHealth > 0.5f)
                return TraversalStateType.Exploring;
            return null;
        }
    }

    /// <summary>Avoiding threat: moving away from danger.</summary>
    public class AvoidingThreatState : ITraversalState
    {
        public string StateName => "AvoidingThreat";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering AvoidingThreat state");
        }

        public void Update(TraversalStateContext context)
        {
            if (context.CurrentGoal != null && context.CurrentGoal.TargetNodeId >= 0)
                context.RequestMoveTo?.Invoke(context.CurrentGoal.TargetNodeId);
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentHealth < 0.2f)
                return TraversalStateType.Retreating;

            // If no longer near danger, resume exploring
            var neighbors = context.Graph.GetNeighbors(context.CurrentNodeId);
            bool dangerNearby = neighbors.Any(n => n.HasTag(NodeTag.Dangerous) || n.HasTag(NodeTag.EnemyPresence));
            if (!dangerNearby)
                return TraversalStateType.Exploring;

            return null;
        }
    }

    /// <summary>Resting state: recovering at a safe zone.</summary>
    public class RestingState : ITraversalState
    {
        public string StateName => "Resting";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Resting state - Recovering...");
        }

        public void Update(TraversalStateContext context)
        {
            // In a real game, health would regenerate here
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentHealth > 0.7f)
                return TraversalStateType.Exploring;
            return null;
        }
    }

    /// <summary>Regrouping state: meeting up with allies.</summary>
    public class RegroupingState : ITraversalState
    {
        public string StateName => "Regrouping";

        public void Enter(TraversalStateContext context)
        {
            UnityEngine.Debug.Log($"[AI] Entering Regrouping state");
        }

        public void Update(TraversalStateContext context)
        {
            if (context.CurrentGoal != null && context.CurrentGoal.TargetNodeId >= 0)
                context.RequestMoveTo?.Invoke(context.CurrentGoal.TargetNodeId);
        }

        public void Exit(TraversalStateContext context) { }

        public TraversalStateType? CheckTransition(TraversalStateContext context)
        {
            if (context.CurrentGoal == null || context.CurrentGoal.GoalType != "Regroup")
                return TraversalStateType.Exploring;
            return null;
        }
    }
}

