using System.Collections.Generic;
using DungeonGeneration.Data;
using UnityEngine;

namespace TraversalAI.Pathfinding
{
    /// <summary>
    /// A* pathfinder that operates on the SpatialMap tile grid.
    /// Finds walkable tile-level paths that respect walls, corridors, and doors.
    /// Used to generate detailed movement waypoints between graph nodes.
    /// </summary>
    public class TilePathfinder
    {
        private readonly SpatialMap _spatialMap;
        private readonly float _tileSize;

        // Cached walkability grid (true = walkable)
        private bool[,] _walkable;

        public TilePathfinder(SpatialMap spatialMap, float tileSize = 1f)
        {
            _spatialMap = spatialMap;
            _tileSize = tileSize;
            BuildWalkabilityGrid();
        }

        private void BuildWalkabilityGrid()
        {
            _walkable = new bool[_spatialMap.Width, _spatialMap.Height];
            for (int x = 0; x < _spatialMap.Width; x++)
            {
                for (int y = 0; y < _spatialMap.Height; y++)
                {
                    var tile = _spatialMap.Tiles[x, y];
                    _walkable[x, y] = IsWalkableTile(tile.Type);
                }
            }
        }

        private static bool IsWalkableTile(TileType type)
        {
            switch (type)
            {
                case TileType.Floor:
                case TileType.Corridor:
                case TileType.Door:
                case TileType.SecretDoor:
                case TileType.StairsUp:
                case TileType.StairsDown:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if a grid position is walkable.
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= _spatialMap.Width || y < 0 || y >= _spatialMap.Height)
                return false;
            return _walkable[x, y];
        }

        /// <summary>
        /// Convert a world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / _tileSize),
                Mathf.RoundToInt(worldPos.z / _tileSize)
            );
        }

        /// <summary>
        /// Convert grid coordinates to world position.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * _tileSize,
                0f,
                gridPos.y * _tileSize
            );
        }

        /// <summary>
        /// Find a walkable tile-level path between two world positions.
        /// Returns a list of world-space waypoints the agent should follow.
        /// </summary>
        public List<Vector3> FindTilePath(Vector3 startWorld, Vector3 endWorld)
        {
            var startGrid = WorldToGrid(startWorld);
            var endGrid = WorldToGrid(endWorld);

            // Snap to nearest walkable tile if start/end are on walls
            startGrid = FindNearestWalkable(startGrid);
            endGrid = FindNearestWalkable(endGrid);

            if (startGrid == endGrid)
            {
                return new List<Vector3> { GridToWorld(startGrid) };
            }

            // A* on the tile grid
            var gridPath = AStarGrid(startGrid, endGrid);
            if (gridPath == null || gridPath.Count == 0)
                return null;

            // Convert grid path to world waypoints and simplify
            var worldPath = new List<Vector3>(gridPath.Count);
            foreach (var gp in gridPath)
                worldPath.Add(GridToWorld(gp));

            // Simplify: remove collinear waypoints
            return SimplifyPath(worldPath);
        }

        /// <summary>
        /// Find tile path for a full sequence of graph nodes.
        /// Computes tile-level paths between consecutive node centers and concatenates them.
        /// </summary>
        public List<Vector3> FindTilePathForNodeSequence(List<Vector3> nodeWorldPositions)
        {
            if (nodeWorldPositions == null || nodeWorldPositions.Count < 2)
                return nodeWorldPositions ?? new List<Vector3>();

            var fullPath = new List<Vector3>();

            for (int i = 0; i < nodeWorldPositions.Count - 1; i++)
            {
                var segment = FindTilePath(nodeWorldPositions[i], nodeWorldPositions[i + 1]);
                if (segment == null || segment.Count == 0)
                {
                    // Pathfinding failed for this segment, fall back to direct
                    UnityEngine.Debug.LogWarning($"[TilePathfinder] No tile path found between nodes {i} and {i + 1}, using direct path.");
                    if (fullPath.Count == 0 || fullPath[fullPath.Count - 1] != nodeWorldPositions[i])
                        fullPath.Add(nodeWorldPositions[i]);
                    fullPath.Add(nodeWorldPositions[i + 1]);
                    continue;
                }

                // Append segment, skipping the first point if it duplicates the last
                int startIdx = 0;
                if (fullPath.Count > 0 && segment.Count > 0)
                {
                    if (Vector3.Distance(fullPath[fullPath.Count - 1], segment[0]) < _tileSize * 0.5f)
                        startIdx = 1;
                }

                for (int j = startIdx; j < segment.Count; j++)
                    fullPath.Add(segment[j]);
            }

            return fullPath;
        }

        /// <summary>
        /// Find the nearest walkable tile to the given grid position.
        /// </summary>
        private Vector2Int FindNearestWalkable(Vector2Int pos)
        {
            if (IsWalkable(pos.x, pos.y))
                return pos;

            // BFS spiral outward to find nearest walkable
            var visited = new HashSet<Vector2Int> { pos };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(pos);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbors = new Vector2Int[]
                {
                    current + Vector2Int.up,
                    current + Vector2Int.down,
                    current + Vector2Int.left,
                    current + Vector2Int.right
                };

                foreach (var n in neighbors)
                {
                    if (visited.Contains(n)) continue;
                    visited.Add(n);

                    if (IsWalkable(n.x, n.y))
                        return n;

                    if (_spatialMap.InBounds(n))
                        queue.Enqueue(n);
                }

                // Safety: don't search forever
                if (visited.Count > 1000) break;
            }

            return pos; // Couldn't find walkable, return original
        }

        /// <summary>
        /// Grid-level A* pathfinding (4-directional movement).
        /// </summary>
        private List<Vector2Int> AStarGrid(Vector2Int start, Vector2Int end)
        {
            var openSet = new PriorityQueue();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();

            gScore[start] = 0f;
            openSet.Enqueue(start, GridHeuristic(start, end));

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current == end)
                    return ReconstructGridPath(cameFrom, current);

                // 4-directional neighbors
                var neighbors = new Vector2Int[]
                {
                    current + Vector2Int.up,
                    current + Vector2Int.down,
                    current + Vector2Int.left,
                    current + Vector2Int.right
                };

                foreach (var neighbor in neighbors)
                {
                    if (!IsWalkable(neighbor.x, neighbor.y)) continue;

                    float tentativeG = gScore[current] + 1f;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        float f = tentativeG + GridHeuristic(neighbor, end);
                        openSet.Enqueue(neighbor, f);
                    }
                }
            }

            return null; // No path found
        }

        private static float GridHeuristic(Vector2Int a, Vector2Int b)
        {
            // Manhattan distance for 4-directional movement
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static List<Vector2Int> ReconstructGridPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Remove intermediate waypoints that are on the same straight line.
        /// Keeps corners and direction changes.
        /// </summary>
        private static List<Vector3> SimplifyPath(List<Vector3> path)
        {
            if (path.Count <= 2) return path;

            var simplified = new List<Vector3> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[i + 1];

                // Check if direction changes
                var dir1 = (curr - prev).normalized;
                var dir2 = (next - curr).normalized;

                if (Vector3.Dot(dir1, dir2) < 0.999f)
                {
                    simplified.Add(curr);
                }
            }

            simplified.Add(path[path.Count - 1]);
            return simplified;
        }

        /// <summary>
        /// Simple min-priority queue for A*.
        /// </summary>
        private class PriorityQueue
        {
            private readonly SortedList<float, Queue<Vector2Int>> _buckets = new SortedList<float, Queue<Vector2Int>>();
            private int _count;

            public int Count => _count;

            public void Enqueue(Vector2Int item, float priority)
            {
                if (!_buckets.TryGetValue(priority, out var queue))
                {
                    queue = new Queue<Vector2Int>();
                    _buckets[priority] = queue;
                }
                queue.Enqueue(item);
                _count++;
            }

            public Vector2Int Dequeue()
            {
                var first = _buckets.Keys[0];
                var queue = _buckets[first];
                var item = queue.Dequeue();
                if (queue.Count == 0)
                    _buckets.Remove(first);
                _count--;
                return item;
            }
        }
    }
}


