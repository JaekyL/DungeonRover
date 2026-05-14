// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Combat Configuration ScriptableObject
// =============================================================================
// Master combat profile for configuring an AI combatant.
// Combines doctrine, weapon, skills, and formation preferences.
// =============================================================================

using System.Collections.Generic;
using Combat.AI;
using Combat.Core;
using Combat.Skills;
using Combat.Weapons;
using UnityEngine;

namespace Combat.Configuration
{
    /// <summary>
    /// Master combat profile. Assign to a CombatAgent to configure all combat behavior.
    /// Analogous to TraversalProfile for the traversal system.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCombatProfile", menuName = "Combat/Combat Profile")]
    public class CombatProfileSO : ScriptableObject
    {
        [Header("Identity")]
        public string ProfileName = "Default Fighter";
        [TextArea] public string Description;

        [Header("Doctrine")]
        public CombatDoctrineSO Doctrine;

        [Header("Weapon")]
        public WeaponProfileSO PrimaryWeapon;

        [Header("Skills")]
        public List<CombatSkillSO> EquippedSkills = new List<CombatSkillSO>();

        [Header("Stats")]
        public float MaxHealth = 100f;
        public float MaxStamina = 100f;
        public float StaminaRegenRate = 15f;
        public float MoveSpeed = 4f;
        public float CombatMoveSpeed = 3f;

        [Header("Formation")]
        public FormationRole PreferredRole = FormationRole.Frontline;

        [Header("Behavior Rules")]
        public List<AI.Rules.CombatRule> CombatRules = new List<AI.Rules.CombatRule>();

        /// <summary>Apply this profile to a CombatAgent.</summary>
        public void ApplyTo(CombatAgent agent)
        {
            if (agent == null) return;

            // Skills
            foreach (var skill in EquippedSkills)
            {
                if (skill != null)
                    agent.EquipSkill(skill);
            }

            // Doctrine
            if (Doctrine != null && agent.Brain != null)
                agent.Brain.SetDoctrine(Doctrine);

            // Formation role
            agent.SetFormationRole(PreferredRole);
        }
    }

    /// <summary>
    /// Factory for creating preset combat profiles.
    /// </summary>
    public static class CombatProfilePresets
    {
        public static CombatProfileSO CreateSwordAndShield()
        {
            var p = ScriptableObject.CreateInstance<CombatProfileSO>();
            p.ProfileName = "Sword & Shield Guardian";
            p.Description = "Defensive frontliner. Holds chokepoints and protects allies.";
            p.PrimaryWeapon = WeaponPresets.CreateSword();
            p.Doctrine = CombatDoctrinePresets.CreateGuardian();
            p.PreferredRole = FormationRole.Frontline;
            p.MaxHealth = 130f;
            p.MaxStamina = 90f;
            p.StaminaRegenRate = 18f;
            return p;
        }

        public static CombatProfileSO CreateDaggerRogue()
        {
            var p = ScriptableObject.CreateInstance<CombatProfileSO>();
            p.ProfileName = "Dagger Flanker";
            p.Description = "Mobile striker seeking flanking and backstab opportunities.";
            p.PrimaryWeapon = WeaponPresets.CreateDagger();
            p.Doctrine = CombatDoctrinePresets.CreateFlanker();
            p.PreferredRole = FormationRole.Flank;
            p.MaxHealth = 80f;
            p.MaxStamina = 120f;
            p.StaminaRegenRate = 20f;
            p.MoveSpeed = 5f;
            p.CombatMoveSpeed = 4f;
            return p;
        }

        public static CombatProfileSO CreateSpearWarden()
        {
            var p = ScriptableObject.CreateInstance<CombatProfileSO>();
            p.ProfileName = "Spear Warden";
            p.Description = "Reach fighter controlling corridors from behind allies.";
            p.PrimaryWeapon = WeaponPresets.CreateSpear();
            p.Doctrine = CombatDoctrinePresets.CreateController();
            p.PreferredRole = FormationRole.Backline;
            p.MaxHealth = 100f;
            p.MaxStamina = 100f;
            return p;
        }

        public static CombatProfileSO CreateHammerBerserker()
        {
            var p = ScriptableObject.CreateInstance<CombatProfileSO>();
            p.ProfileName = "Hammer Berserker";
            p.Description = "Reckless damage dealer seeking clustered enemies.";
            p.PrimaryWeapon = WeaponPresets.CreateHammer();
            p.Doctrine = CombatDoctrinePresets.CreateBerserker();
            p.PreferredRole = FormationRole.Vanguard;
            p.MaxHealth = 110f;
            p.MaxStamina = 80f;
            p.StaminaRegenRate = 12f;
            return p;
        }

        public static CombatProfileSO CreateGreatSwordDuelist()
        {
            var p = ScriptableObject.CreateInstance<CombatProfileSO>();
            p.ProfileName = "Greatsword Duelist";
            p.Description = "Precision two-handed fighter with tempo control.";
            p.PrimaryWeapon = WeaponPresets.CreateGreatSword();
            p.Doctrine = CombatDoctrinePresets.CreateDuelist();
            p.PreferredRole = FormationRole.Frontline;
            p.MaxHealth = 100f;
            p.MaxStamina = 100f;
            return p;
        }
    }
}

