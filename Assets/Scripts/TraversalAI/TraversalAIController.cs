using System.Collections.Generic;
using TraversalAI.BehaviorRules;
using TraversalAI.Configuration;
using TraversalAI.Core;
using TraversalAI.Goals;
using TraversalAI.Pathfinding;
using TraversalAI.Perception;
using TraversalAI.Strategy;
using TraversalAI.StateMachine;
using TraversalAI.StateMachine.States;
using TraversalAI.UtilityAI;
using UnityEngine;

namespace TraversalAI
{
    /// <summary>
    /// Main AI controller MonoBehaviour. Orchestrates all traversal subsystems.
    /// Attach to an AI explorer GameObject.
    ///
    /// System interaction flow:
    /// 1. PerceptionComponent scans the dungeon graph
    /// 2. GoalGenerator produces candidate goals from perceived state
    /// 3. RuleEvaluator checks player-configured behavior rules
    /// 4. GoalEvaluator scores goals using UtilityScorer + InfluenceSampler
    /// 5. ITraversalStrategy biases node selection and path weights
    /// 6. IPathfinder finds the graph-level path (which nodes to visit)
    /// 7. TilePathfinder converts graph path into tile-level waypoints through walkable space
    /// 8. TraversalStateMachine orchestrates high-level behavior
    /// 9. Controller moves the character along tile waypoints
    /// </summary>
    [RequireComponent(typeof(PerceptionComponent))]
    public class TraversalAIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TraversalProfile _profile;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _decisionInterval = 1f;

        [Header("Movement")]
        [SerializeField] private float _waypointArrivalDistance = 0.3f;
        [SerializeField] private float _nodeArrivalDistance = 1.5f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private string _currentStateName = "None";
        [SerializeField] private string _currentGoalDescription = "None";
        [SerializeField] private int _currentNodeId = -1;
        [SerializeField] private int _targetNodeId = -1;
        [SerializeField] private float _health = 1f;
        [SerializeField] private float _resources = 1f;
        [SerializeField] private float _inventoryFullness;

        // Core systems
        private TraversalDungeonGraph _dungeonGraph;
        private PerceptionComponent _perception;
        private MemorySystem _memory;
        private GoalGenerator _goalGenerator;
        private GoalEvaluator _goalEvaluator;
        private UtilityScorer _utilityScorer;
        private RuleEvaluator _ruleEvaluator;
        private ITraversalStrategy _strategy;
        private IPathfinder _pathfinder;
        private TraversalStateMachine _stateMachine;
        private InfluenceMap.InfluenceMap _influenceMap;
        private InfluenceMap.InfluenceSampler _influenceSampler;

        // Tile-level pathfinding
        private TilePathfinder _tilePathfinder;

        // Runtime state
        private ITraversalGoal _currentGoal;
        private PathResult _currentPath;
        private List<Vector3> _tileWaypoints;     // Tile-level waypoints for current movement
        private int _waypointIndex;                // Current position in _tileWaypoints
        private float _lastDecisionTime;
        private bool _isInitialized;

        // Public accessors for debug visualization
        public TraversalProfile Profile => _profile;
        public TraversalDungeonGraph DungeonGraph => _dungeonGraph;
        public PerceptionComponent Perception => _perception;
        public MemorySystem Memory => _memory;
        public ITraversalGoal CurrentGoal => _currentGoal;
        public PathResult CurrentPath => _currentPath;
        public List<Vector3> TileWaypoints => _tileWaypoints;
        public int WaypointIndex => _waypointIndex;
        public GoalEvaluator GoalEval => _goalEvaluator;
        public RuleEvaluator RuleEval => _ruleEvaluator;
        public ITraversalStrategy Strategy => _strategy;
        public TraversalStateMachine StateMachine => _stateMachine;
        public InfluenceMap.InfluenceMap InfluenceMapData => _influenceMap;
        public int CurrentNodeId => _currentNodeId;
        public float Health { get => _health; set => _health = Mathf.Clamp01(value); }
        public bool HasTilePathfinder => _tilePathfinder != null;

        /// <summary>
        /// Initialize the AI with a dungeon graph. Call after dungeon generation.
        /// </summary>
        public void Initialize(TraversalDungeonGraph graph, int startNodeId,
            InfluenceMap.InfluenceMap influenceMap = null)
        {
            _dungeonGraph = graph;
            _currentNodeId = startNodeId;
            _perception = GetComponent<PerceptionComponent>();

            // Initialize tile-level pathfinder if spatial map is available
            if (graph.SpatialMap != null)
            {
                _tilePathfinder = new TilePathfinder(graph.SpatialMap);
                UnityEngine.Debug.Log($"[TraversalAI] {gameObject.name}: Tile pathfinder enabled " +
                    $"(map: {graph.SpatialMap.Width}x{graph.SpatialMap.Height})");
            }
            else
            {
                _tilePathfinder = null;
                UnityEngine.Debug.LogWarning($"[TraversalAI] {gameObject.name}: No SpatialMap — " +
                    "tile pathfinding disabled, using direct node-to-node movement.");
            }

            // Initialize subsystems
            _memory = new MemorySystem();
            _goalGenerator = new GoalGenerator();
            _utilityScorer = new UtilityScorer();
            _ruleEvaluator = new RuleEvaluator(cumulativeMode: false);
            _pathfinder = new GraphPathfinder();

            // Apply profile configuration
            if (_profile != null)
            {
                _profile.ApplyStanceModifiers();
                _strategy = TraversalStrategyFactory.Create(_profile.primaryStrategy);
                _perception.PerceptionDepth = _profile.perceptionDepth;

                // Add considerations from profile
                foreach (var consideration in _profile.considerations)
                {
                    if (consideration != null)
                        _utilityScorer.AddConsideration(consideration);
                }
            }
            else
            {
                _strategy = new Strategy.Strategies.UnvisitedPreferenceTraversal();
            }

            _goalEvaluator = new GoalEvaluator(_utilityScorer);

            // Setup influence maps
            _influenceMap = influenceMap ?? new InfluenceMap.InfluenceMap(100, 100);
            _influenceSampler = new InfluenceMap.InfluenceSampler(_influenceMap);

            // Initialize perception
            _perception.Initialize(graph);
            _perception.SetCurrentNode(startNodeId);
            _perception.UpdatePerception();

            // Record initial visit
            _memory.RecordVisit(startNodeId);

            // Setup state machine
            SetupStateMachine();

            // Snap to start node walkable position
            var startNode = _dungeonGraph.GetNode(startNodeId);
            if (startNode != null && _tilePathfinder != null)
            {
                var gridPos = _tilePathfinder.WorldToGrid(startNode.WorldPosition);
                transform.position = _tilePathfinder.GridToWorld(gridPos) + Vector3.up * 0.5f;
            }

            _isInitialized = true;
            UnityEngine.Debug.Log($"[TraversalAI] {gameObject.name} initialized at node {startNodeId} " +
                                  $"with strategy '{_strategy.StrategyName}'");
        }

        private void SetupStateMachine()
        {
            var stateContext = new TraversalStateContext
            {
                Graph = _dungeonGraph,
                PerceivedState = _perception.PerceivedState,
                Memory = _memory,
                CurrentNodeId = _currentNodeId,
                CurrentHealth = _health,
                DangerTolerance = _profile?.dangerTolerance ?? 0.5f,
                RequestMoveTo = (nodeId) => _targetNodeId = nodeId,
                RequestStateChange = (state) => _stateMachine.TransitionTo(state)
            };

            _stateMachine = new TraversalStateMachine(stateContext);
            _stateMachine.RegisterState(TraversalStateType.Exploring, new ExploringState());
            _stateMachine.RegisterState(TraversalStateType.Searching, new SearchingState());
            _stateMachine.RegisterState(TraversalStateType.Descending, new DescendingState());
            _stateMachine.RegisterState(TraversalStateType.Retreating, new RetreatingState());
            _stateMachine.RegisterState(TraversalStateType.AvoidingThreat, new AvoidingThreatState());
            _stateMachine.RegisterState(TraversalStateType.Resting, new RestingState());
            _stateMachine.RegisterState(TraversalStateType.Regrouping, new RegroupingState());
            _stateMachine.SetInitialState(TraversalStateType.Exploring);
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Update debug display
            _currentStateName = _stateMachine?.CurrentStateName ?? "None";
            _currentGoalDescription = _currentGoal?.GetDescription() ?? "None";

            // Decision-making at intervals
            if (Time.time - _lastDecisionTime >= _decisionInterval)
            {
                MakeDecision();
                _lastDecisionTime = Time.time;
            }

            // Movement execution
            ExecuteMovement();

            // Update state machine
            UpdateStateContext();
            _stateMachine.Update();
        }

        private void MakeDecision()
        {
            // 1. Build goal context
            var goalContext = new GoalContext
            {
                DungeonGraph = _dungeonGraph,
                PerceivedState = _perception.PerceivedState,
                Memory = _memory,
                CurrentNodeId = _currentNodeId,
                CurrentHealth = _health,
                CurrentResources = _resources,
                InventoryFullness = _inventoryFullness,
                CurrentTime = Time.time,
                DangerTolerance = _profile?.dangerTolerance ?? 0.5f
            };

            // 2. Generate candidate goals
            var candidates = _goalGenerator.GenerateGoals(goalContext);

            // 3. Evaluate behavior rules
            if (_profile != null && _profile.behaviorRules.Count > 0)
            {
                var behaviorContext = _ruleEvaluator.BuildContext(goalContext, _dungeonGraph);
                var directives = _ruleEvaluator.Evaluate(_profile.behaviorRules, behaviorContext);
                _ruleEvaluator.ApplyDirectives(directives, candidates, goalContext);
            }

            // 4. Apply traversal strategy biases
            var traversalContext = new TraversalContext
            {
                Graph = _dungeonGraph,
                PerceivedState = _perception.PerceivedState,
                Memory = _memory,
                CurrentNodeId = _currentNodeId,
                DangerTolerance = goalContext.DangerTolerance,
                InfluenceSampler = _influenceSampler
            };

            var strategyDecision = _strategy.Evaluate(traversalContext);

            // Bias goals based on strategy decision
            foreach (var candidate in candidates)
            {
                if (candidate is BaseTraversalGoal btg)
                {
                    foreach (var scored in strategyDecision.RankedCandidates)
                    {
                        if (scored.NodeId == btg.TargetNodeId)
                        {
                            btg.BasePriority *= (1f + scored.Score * 0.5f);
                            break;
                        }
                    }
                }
            }

            // 5. Evaluate and select best goal
            var bestGoal = _goalEvaluator.EvaluateBest(candidates, goalContext, _influenceSampler);

            if (bestGoal != null && (
                _currentGoal == null ||
                _currentGoal.IsComplete(goalContext) ||
                !_currentGoal.IsValid(goalContext) ||
                bestGoal.BasePriority > _currentGoal.BasePriority * 1.3f)) // Hysteresis
            {
                _currentGoal = bestGoal;
                _targetNodeId = bestGoal.TargetNodeId;

                // 6. Find graph-level path to target
                if (_targetNodeId >= 0 && _targetNodeId != _currentNodeId)
                {
                    var pathRequest = new PathRequest
                    {
                        StartNodeId = _currentNodeId,
                        EndNodeId = _targetNodeId,
                        AvoidDanger = _profile?.dangerAvoidanceWeight > 0.5f,
                        MaxAcceptableDanger = _profile?.dangerTolerance ?? 0.7f,
                        EdgeWeightModifier = (edge) => _strategy.GetEdgeWeightBias(edge, traversalContext)
                    };

                    _currentPath = _pathfinder.FindPath(pathRequest, _dungeonGraph);

                    // 7. Convert graph path to tile-level waypoints
                    ComputeTileWaypoints();
                }
            }
        }

        /// <summary>
        /// Converts the current graph-level node path into detailed tile-level waypoints
        /// that navigate through walkable tiles (rooms, corridors, doors).
        /// Falls back to direct node positions if no tile pathfinder is available.
        /// </summary>
        private void ComputeTileWaypoints()
        {
            _tileWaypoints = null;
            _waypointIndex = 0;

            if (_currentPath == null || !_currentPath.Success || _currentPath.NodePath.Count < 2)
                return;

            if (_tilePathfinder != null)
            {
                // Gather world positions for each node in the graph path
                var nodePositions = new List<Vector3>();
                // Start from current actual position instead of node center
                nodePositions.Add(new Vector3(transform.position.x, 0f, transform.position.z));

                for (int i = 1; i < _currentPath.NodePath.Count; i++)
                {
                    var node = _dungeonGraph.GetNode(_currentPath.NodePath[i]);
                    if (node != null)
                        nodePositions.Add(node.WorldPosition);
                }

                // Compute tile-level path through walkable space
                _tileWaypoints = _tilePathfinder.FindTilePathForNodeSequence(nodePositions);

                if (_tileWaypoints != null && _tileWaypoints.Count > 0)
                {
                    // Add Y offset for the agent
                    for (int i = 0; i < _tileWaypoints.Count; i++)
                    {
                        _tileWaypoints[i] = new Vector3(
                            _tileWaypoints[i].x,
                            0.5f,
                            _tileWaypoints[i].z
                        );
                    }
                }
            }
            else
            {
                // Fallback: use node centers directly (old behavior)
                _tileWaypoints = new List<Vector3>();
                for (int i = 0; i < _currentPath.NodePath.Count; i++)
                {
                    var node = _dungeonGraph.GetNode(_currentPath.NodePath[i]);
                    if (node != null)
                        _tileWaypoints.Add(node.WorldPosition + Vector3.up * 0.5f);
                }
            }
        }

        private void ExecuteMovement()
        {
            if (_tileWaypoints == null || _tileWaypoints.Count == 0 || _waypointIndex >= _tileWaypoints.Count)
                return;

            // Target the current waypoint
            Vector3 targetPos = _tileWaypoints[_waypointIndex];
            Vector3 direction = targetPos - transform.position;
            direction.y = 0; // Keep on ground plane

            float distToWaypoint = direction.magnitude;

            if (distToWaypoint < _waypointArrivalDistance)
            {
                // Arrived at this waypoint, advance to next
                _waypointIndex++;

                // Check if we've entered a new graph node's area
                UpdateCurrentNodeFromPosition();

                // Check if we've completed the entire path
                if (_waypointIndex >= _tileWaypoints.Count)
                {
                    OnPathCompleted();
                }
            }
            else
            {
                // Move towards current waypoint
                Vector3 moveDir = direction.normalized;
                transform.position += moveDir * _moveSpeed * Time.deltaTime;

                if (direction.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(moveDir);
            }
        }

        /// <summary>
        /// Determine which graph node the agent is currently in based on world position.
        /// Updates _currentNodeId when the agent enters a new node's bounds.
        /// </summary>
        private void UpdateCurrentNodeFromPosition()
        {
            Vector3 pos = transform.position;

            // Check if we're now closer to a different node
            float bestDist = float.MaxValue;
            int bestNodeId = _currentNodeId;

            // Only check nodes on the current path for efficiency
            if (_currentPath != null && _currentPath.NodePath != null)
            {
                foreach (int nodeId in _currentPath.NodePath)
                {
                    var node = _dungeonGraph.GetNode(nodeId);
                    if (node == null) continue;

                    float dist = Vector3.Distance(
                        new Vector3(pos.x, 0, pos.z),
                        node.WorldPosition
                    );

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestNodeId = nodeId;
                    }
                }
            }

            if (bestNodeId != _currentNodeId && bestDist < _nodeArrivalDistance)
            {
                OnEnteredNode(bestNodeId);
            }
        }

        /// <summary>
        /// Called when the agent enters a new graph node's area.
        /// </summary>
        private void OnEnteredNode(int nodeId)
        {
            _currentNodeId = nodeId;

            // Update perception
            _perception.SetCurrentNode(_currentNodeId);

            // Record visit in memory
            if (!_memory.HasVisited(_currentNodeId))
            {
                _memory.RecordVisit(_currentNodeId);

                // Mark node as explored in the graph
                var node = _dungeonGraph.GetNode(_currentNodeId);
                if (node != null)
                {
                    node.RemoveTag(NodeTag.Unexplored);
                    node.AddTag(NodeTag.Explored);
                }

                // Update influence maps
                var nodeData = _dungeonGraph.GetNode(_currentNodeId);
                if (nodeData != null)
                {
                    _influenceMap.AddInfluence(
                        InfluenceMap.InfluenceLayerType.ExplorationCuriosity,
                        nodeData.WorldPosition, -0.3f);

                    _influenceMap.AddInfluence(
                        InfluenceMap.InfluenceLayerType.Safety,
                        nodeData.WorldPosition, 0.2f);
                }
            }
            else
            {
                _memory.RecordVisit(_currentNodeId);
            }
        }

        /// <summary>
        /// Called when the agent completes the entire tile waypoint path.
        /// </summary>
        private void OnPathCompleted()
        {
            // Ensure we register arrival at the target node
            if (_targetNodeId >= 0 && _currentNodeId != _targetNodeId)
            {
                OnEnteredNode(_targetNodeId);
            }

            _tileWaypoints = null;
            _currentPath = null;
        }

        private void UpdateStateContext()
        {
            if (_stateMachine == null) return;
        }

        /// <summary>Change the AI's traversal profile at runtime.</summary>
        public void SetProfile(TraversalProfile profile)
        {
            _profile = profile;
            if (profile != null && _isInitialized)
            {
                profile.ApplyStanceModifiers();
                _strategy = TraversalStrategyFactory.Create(profile.primaryStrategy);
                _perception.PerceptionDepth = profile.perceptionDepth;
                UnityEngine.Debug.Log($"[TraversalAI] {gameObject.name} profile changed to '{profile.profileName}'");
            }
        }

        /// <summary>Change traversal strategy at runtime.</summary>
        public void SetStrategy(TraversalStrategyType type)
        {
            _strategy = TraversalStrategyFactory.Create(type);
            UnityEngine.Debug.Log($"[TraversalAI] {gameObject.name} strategy changed to '{_strategy.StrategyName}'");
        }
    }
}

