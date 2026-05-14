using System.Collections.Generic;
using TraversalAI.BehaviorRules;
using TraversalAI.Strategy;
using TraversalAI.UtilityAI;
using UnityEngine;

namespace TraversalAI.Configuration
{
    /// <summary>
    /// Exploration stance presets that globally modify utility scoring and strategy selection.
    /// </summary>
    public enum ExplorationStance
    {
        Cautious,    // Prefers safe explored routes, avoids danger, BFS tendencies
        Aggressive,  // Prioritizes deep traversal, accepts danger, DFS tendencies
        Thorough,    // Maximizes map completion
        Efficient,   // Minimizes redundant travel
        Sneaky       // Prefers low visibility routes
    }

    /// <summary>
    /// ScriptableObject that holds the complete AI behavior configuration.
    /// This is the main data-driven configuration asset.
    /// </summary>
    [CreateAssetMenu(fileName = "TraversalProfile", menuName = "Traversal AI/Traversal Profile")]
    public class TraversalProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for this profile.")]
        public string profileName = "Default Explorer";

        [Tooltip("Description of this profile's behavior.")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("Exploration Stance")]
        public ExplorationStance stance = ExplorationStance.Thorough;

        [Header("Traversal Strategy")]
        [Tooltip("Which traversal strategy to use. Index into the strategy registry.")]
        public TraversalStrategyType primaryStrategy = TraversalStrategyType.UnvisitedPreference;

        [Header("Utility Weights")]
        [Range(0f, 2f)] public float explorationWeight = 1f;
        [Range(0f, 2f)] public float dangerAvoidanceWeight = 1f;
        [Range(0f, 2f)] public float lootWeight = 0.5f;
        [Range(0f, 2f)] public float progressionWeight = 0.5f;
        [Range(0f, 2f)] public float safetyWeight = 0.5f;

        [Header("Danger & Risk")]
        [Range(0f, 1f)] public float dangerTolerance = 0.5f;
        [Range(0f, 1f)] public float retreatHealthThreshold = 0.25f;

        [Header("Perception")]
        [Range(1, 5)] public int perceptionDepth = 2;
        public bool canDetectSecrets;

        [Header("Behavior Rules")]
        public List<BehaviorRule> behaviorRules = new List<BehaviorRule>();

        [Header("Utility Considerations")]
        public List<UtilityConsideration> considerations = new List<UtilityConsideration>();

        /// <summary>Apply stance modifiers to utility weights.</summary>
        public void ApplyStanceModifiers()
        {
            switch (stance)
            {
                case ExplorationStance.Cautious:
                    dangerAvoidanceWeight = 1.8f;
                    safetyWeight = 1.5f;
                    explorationWeight = 0.7f;
                    dangerTolerance = 0.2f;
                    break;
                case ExplorationStance.Aggressive:
                    dangerAvoidanceWeight = 0.3f;
                    explorationWeight = 0.8f;
                    progressionWeight = 1.5f;
                    dangerTolerance = 0.8f;
                    break;
                case ExplorationStance.Thorough:
                    explorationWeight = 1.8f;
                    lootWeight = 1.2f;
                    progressionWeight = 0.3f;
                    break;
                case ExplorationStance.Efficient:
                    explorationWeight = 1.0f;
                    progressionWeight = 1.2f;
                    lootWeight = 0.3f;
                    break;
                case ExplorationStance.Sneaky:
                    dangerAvoidanceWeight = 1.5f;
                    safetyWeight = 0.8f;
                    explorationWeight = 1.0f;
                    dangerTolerance = 0.3f;
                    break;
            }
        }
    }

    /// <summary>
    /// Registry of traversal strategy types for serialization.
    /// </summary>
    public enum TraversalStrategyType
    {
        DepthFirst,
        BreadthFirst,
        Spiral,
        WallFollowRight,
        WallFollowLeft,
        HubAndSpoke,
        SectorSweep,
        UnvisitedPreference,
        SafeRadius,
        StraightBias
    }

    /// <summary>
    /// Factory to create strategy instances from type enum.
    /// </summary>
    public static class TraversalStrategyFactory
    {
        public static ITraversalStrategy Create(TraversalStrategyType type)
        {
            switch (type)
            {
                case TraversalStrategyType.DepthFirst:
                    return new Strategy.Strategies.DepthFirstTraversal();
                case TraversalStrategyType.BreadthFirst:
                    return new Strategy.Strategies.BreadthFirstTraversal();
                case TraversalStrategyType.Spiral:
                    return new Strategy.Strategies.SpiralTraversal();
                case TraversalStrategyType.WallFollowRight:
                    return new Strategy.Strategies.WallFollowTraversal(true);
                case TraversalStrategyType.WallFollowLeft:
                    return new Strategy.Strategies.WallFollowTraversal(false);
                case TraversalStrategyType.HubAndSpoke:
                    return new Strategy.Strategies.HubAndSpokeTraversal();
                case TraversalStrategyType.SectorSweep:
                    return new Strategy.Strategies.SectorSweepTraversal();
                case TraversalStrategyType.UnvisitedPreference:
                    return new Strategy.Strategies.UnvisitedPreferenceTraversal();
                case TraversalStrategyType.SafeRadius:
                    return new Strategy.Strategies.SafeRadiusTraversal();
                case TraversalStrategyType.StraightBias:
                    return new Strategy.Strategies.StraightBiasTraversal();
                default:
                    return new Strategy.Strategies.UnvisitedPreferenceTraversal();
            }
        }
    }
}

