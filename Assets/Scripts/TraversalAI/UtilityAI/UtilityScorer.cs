using System.Collections.Generic;
using TraversalAI.Goals;
using UnityEngine;

namespace TraversalAI.UtilityAI
{
    /// <summary>
    /// Scores a goal using a list of weighted utility considerations.
    /// Uses multiplicative scoring (Dual Utility Theory approach).
    /// </summary>
    [System.Serializable]
    public class UtilityScorer
    {
        [SerializeField] private List<UtilityConsideration> _considerations = new List<UtilityConsideration>();

        /// <summary>
        /// Calculate the utility score for a goal given context.
        /// Returns a float where higher = more desirable.
        /// </summary>
        public float Score(ITraversalGoal goal, GoalContext context, ScoringContext scoringContext)
        {
            if (_considerations.Count == 0) return goal.BasePriority;

            float finalScore = goal.BasePriority;
            float modificationFactor = 1f - (1f / _considerations.Count);

            foreach (var consideration in _considerations)
            {
                if (consideration == null) continue;

                float input = GetNormalizedInput(consideration.inputType, goal, context, scoringContext);
                float score = consideration.Evaluate(input);

                // Compensation factor to prevent single low score from zeroing everything
                float makeUpValue = (1f - score) * modificationFactor;
                score += makeUpValue * score;

                finalScore *= Mathf.Clamp01(score);

                if (finalScore < 0.01f) return 0f;
            }

            return finalScore;
        }

        private float GetNormalizedInput(ConsiderationInput inputType, ITraversalGoal goal,
            GoalContext context, ScoringContext scoringContext)
        {
            switch (inputType)
            {
                case ConsiderationInput.Distance: return scoringContext.NormalizedDistance;
                case ConsiderationInput.Danger: return goal.EstimatedRisk;
                case ConsiderationInput.LootValue: return goal.EstimatedReward;
                case ConsiderationInput.ExplorationValue: return scoringContext.ExplorationValue;
                case ConsiderationInput.Health: return context.CurrentHealth;
                case ConsiderationInput.Resources: return context.CurrentResources;
                case ConsiderationInput.InventoryFullness: return context.InventoryFullness;
                case ConsiderationInput.NodeVisitCount: return scoringContext.NormalizedVisitCount;
                case ConsiderationInput.TimeSinceLastSeen: return scoringContext.NormalizedStaleness;
                case ConsiderationInput.DangerTolerance: return context.DangerTolerance;
                case ConsiderationInput.AllyProximity: return scoringContext.AllyProximity;
                case ConsiderationInput.NoiseLevel: return scoringContext.NoiseLevel;
                default: return 0.5f;
            }
        }

        public void AddConsideration(UtilityConsideration consideration)
        {
            _considerations.Add(consideration);
        }
    }

    /// <summary>
    /// Pre-computed contextual values for scoring a specific goal.
    /// </summary>
    [System.Serializable]
    public struct ScoringContext
    {
        public float NormalizedDistance;
        public float ExplorationValue;
        public float NormalizedVisitCount;
        public float NormalizedStaleness;
        public float AllyProximity;
        public float NoiseLevel;
    }
}

