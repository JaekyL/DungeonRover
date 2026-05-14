using TraversalAI.InfluenceMap;
using UnityEngine;

namespace TraversalAI.Debug
{
    /// <summary>
    /// Standalone influence map visualizer component.
    /// Can be placed on any GameObject to visualize influence maps in the scene.
    /// </summary>
    public class InfluenceMapVisualizer : MonoBehaviour
    {
        [Header("Settings")]
        public InfluenceLayerType layerToVisualize = InfluenceLayerType.Danger;
        [Range(0.01f, 1f)] public float minDisplayThreshold = 0.05f;
        [Range(0.1f, 5f)] public float cellDisplayScale = 0.9f;
        public bool showGrid = true;

        [Header("Colors")]
        public Gradient colorGradient;

        private InfluenceMap.InfluenceMap _map;

        public void SetInfluenceMap(InfluenceMap.InfluenceMap map)
        {
            _map = map;
        }

        private void Reset()
        {
            // Default gradient: blue to red
            colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.blue, 0f),
                    new GradientColorKey(Color.yellow, 0.5f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.1f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0.8f, 1f)
                }
            );
        }

        private void OnDrawGizmos()
        {
            if (_map == null) return;

            var layer = _map.GetLayer(layerToVisualize);
            if (layer == null) return;

            var values = layer.GetRawValues();

            for (int x = 0; x < layer.Width; x++)
            {
                for (int y = 0; y < layer.Height; y++)
                {
                    float v = values[x, y];
                    if (v < minDisplayThreshold) continue;

                    Vector3 pos = layer.GridToWorld(x, y);
                    Gizmos.color = colorGradient != null
                        ? colorGradient.Evaluate(v)
                        : Color.Lerp(Color.blue, Color.red, v);

                    Gizmos.DrawCube(pos, Vector3.one * layer.CellSize * cellDisplayScale);
                }
            }

            if (showGrid)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.05f);
                for (int x = 0; x <= layer.Width; x++)
                {
                    Vector3 start = new Vector3(x * layer.CellSize, 0, 0);
                    Vector3 end = new Vector3(x * layer.CellSize, 0, layer.Height * layer.CellSize);
                    Gizmos.DrawLine(start, end);
                }
                for (int y = 0; y <= layer.Height; y++)
                {
                    Vector3 start = new Vector3(0, 0, y * layer.CellSize);
                    Vector3 end = new Vector3(layer.Width * layer.CellSize, 0, y * layer.CellSize);
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}

