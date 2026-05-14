using UnityEngine;

namespace TraversalAI.InfluenceMap
{
    /// <summary>
    /// Simplified interface for querying influence maps.
    /// Used by traversal strategies and utility scoring.
    /// </summary>
    public class InfluenceSampler
    {
        private InfluenceMap _influenceMap;

        public InfluenceSampler(InfluenceMap map)
        {
            _influenceMap = map;
        }

        public float SampleLayer(InfluenceLayerType type, Vector3 worldPos)
        {
            return _influenceMap?.Sample(type, worldPos) ?? 0f;
        }

        /// <summary>
        /// Get a composite "desirability" score at a position.
        /// Combines multiple layers with configurable weights.
        /// </summary>
        public float GetDesirability(Vector3 worldPos,
            float dangerWeight = -1f,
            float curiosityWeight = 1f,
            float lootWeight = 0.8f,
            float safetyWeight = 0.5f)
        {
            float score = 0f;
            score += SampleLayer(InfluenceLayerType.Danger, worldPos) * dangerWeight;
            score += SampleLayer(InfluenceLayerType.ExplorationCuriosity, worldPos) * curiosityWeight;
            score += SampleLayer(InfluenceLayerType.LootDensity, worldPos) * lootWeight;
            score += SampleLayer(InfluenceLayerType.Safety, worldPos) * safetyWeight;
            return score;
        }

        public bool IsDangerous(Vector3 worldPos, float threshold = 0.6f)
        {
            return SampleLayer(InfluenceLayerType.Danger, worldPos) >= threshold;
        }
    }
}

