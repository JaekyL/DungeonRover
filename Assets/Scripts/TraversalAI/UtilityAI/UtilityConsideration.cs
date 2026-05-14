using UnityEngine;

namespace TraversalAI.UtilityAI
{
    /// <summary>
    /// A single scoring consideration used in utility evaluation.
    /// ScriptableObject for data-driven configuration.
    /// Maps an input value [0..1] through a response curve to produce a score.
    /// </summary>
    [CreateAssetMenu(fileName = "UtilityConsideration", menuName = "Traversal AI/Utility Consideration")]
    public class UtilityConsideration : ScriptableObject
    {
        [Tooltip("Name of this consideration for debugging.")]
        public string considerationName = "Unnamed";

        [Tooltip("What input parameter this consideration evaluates.")]
        public ConsiderationInput inputType = ConsiderationInput.Distance;

        [Tooltip("Weight multiplier for this consideration.")]
        [Range(0f, 2f)]
        public float weight = 1f;

        [Tooltip("Response curve mapping input [0..1] to output score [0..1].")]
        public AnimationCurve responseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Whether to invert the result (1 - score).")]
        public bool invert;

        /// <summary>Evaluate this consideration given a normalized input [0..1].</summary>
        public float Evaluate(float normalizedInput)
        {
            float score = responseCurve.Evaluate(Mathf.Clamp01(normalizedInput));
            if (invert) score = 1f - score;
            return score * weight;
        }
    }

    /// <summary>Types of inputs that considerations can evaluate.</summary>
    public enum ConsiderationInput
    {
        Distance,
        Danger,
        LootValue,
        ExplorationValue,
        Health,
        Resources,
        InventoryFullness,
        NodeVisitCount,
        TimeSinceLastSeen,
        DangerTolerance,
        AllyProximity,
        NoiseLevel
    }
}

