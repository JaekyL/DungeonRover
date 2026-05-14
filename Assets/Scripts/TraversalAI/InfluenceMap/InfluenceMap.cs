using System.Collections.Generic;
using UnityEngine;

namespace TraversalAI.InfluenceMap
{
    /// <summary>
    /// Container for multiple influence layers. Provides unified access
    /// for querying and updating all layers.
    /// </summary>
    public class InfluenceMap
    {
        private Dictionary<InfluenceLayerType, InfluenceLayer> _layers
            = new Dictionary<InfluenceLayerType, InfluenceLayer>();

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        public InfluenceMap(int width, int height, float cellSize = 1f)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;

            foreach (InfluenceLayerType type in System.Enum.GetValues(typeof(InfluenceLayerType)))
            {
                _layers[type] = new InfluenceLayer(type, width, height, cellSize);
            }
        }

        public InfluenceLayer GetLayer(InfluenceLayerType type)
        {
            return _layers.TryGetValue(type, out var layer) ? layer : null;
        }

        public void UpdateAll()
        {
            foreach (var layer in _layers.Values)
                layer.Update();
        }

        public void SetInfluence(InfluenceLayerType type, Vector3 worldPos, float value)
        {
            GetLayer(type)?.SetInfluence(worldPos, value);
        }

        public void AddInfluence(InfluenceLayerType type, Vector3 worldPos, float amount)
        {
            GetLayer(type)?.AddInfluence(worldPos, amount);
        }

        public float Sample(InfluenceLayerType type, Vector3 worldPos)
        {
            return GetLayer(type)?.Sample(worldPos) ?? 0f;
        }

        public void ClearAll()
        {
            foreach (var layer in _layers.Values)
                layer.Clear();
        }

        public IEnumerable<InfluenceLayerType> GetLayerTypes() => _layers.Keys;
    }
}

