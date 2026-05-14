# Dungeon Traversal AI System

## Architecture Overview

A modular, extensible AI framework for autonomous dungeon exploration, inspired by Pillars of Eternity II: Deadfire's configurable AI system. The player configures **how** AI adventurers explore, and the system autonomously handles goal selection, pathfinding, and traversal.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    TraversalAIController (Orchestrator)             │
│                                                                     │
│  ┌──────────────┐   ┌──────────────┐   ┌─────────────────────────┐ │
│  │  Perception   │──▶│    Goals     │──▶│    Utility AI Scorer    │ │
│  │  Component    │   │  Generator   │   │   + Considerations      │ │
│  └──────┬───────┘   └──────────────┘   └──────────┬──────────────┘ │
│         │                                          │                │
│  ┌──────▼───────┐   ┌──────────────┐   ┌──────────▼──────────────┐ │
│  │   Memory     │   │  Behavior    │──▶│   Goal Evaluator        │ │
│  │   System     │   │  Rules       │   │   (selects best goal)   │ │
│  └──────────────┘   └──────────────┘   └──────────┬──────────────┘ │
│                                                    │                │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────▼──────────────┐ │
│  │  Influence   │──▶│  Traversal   │──▶│    Pathfinder (A*)      │ │
│  │  Maps        │   │  Strategy    │   │   danger-aware          │ │
│  └──────────────┘   └──────────────┘   └──────────┬──────────────┘ │
│                                                    │                │
│  ┌──────────────────────────────────────────────────▼──────────────┐ │
│  │              State Machine (orchestrates high-level behavior)   │ │
│  │  Exploring │ Searching │ Descending │ Retreating │ Resting     │ │
│  └────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

## Decision Flow (per evaluation cycle)

1. **Perception** scans the dungeon graph within perception depth (BFS)
2. **Goals** are generated from the perceived state (explore, loot, retreat, etc.)
3. **Behavior Rules** (player-configured) modify goal priorities
4. **Traversal Strategy** biases node selection based on exploration heuristic
5. **Utility Scorer** evaluates each goal with weighted considerations
6. **Goal Evaluator** selects the highest-scoring valid goal
7. **Pathfinder** finds a danger-aware path to the goal target
8. **State Machine** orchestrates high-level behavior transitions
9. **Controller** moves the character along the computed path

---

## Directory Structure

```
Assets/Scripts/TraversalAI/
├── Bridge/                     # DungeonGenerator integration
│   └── DungeonTraversalBridge.cs  # Converts generated dungeon → AI graph
│
├── Core/                       # Dungeon graph abstraction
│   ├── NodeTag.cs             # Flags enum for node classification
│   ├── DungeonNode.cs         # AI-facing node representation
│   ├── DungeonEdge.cs         # AI-facing edge representation
│   └── TraversalDungeonGraph.cs  # Graph with adjacency lookup
│
├── Perception/                 # Fog of war & memory
│   ├── VisibilityState.cs     # Unknown/Inferred/Remembered/Visible
│   ├── PerceivedNodeInfo.cs   # What the AI believes about a node
│   ├── PerceivedDungeonState.cs # Subjective dungeon model
│   ├── PerceptionComponent.cs # MonoBehaviour perception scanner
│   └── MemorySystem.cs        # Visit history & event tracking
│
├── Goals/                      # Goal generation
│   ├── ITraversalGoal.cs      # Goal interface
│   ├── BaseTraversalGoal.cs   # Abstract base
│   ├── GoalContext.cs         # Shared context for goals
│   ├── ConcreteGoals.cs       # All goal implementations
│   └── GoalGenerator.cs      # Pluggable goal providers
│
├── UtilityAI/                  # Utility-based scoring
│   ├── UtilityConsideration.cs # ScriptableObject scoring curves
│   ├── UtilityScorer.cs       # Multiplicative utility scoring
│   └── GoalEvaluator.cs      # Scores goals, selects best
│
├── Strategy/                   # Traversal heuristics
│   ├── ITraversalStrategy.cs  # Strategy interface
│   ├── TraversalContext.cs    # Strategy context data
│   ├── TraversalDecision.cs   # Ranked candidates result
│   └── Strategies/            # 9 strategy implementations
│       ├── DepthFirstTraversal.cs
│       ├── BreadthFirstTraversal.cs
│       ├── WallFollowTraversal.cs
│       ├── SpiralTraversal.cs
│       ├── HubAndSpokeTraversal.cs
│       ├── SectorSweepTraversal.cs
│       ├── UnvisitedPreferenceTraversal.cs
│       ├── SafeRadiusTraversal.cs
│       └── StraightBiasTraversal.cs
│
├── InfluenceMap/               # Layered influence maps
│   ├── InfluenceLayerType.cs  # Danger/Curiosity/Loot/Safety/Ally/Noise
│   ├── InfluenceLayer.cs      # Grid layer with propagation/decay
│   ├── InfluenceMap.cs        # Multi-layer container
│   └── InfluenceSampler.cs    # Query interface
│
├── BehaviorRules/              # Player-configurable rules
│   ├── BehaviorContext.cs     # Rule evaluation context
│   ├── Condition.cs           # IF conditions with operators
│   ├── ActionDirective.cs     # THEN actions (boost/suppress/force)
│   ├── BehaviorRule.cs        # IF-THEN rule structure
│   └── RuleEvaluator.cs      # Evaluates & applies rules
│
├── StateMachine/               # High-level AI states
│   ├── ITraversalState.cs     # State interface + enum
│   ├── TraversalStateContext.cs # Shared state context
│   ├── TraversalStateMachine.cs # FSM manager
│   └── States/
│       └── ConcreteStates.cs  # All 7 state implementations
│
├── Pathfinding/                # Pathfinding abstraction
│   ├── IPathfinder.cs         # Interface + PathRequest/PathResult
│   └── GraphPathfinder.cs     # A* with danger awareness
│
├── Configuration/              # Data-driven configuration
│   ├── TraversalProfile.cs    # ScriptableObject profile + factory
│   └── TraversalPresets.cs    # Diver/Hunter/Scavenger/Cartographer/Ghost
│
├── Debug/                      # Visualization
│   ├── TraversalDebugVisualizer.cs  # Gizmos + GUI overlay
│   └── InfluenceMapVisualizer.cs    # Influence map rendering
│
├── Editor/                     # Custom inspectors
│   ├── TraversalProfileEditor.cs
│   └── TraversalAIControllerEditor.cs
│
├── Demo/
│   └── TraversalAIDemoScene.cs  # Test scene bootstrapper
│
└── TraversalAIController.cs    # Main controller MonoBehaviour
```

## Exploration Stances

| Stance | Strategy | Danger Tolerance | Behavior |
|--------|----------|-----------------|----------|
| **Cautious** | BFS tendencies | 0.2 | Safe routes, avoids danger |
| **Aggressive** | DFS tendencies | 0.8 | Deep penetration, accepts risk |
| **Thorough** | Sector Sweep | 0.5 | Complete map coverage |
| **Efficient** | Straight Bias | 0.5 | Minimal backtracking |
| **Sneaky** | Safe Radius | 0.3 | Low visibility routes |

## Presets

| Preset | Stance | Strategy | Priority |
|--------|--------|----------|----------|
| **Diver** | Aggressive | Depth-First | Rush to stairs |
| **Hunter** | Aggressive | Depth-First | Seek enemies |
| **Scavenger** | Thorough | Breadth-First | Maximize loot |
| **Cartographer** | Thorough | Sector Sweep | Map everything |
| **Ghost** | Sneaky | Safe Radius | Avoid all danger |

## Quick Start

1. Create a `TraversalProfile` asset: **Assets → Create → Traversal AI → Traversal Profile**
2. Configure the profile in the Inspector (or use a preset button)
3. Add `TraversalAIController` to a GameObject
4. Assign the profile
5. Call `controller.Initialize(graph, startNodeId)` after dungeon generation
6. Add `TraversalDebugVisualizer` for scene view visualization

### Demo Scene

Add the `TraversalAIDemoScene` component to an empty GameObject and press Play.
It creates a 15-node test dungeon with branching paths, dead ends, enemy zones,
treasure rooms, and stairs, then spawns 4 AI explorers with different profiles.

## Design Principles

- **Composition over inheritance**: Systems are composed, not inherited
- **Data-driven**: ScriptableObjects for all configuration
- **Clean interfaces**: `ITraversalGoal`, `ITraversalStrategy`, `IPathfinder`, `ITraversalState`
- **Separation of concerns**: Perception ≠ Decision ≠ Movement ≠ Pathfinding
- **No omniscience**: AI only uses `PerceivedDungeonState`, never raw graph
- **Extensible**: Plug in new goals, strategies, rules, considerations via interfaces
- **Save/load ready**: All state classes provide `ToSaveData()`/`LoadFromSaveData()`
- **ECS-ready**: Data-oriented node/edge structures, stateless strategies
- **Multiplayer-ready**: Per-agent perception and memory, shared influence maps

## Extending the System

### Adding a new goal type:
1. Create a class extending `BaseTraversalGoal`
2. Create an `IGoalProvider` that generates it
3. Register with `GoalGenerator.RegisterProvider()`

### Adding a new traversal strategy:
1. Implement `ITraversalStrategy`
2. Add to `TraversalStrategyType` enum
3. Add to `TraversalStrategyFactory.Create()`

### Adding a new behavior condition:
1. Add to `ConditionParameter` enum
2. Handle in `Condition.GetParameterValue()`
3. Provide the value in `BehaviorContext`

### Adding a new influence layer:
1. Add to `InfluenceLayerType` enum
2. Layers are auto-created by `InfluenceMap` constructor

---

## Integration with DungeonGenerator

The TraversalAI system integrates with the existing `DungeonGenerator` via the **`DungeonTraversalBridge`** component.

### Setup (Recommended)

1. Add `DungeonTraversalBridge` to the same GameObject as `DungeonGenerator`
2. Assign `TraversalProfile` assets to the explorer profiles list  
3. Configure auto-spawn settings
4. **Done!** The bridge auto-initializes when the generator fires `OnDungeonGenerated`

### Setup (Demo Scene)

1. Add `TraversalAIDemoScene` to any GameObject in a scene with a `DungeonGenerator`
2. It auto-discovers the generator and sets up the bridge
3. If no generator exists, falls back to a built-in test dungeon

### How it works

```
DungeonGenerator                  DungeonTraversalBridge
     │                                    │
     │ Generate()                         │
     │──── GenerationContext ────────────▶│
     │     (DungeonGraph + SpatialMap)    │ BuildFromGenerationData()
     │                                    │──▶ TraversalDungeonGraph
     │                                    │
     │ OnDungeonGenerated event ─────────▶│ InitializeFromContext()
     │                                    │──▶ Seed InfluenceMaps
     │                                    │──▶ Enrich from Encounters
     │                                    │──▶ SpawnExplorers()
     │                                    │    └── TraversalAIController[]
```

### Programmatic usage

```csharp
// Option 1: Let the bridge listen to generator events
var bridge = dungeonGenerator.gameObject.AddComponent<DungeonTraversalBridge>();
// Bridge auto-initializes on next Generate()

// Option 2: Manual initialization
var context = dungeonGenerator.Generate();
bridge.InitializeFromContext(context);

// Option 3: Generate + initialize in one call
bridge.GenerateAndInitialize();

// Option 4: Spawn individual explorers at runtime
var profile = TraversalPresets.CreateGhost();
bridge.SpawnExplorer(profile, startNodeId: 0);
```
