# Autonomous Melee Combat System

## Architecture Overview

A modular, extensible autonomous melee combat system designed to integrate with the existing Dungeon Traversal AI framework. Combat emerges from spatial relationships, positioning, formations, and tactical commitment — NOT cooldown rotations or DPS trading.

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         CombatAgent (Core Orchestrator)                        │
│                                                                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  CombatBrain │  │ SkillExecutor│  │  FacingComp  │  │ EngagementTracker│  │
│  │  (Utility AI)│  │ (Pipeline)   │  │ (Direction)  │  │ (Melee Zones)    │  │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘  └──────────────────┘  │
│         │                  │                                                   │
│  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ CombatUtility│  │ SkillEffects │  │  Formation   │  │  ThreatTracker   │  │
│  │ Scorer       │  │ (SO chain)   │  │  Controller  │  │  (Soft aggro)    │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────────┘  │
│                                                                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  CombatRules │  │   Spatial    │  │   Terrain    │  │  CombatMovement  │  │
│  │  (IF/THEN)   │  │   System     │  │  Evaluator   │  │  Controller      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Decision Flow (per evaluation cycle)

1. **CombatContext** is rebuilt from spatial queries, engagement state, and terrain data
2. **CombatBrain** iterates all equipped skills
3. **CombatUtilityScorer** scores each skill × target combination using:
   - Skill's `UtilityProfile` (situational preferences)
   - `CombatDoctrineSO` (personality multipliers)
   - Terrain bonuses
   - Commitment/Risk penalties
4. Highest-scoring skill is selected
5. **CombatMovementController** positions agent for execution
6. **SkillExecutor** runs skill through Windup → Active → Recovery pipeline
7. **SkillEffects** resolve hits with directional damage/displacement/CC

---

## Directory Structure

```
Assets/Scripts/Combat/
├── Core/
│   ├── CombatEnums.cs              # All shared enumerations
│   ├── CombatEvents.cs             # Static event bus (decoupled communication)
│   ├── CombatAgent.cs              # Core MonoBehaviour (orchestrator)
│   ├── CombatAgentRegistry.cs      # Global agent lookup for spatial queries
│   ├── CombatBrain.cs              # Utility-based autonomous decision making
│   ├── CombatContext.cs            # Tactical situation snapshot
│   └── CombatMemory.cs            # Combat history tracking
│
├── Skills/
│   ├── CombatSkillSO.cs           # Core ScriptableObject skill definition
│   ├── SkillExecutor.cs           # Windup → Active → Recovery pipeline
│   ├── Effects/
│   │   └── SkillEffects.cs        # Damage, Displacement, Stun, etc.
│   ├── Conditions/
│   │   └── SkillConditions.cs     # Behind target, Near wall, etc.
│   └── Definitions/
│       └── SkillFactory.cs        # 30+ example skills (programmatic creation)
│
├── AI/
│   ├── CombatDoctrineSO.cs        # Combat personality/strategy SO
│   ├── Utility/
│   │   └── CombatUtilityScorer.cs # Skill utility evaluation
│   └── Rules/
│       └── CombatRules.cs         # IF/THEN tactical rule system
│
├── Engagement/
│   └── EngagementSystem.cs        # Melee engagement tracking & zones
│
├── Facing/
│   └── FacingSystem.cs            # Direction, flank detection, hit resolution
│
├── Formation/
│   └── FormationSystem.cs         # Autonomous formation controller
│
├── Spatial/
│   └── SpatialCombatSystem.cs     # Body blocking, crowd density, chokepoints
│
├── Terrain/
│   └── TerrainCombatSystem.cs     # Wall slams, cliff pushes, corridor bonuses
│
├── Stamina/
│   └── StaminaComponent.cs        # Exhaustion and commitment mechanics
│
├── Movement/
│   └── CombatMovementController.cs # Flanking, repositioning, retreat
│
├── Threat/
│   └── ThreatSystem.cs            # Soft threat from proximity & damage
│
├── Weapons/
│   └── WeaponProfileSO.cs        # Weapon identity and behavioral effects
│
├── Animation/
│   └── CombatAnimator.cs         # Decoupled animation interface
│
├── Debug/
│   └── CombatDebugVisualizer.cs   # Runtime gizmos for all systems
│
├── Configuration/
│   └── CombatProfileSO.cs        # Master combat profile + presets
│
└── CombatDemoScene.cs             # Test scene bootstrapper
```

---

## Skill Categories (30+ examples)

| Category | Skills | Key Property |
|----------|--------|-------------|
| **Attack Geometry** | Thrust, Wide Sweep, Overhead Smash, Spin Attack, Advancing Strike | Shape determines AoE |
| **Movement Attack** | Lunge, Leap Attack, Retreat Slash, Sidestep Counter, Charge | Moves agent physically |
| **Positional** | Backstab, Flank Strike, Wall Slam, Cliff Push, Formation Splitter | Requires positioning |
| **Tempo** | Interrupt, Riposte, Guard Break, Delayed Strike, Execute | Manipulates timing |
| **Defensive** | Shield Wall, Brace, Intercept, Zone Hold, Defensive Stance | Protects/holds |
| **Crowd Manipulation** | Knockback, Pull, Trip, Sweep Leg, Fear Strike | Moves enemies |
| **Commitment** | Charged Attack, Berserk Rush, Reckless Cleave, Duel Lock, Momentum Combo | Risk/reward |

---

## Weapon Archetypes

| Weapon | Reach | Speed | Behavior |
|--------|-------|-------|----------|
| **Sword** | 1.5m | Normal | Balanced, versatile |
| **Dagger** | 1.0m | Fast | Seeks flanks, tight spaces |
| **Spear** | 3.0m | Normal | Maintains distance, attacks over allies |
| **Polearm** | 3.5m | Slow | Controls corridors, max reach |
| **Axe** | 1.5m | Slow | High damage, commitment-heavy |
| **Hammer** | 1.8m | Slow | Seeks clusters, displacement |
| **Shield** | 1.0m | Normal | Holds chokepoints, blocks |
| **Greatsword** | 2.2m | Slow | Wide sweeps, tempo control |

---

## Combat Doctrines (AI Personalities)

| Doctrine | Style | Preferred Skills |
|----------|-------|-----------------|
| **Berserker** | Reckless aggression | Commitment, Attack |
| **Guardian** | Defensive bulwark | Defensive, Crowd Control |
| **Flanker** | Mobile positioning | Positional, Movement |
| **Controller** | Space manipulation | Crowd, Tempo |
| **Duelist** | Precision timing | Tempo, Positional |

---

## Integration with Traversal AI

Combat integrates with the existing `TraversalAIController` via:

1. **State Machine Extension**: Add `Fighting` state to `TraversalStateType`
2. **Movement Override**: `CombatMovementController` takes over movement when `CombatState != Idle`
3. **Influence Maps**: Combat generates danger/noise influence
4. **Shared Pathfinding**: Combat retreat uses the same `TilePathfinder`
5. **Perception Extension**: Combat awareness extends perception scanning

```csharp
// Integration example:
// When TraversalAI detects enemies via perception, transition to combat:
if (perceiver.DetectsHostiles && agent.GetComponent<CombatAgent>())
{
    agent.GetComponent<CombatAgent>().SetCombatState(CombatState.Alert);
    stateMachine.TransitionTo(TraversalStateType.Fighting);
}
```

---

## Quick Start

1. Add `CombatDemoScene` component to an empty GameObject
2. Press Play
3. Watch two AI parties fight with different doctrines
4. Use Scene view to see debug gizmos (engagement zones, facing arcs, attack areas)

### Creating a Custom Skill (No Code Required)

1. **Assets → Create → Combat → Skill**
2. Set targeting profile (shape, range, arc)
3. Add effects (damage, displacement, stun)
4. Add conditions (behind target, in corridor, etc.)
5. Configure utility profile (when should AI use this?)
6. Set timing (windup, active, recovery)
7. Equip on a CombatAgent

---

## Design Principles

- **Composition over Inheritance**: Systems are independent, composed on GameObjects
- **Data-Driven**: ScriptableObjects for all combat configuration
- **Event-Driven**: `CombatEvents` static bus for decoupled communication
- **Modularity**: Each system works independently, integrates via events
- **ECS-Ready**: Data-oriented structures, stateless evaluators
- **Save/Load Ready**: Serializable state, no closures in persistent data
- **Multiplayer Ready**: Per-agent state, deterministic evaluation
- **No Animation Coupling**: Combat works without animations (visualization optional)

---

## Emergent Behaviors

The system naturally produces:
- ✅ Frontline formation holding
- ✅ Intelligent flanking by dagger-wielders
- ✅ Chokepoint defense by shield users
- ✅ Crowd splitting by charging agents
- ✅ Tactical retreats when stamina is low
- ✅ Ally protection via intercept skills
- ✅ Overextension punishment (exhaustion)
- ✅ Weapon-appropriate spacing
- ✅ Terrain exploitation (wall slams, corridor thrusts)
- ✅ Target switching based on threat

---

## Extending the System

### Adding a New Skill Effect:
1. Create class extending `SkillEffectSO`
2. Override `Apply(SkillEffectContext ctx)`
3. Add `[CreateAssetMenu]` attribute
4. Create asset in editor, assign to skill

### Adding a New Condition:
1. Create class extending `SkillConditionSO`
2. Override `Evaluate(CombatContext ctx)`
3. Create asset, assign to skill conditions list

### Adding a New Weapon Type:
1. Add to `WeaponType` enum
2. Create `WeaponProfileSO` asset with properties
3. Existing utility scoring automatically adapts

### Adding a New Doctrine:
1. Create `CombatDoctrineSO` asset
2. Configure category multipliers and biases
3. Assign to `CombatBrain` via profile

