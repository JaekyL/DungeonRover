using UnityEngine;

namespace TraversalAI.InfluenceMap
{
    /// <summary>
    /// A single layer of the influence map, representing one type of influence.
    /// Operates on a 2D grid with propagation and decay.
    /// </summary>
    [System.Serializable]
    public class InfluenceLayer
    {
        public InfluenceLayerType LayerType { get; }
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        private float[,] _values;
        private float[,] _buffer;

        public float DecayRate = 0.05f;
        public float PropagationRate = 0.1f;

        public InfluenceLayer(InfluenceLayerType type, int width, int height, float cellSize = 1f)
        {
            LayerType = type;
            Width = width;
            Height = height;
            CellSize = cellSize;
            _values = new float[width, height];
            _buffer = new float[width, height];
        }

        public void SetInfluence(Vector3 worldPos, float value)
        {
            var (x, y) = WorldToGrid(worldPos);
            if (InBounds(x, y))
                _values[x, y] = Mathf.Clamp01(value);
        }

        public void AddInfluence(Vector3 worldPos, float amount)
        {
            var (x, y) = WorldToGrid(worldPos);
            if (InBounds(x, y))
                _values[x, y] = Mathf.Clamp01(_values[x, y] + amount);
        }

        public float Sample(Vector3 worldPos)
        {
            var (x, y) = WorldToGrid(worldPos);
            return InBounds(x, y) ? _values[x, y] : 0f;
        }

        public float SampleGrid(int x, int y)
        {
            return InBounds(x, y) ? _values[x, y] : 0f;
        }

        /// <summary>Update the layer: propagate and decay influence.</summary>
        public void Update()
        {
            System.Array.Copy(_values, _buffer, _values.Length);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float neighborSum = 0f;
                    int neighborCount = 0;

                    if (x > 0) { neighborSum += _buffer[x - 1, y]; neighborCount++; }
                    if (x < Width - 1) { neighborSum += _buffer[x + 1, y]; neighborCount++; }
                    if (y > 0) { neighborSum += _buffer[x, y - 1]; neighborCount++; }
                    if (y < Height - 1) { neighborSum += _buffer[x, y + 1]; neighborCount++; }

                    float avgNeighbor = neighborCount > 0 ? neighborSum / neighborCount : 0f;

                    _values[x, y] = _buffer[x, y] * (1f - DecayRate) + avgNeighbor * PropagationRate;
                    _values[x, y] = Mathf.Clamp01(_values[x, y]);
                }
            }
        }

        public void Clear()
        {
            System.Array.Clear(_values, 0, _values.Length);
        }

        public (int x, int y) WorldToGrid(Vector3 worldPos)
        {
            return (Mathf.FloorToInt(worldPos.x / CellSize), Mathf.FloorToInt(worldPos.z / CellSize));
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
        }

        private bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>Get raw grid values for debug visualization.</summary>
        public float[,] GetRawValues() => _values;
    }
}

