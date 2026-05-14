// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Demo Scene Bootstrapper
// =============================================================================
// Creates a test combat scenario demonstrating all combat systems.
// Spawns two parties with different combat doctrines in a dungeon-like arena.
// =============================================================================

using System.Collections.Generic;
using Combat.AI;
using Combat.Configuration;
using Combat.Core;
using Combat.Debug;
using Combat.Formation;
using Combat.Movement;
using Combat.Skills;
using Combat.Skills.Definitions;
using Combat.Spatial;
using Combat.Weapons;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Demo scene bootstrapper. Creates a combat test arena with two AI parties.
    /// Add to an empty GameObject and press Play.
    /// 
    /// Demonstrates:
    /// - Different weapon types and their behavioral effects
    /// - Formation holding in corridors
    /// - Flanking and backstab AI
    /// - Crowd manipulation and displacement
    /// - Defensive chokepoint holding
    /// - Commitment and exhaustion dynamics
    /// </summary>
    public class CombatDemoScene : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private int _partySize = 4;
        [SerializeField] private float _arenaWidth = 20f;
        [SerializeField] private float _arenaLength = 30f;
        [SerializeField] private bool _createCorridorArena = true;
        [SerializeField] private bool _spawnDebugVisualizers = true;

        [Header("Spawned References (Read Only)")]
        [SerializeField] private List<CombatAgent> _partyA = new List<CombatAgent>();
        [SerializeField] private List<CombatAgent> _partyB = new List<CombatAgent>();

        private FormationController _formationA;
        private FormationController _formationB;

        private void Start()
        {
            CreateArena();
            SpawnSpatialSystem();
            SpawnParties();
            SetupFormations();

            UnityEngine.Debug.Log("[CombatDemo] Demo scene initialized. " +
                $"Party A: {_partyA.Count} agents, Party B: {_partyB.Count} agents.");
        }

        private void CreateArena()
        {
            if (_createCorridorArena)
            {
                // Floor
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Arena_Floor";
                floor.transform.localScale = new Vector3(_arenaWidth / 10f, 1, _arenaLength / 10f);
                floor.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);

                // Walls (create corridor-like environment)
                CreateWall("Wall_Left", new Vector3(-_arenaWidth / 2, 1.5f, 0), new Vector3(0.5f, 3f, _arenaLength));
                CreateWall("Wall_Right", new Vector3(_arenaWidth / 2, 1.5f, 0), new Vector3(0.5f, 3f, _arenaLength));
                CreateWall("Wall_Back", new Vector3(0, 1.5f, -_arenaLength / 2), new Vector3(_arenaWidth, 3f, 0.5f));
                CreateWall("Wall_Front", new Vector3(0, 1.5f, _arenaLength / 2), new Vector3(_arenaWidth, 3f, 0.5f));

                // Corridor narrowing (chokepoint)
                CreateWall("Choke_Left", new Vector3(-3f, 1.5f, 0), new Vector3(0.5f, 3f, 6f));
                CreateWall("Choke_Right", new Vector3(3f, 1.5f, 0), new Vector3(0.5f, 3f, 6f));

                // Some cover elements
                CreateWall("Pillar_1", new Vector3(-5f, 1f, 5f), new Vector3(1f, 2f, 1f));
                CreateWall("Pillar_2", new Vector3(5f, 1f, -5f), new Vector3(1f, 2f, 1f));
            }
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.layer = LayerMask.NameToLayer("Default");
            wall.GetComponent<Renderer>().material.color = new Color(0.5f, 0.45f, 0.4f);
            wall.isStatic = true;
        }

        private void SpawnSpatialSystem()
        {
            var spatialGO = new GameObject("SpatialCombatSystem");
            var spatial = spatialGO.AddComponent<SpatialCombatSystem>();
            // Note: LayerMask setup would be done via inspector in production
        }

        private void SpawnParties()
        {
            // Party A (Player party) - diverse roles
            var profiles = new[]
            {
                ("Guardian", Faction.PlayerParty, FormationRole.Frontline, WeaponPresets.CreateSword(), CombatDoctrinePresets.CreateGuardian()),
                ("Flanker", Faction.PlayerParty, FormationRole.Flank, WeaponPresets.CreateDagger(), CombatDoctrinePresets.CreateFlanker()),
                ("Warden", Faction.PlayerParty, FormationRole.Backline, WeaponPresets.CreateSpear(), CombatDoctrinePresets.CreateController()),
                ("Berserker", Faction.PlayerParty, FormationRole.Vanguard, WeaponPresets.CreateHammer(), CombatDoctrinePresets.CreateBerserker()),
            };

            Vector3 baseA = new Vector3(0, 0.5f, -8f);
            for (int i = 0; i < Mathf.Min(_partySize, profiles.Length); i++)
            {
                var (name, faction, role, weapon, doctrine) = profiles[i];
                Vector3 offset = new Vector3((i - _partySize * 0.5f + 0.5f) * 2f, 0, 0);
                var agent = SpawnAgent(name, faction, baseA + offset, weapon, doctrine, role);
                _partyA.Add(agent);
            }

            // Party B (Enemy) - uniform aggressive warriors
            var enemyProfiles = new[]
            {
                ("Enemy_Sword", Faction.EnemyA, FormationRole.Frontline, WeaponPresets.CreateSword(), CombatDoctrinePresets.CreateDuelist()),
                ("Enemy_Axe", Faction.EnemyA, FormationRole.Frontline, WeaponPresets.CreateAxe(), CombatDoctrinePresets.CreateBerserker()),
                ("Enemy_Spear", Faction.EnemyA, FormationRole.Backline, WeaponPresets.CreateSpear(), CombatDoctrinePresets.CreateController()),
                ("Enemy_Shield", Faction.EnemyA, FormationRole.Frontline, WeaponPresets.CreateShield(), CombatDoctrinePresets.CreateGuardian()),
            };

            Vector3 baseB = new Vector3(0, 0.5f, 8f);
            for (int i = 0; i < Mathf.Min(_partySize, enemyProfiles.Length); i++)
            {
                var (name, faction, role, weapon, doctrine) = enemyProfiles[i];
                Vector3 offset = new Vector3((i - _partySize * 0.5f + 0.5f) * 2f, 0, 0);
                var agent = SpawnAgent(name, faction, baseB + offset, weapon, doctrine, role);
                _partyB.Add(agent);
            }
        }

        private CombatAgent SpawnAgent(string agentName, Faction faction, Vector3 position,
            WeaponProfileSO weapon, CombatDoctrineSO doctrine, FormationRole role)
        {
            // Create visual representation
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Combat_{agentName}";
            go.transform.position = position;

            // Color by faction
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = faction == Faction.PlayerParty
                ? new Color(0.2f, 0.5f, 0.9f) : new Color(0.9f, 0.3f, 0.2f);

            // Remove default collider (we don't want physics)
            var collider = go.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            // Add combat components
            var agent = go.AddComponent<CombatAgent>();

            // Set private fields via reflection for demo
            SetPrivateField(agent, "_faction", faction);
            SetPrivateField(agent, "_agentName", agentName);
            SetPrivateField(agent, "_equippedWeapon", weapon);
            SetPrivateField(agent, "_maxHealth", 100f);
            SetPrivateField(agent, "_currentHealth", 100f);
            SetPrivateField(agent, "_maxStamina", 100f);
            SetPrivateField(agent, "_currentStamina", 100f);

            // Combat movement
            go.AddComponent<CombatMovementController>();

            // Equip skills based on weapon type
            EquipSkillsForWeapon(agent, weapon.WeaponType);

            // Set doctrine
            if (agent.Brain != null)
                agent.Brain.SetDoctrine(doctrine);

            // Formation role
            agent.SetFormationRole(role);

            // Debug visualizer
            if (_spawnDebugVisualizers)
                go.AddComponent<CombatDebugVisualizer>();

            // Start in alert state
            agent.SetCombatState(CombatState.Alert);

            return agent;
        }

        private void EquipSkillsForWeapon(CombatAgent agent, WeaponType type)
        {
            // Base skills everyone gets
            agent.EquipSkill(SkillFactory.CreateAdvancingStrike());
            agent.EquipSkill(SkillFactory.CreateRetreatSlash());

            // Weapon-specific skills
            switch (type)
            {
                case WeaponType.Sword:
                    agent.EquipSkill(SkillFactory.CreateWideSweep());
                    agent.EquipSkill(SkillFactory.CreateRiposte());
                    agent.EquipSkill(SkillFactory.CreateThrust());
                    break;

                case WeaponType.Dagger:
                    agent.EquipSkill(SkillFactory.CreateBackstab());
                    agent.EquipSkill(SkillFactory.CreateFlankStrike());
                    agent.EquipSkill(SkillFactory.CreateSidestepCounter());
                    agent.EquipSkill(SkillFactory.CreateMomentumCombo());
                    break;

                case WeaponType.Spear:
                    agent.EquipSkill(SkillFactory.CreateThrust());
                    agent.EquipSkill(SkillFactory.CreateLunge());
                    agent.EquipSkill(SkillFactory.CreateKnockback());
                    agent.EquipSkill(SkillFactory.CreateZoneHold());
                    break;

                case WeaponType.Hammer:
                    agent.EquipSkill(SkillFactory.CreateOverheadSmash());
                    agent.EquipSkill(SkillFactory.CreateChargeAttack());
                    agent.EquipSkill(SkillFactory.CreateRecklessCleave());
                    agent.EquipSkill(SkillFactory.CreateKnockback());
                    break;

                case WeaponType.Shield:
                    agent.EquipSkill(SkillFactory.CreateShieldWall());
                    agent.EquipSkill(SkillFactory.CreateBrace());
                    agent.EquipSkill(SkillFactory.CreateIntercept());
                    agent.EquipSkill(SkillFactory.CreateKnockback());
                    break;

                case WeaponType.Axe:
                    agent.EquipSkill(SkillFactory.CreateWideSweep());
                    agent.EquipSkill(SkillFactory.CreateBerserkRush());
                    agent.EquipSkill(SkillFactory.CreateChargedAttack());
                    break;

                default:
                    agent.EquipSkill(SkillFactory.CreateThrust());
                    agent.EquipSkill(SkillFactory.CreateWideSweep());
                    break;
            }
        }

        private void SetupFormations()
        {
            // Party A formation
            var formGO_A = new GameObject("Formation_PartyA");
            _formationA = formGO_A.AddComponent<FormationController>();
            foreach (var agent in _partyA)
                _formationA.AddAgent(agent, agent.Role);
            _formationA.SetFacing(Vector3.forward);

            // Party B formation
            var formGO_B = new GameObject("Formation_PartyB");
            _formationB = formGO_B.AddComponent<FormationController>();
            foreach (var agent in _partyB)
                _formationB.AddAgent(agent, agent.Role);
            _formationB.SetFacing(Vector3.back);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("<b>AUTONOMOUS MELEE COMBAT DEMO</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(5);
            GUILayout.Label($"Party A (Blue): {CountAlive(_partyA)} alive");
            GUILayout.Label($"Party B (Red): {CountAlive(_partyB)} alive");
            GUILayout.Space(5);
            GUILayout.Label($"Formation A Integrity: {(_formationA != null ? _formationA.Integrity.ToString("F2") : "N/A")}");
            GUILayout.Label($"Formation B Integrity: {(_formationB != null ? _formationB.Integrity.ToString("F2") : "N/A")}");
            GUILayout.EndArea();
        }

        private int CountAlive(List<CombatAgent> party)
        {
            int count = 0;
            foreach (var a in party) if (a != null && a.IsAlive) count++;
            return count;
        }
    }
}

