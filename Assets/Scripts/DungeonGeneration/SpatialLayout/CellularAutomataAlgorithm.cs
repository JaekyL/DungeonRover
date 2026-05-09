using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.SpatialLayout
{
    /// <summary>
    /// Cellular automata algorithm for organic cave-like layouts.
    /// </summary>
    public class CellularAutomataAlgorithm : ISpatialLayoutAlgorithm
    {
        public string AlgorithmName => "Cellular Automata Caves";
        public int Iterations { get; set; } = 5;
        public float InitialFillProbability { get; set; } = 0.45f;
        public int BirthThreshold { get; set; } = 5;
        public int DeathThreshold { get; set; } = 4;
        public void Generate(SpatialMap map, DungeonGraph graph, DungeonConfig config, SeededRandom rng)
        {
            int w = map.Width, h = map.Height;
            bool[,] cells = new bool[w, h];
            // Initialize randomly
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                        cells[x, y] = true; // walls at border
                    else
                        cells[x, y] = rng.NextFloat() < InitialFillProbability;
                }
            // Iterate cellular automata
            for (int iter = 0; iter < Iterations; iter++)
            {
                var next = new bool[w, h];
                for (int x = 1; x < w - 1; x++)
                    for (int y = 1; y < h - 1; y++)
                    {
                        int neighbors = CountNeighbors(cells, x, y);
                        next[x, y] = cells[x, y]
                            ? neighbors >= DeathThreshold
                            : neighbors >= BirthThreshold;
                    }
                // Keep borders as walls
                for (int x = 0; x < w; x++) { next[x, 0] = true; next[x, h - 1] = true; }
                for (int y = 0; y < h; y++) { next[0, y] = true; next[w - 1, y] = true; }
                cells = next;
            }
            // Apply to spatial map
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    map.SetTile(x, y, cells[x, y] ? TileType.Wall : TileType.Floor);
        }
        private int CountNeighbors(bool[,] cells, int cx, int cy)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= cells.GetLength(0) || ny >= cells.GetLength(1))
                        count++;
                    else if (cells[nx, ny])
                        count++;
                }
            return count;
        }
    }
}
