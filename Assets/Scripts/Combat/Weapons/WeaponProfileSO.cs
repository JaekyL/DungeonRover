// =============================================================================
// AUTONOMOUS MELEE COMBAT SYSTEM
// Weapon Profiles
// =============================================================================
// ScriptableObject defining weapon types and their combat properties.
// Weapons strongly influence utility scoring, spacing, and available skills.
// =============================================================================

using Combat.Core;
using UnityEngine;

namespace Combat.Weapons
{
    /// <summary>
    /// ScriptableObject defining a weapon's combat properties.
    /// Weapons alter: reach, damage, speed, preferred spacing, skill availability.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Profile")]
    public class WeaponProfileSO : ScriptableObject
    {
        [Header("Identity")]
        public string WeaponName = "Weapon";
        public WeaponType WeaponType = WeaponType.Sword;
        public Sprite Icon;

        [Header("Combat Stats")]
        [Tooltip("Effective combat reach in units")]
        public float Reach = 1.5f;

        [Tooltip("Base damage per hit")]
        public float BaseDamage = 15f;

        [Tooltip("Attack speed multiplier (1=normal)")]
        [Range(0.5f, 2f)] public float SpeedMultiplier = 1f;

        [Tooltip("Stamina cost multiplier for attacks")]
        [Range(0.5f, 2f)] public float StaminaCostMultiplier = 1f;

        [Header("Spacing Preferences")]
        [Tooltip("Preferred engagement distance (as multiplier of reach)")]
        [Range(0.5f, 1.5f)] public float PreferredDistanceRatio = 0.8f;

        [Tooltip("Minimum comfortable distance")]
        public float MinComfortDistance = 0.5f;

        [Header("Behavioral Properties")]
        [Tooltip("Can this weapon attack over allies? (spears, polearms)")]
        public bool CanAttackOverAllies = false;

        [Tooltip("Does this weapon excel in tight spaces? (daggers)")]
        public bool ExcelsInTightSpaces = false;

        [Tooltip("Does this weapon control corridors? (polearms)")]
        public bool ControlsCorridors = false;

        [Tooltip("Does this weapon provide a blocking bonus?")]
        public float BlockingBonus = 0f;

        [Header("Utility AI Biases")]
        [Tooltip("Bonus to clustered enemy targeting (hammers)")]
        [Range(0, 1)] public float ClusterPreference = 0f;

        [Tooltip("Bonus to flank-seeking behavior (daggers)")]
        [Range(0, 1)] public float FlankPreference = 0f;

        [Tooltip("Bonus to chokepoint-holding behavior (shields)")]
        [Range(0, 1)] public float ChokepointPreference = 0f;

        [Tooltip("Bonus to maintaining distance (spears)")]
        [Range(0, 1)] public float DistancePreference = 0f;

        /// <summary>Get the preferred engagement distance for this weapon.</summary>
        public float PreferredDistance => Reach * PreferredDistanceRatio;
    }

    /// <summary>
    /// Factory for creating weapon profile presets.
    /// </summary>
    public static class WeaponPresets
    {
        public static WeaponProfileSO CreateSword()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Sword";
            w.WeaponType = WeaponType.Sword;
            w.Reach = 1.5f;
            w.BaseDamage = 15f;
            w.SpeedMultiplier = 1f;
            w.PreferredDistanceRatio = 0.8f;
            return w;
        }

        public static WeaponProfileSO CreateDagger()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Dagger";
            w.WeaponType = WeaponType.Dagger;
            w.Reach = 1.0f;
            w.BaseDamage = 10f;
            w.SpeedMultiplier = 1.5f;
            w.StaminaCostMultiplier = 0.7f;
            w.PreferredDistanceRatio = 0.7f;
            w.MinComfortDistance = 0.3f;
            w.ExcelsInTightSpaces = true;
            w.FlankPreference = 0.9f;
            return w;
        }

        public static WeaponProfileSO CreateSpear()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Spear";
            w.WeaponType = WeaponType.Spear;
            w.Reach = 3f;
            w.BaseDamage = 12f;
            w.SpeedMultiplier = 0.9f;
            w.PreferredDistanceRatio = 0.9f;
            w.CanAttackOverAllies = true;
            w.ControlsCorridors = true;
            w.DistancePreference = 0.8f;
            return w;
        }

        public static WeaponProfileSO CreatePolearm()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Polearm";
            w.WeaponType = WeaponType.Polearm;
            w.Reach = 3.5f;
            w.BaseDamage = 18f;
            w.SpeedMultiplier = 0.7f;
            w.StaminaCostMultiplier = 1.3f;
            w.PreferredDistanceRatio = 0.85f;
            w.CanAttackOverAllies = true;
            w.ControlsCorridors = true;
            w.DistancePreference = 0.9f;
            w.ChokepointPreference = 0.7f;
            return w;
        }

        public static WeaponProfileSO CreateAxe()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Axe";
            w.WeaponType = WeaponType.Axe;
            w.Reach = 1.5f;
            w.BaseDamage = 20f;
            w.SpeedMultiplier = 0.85f;
            w.StaminaCostMultiplier = 1.2f;
            w.PreferredDistanceRatio = 0.8f;
            return w;
        }

        public static WeaponProfileSO CreateHammer()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Hammer";
            w.WeaponType = WeaponType.Hammer;
            w.Reach = 1.8f;
            w.BaseDamage = 25f;
            w.SpeedMultiplier = 0.7f;
            w.StaminaCostMultiplier = 1.4f;
            w.PreferredDistanceRatio = 0.85f;
            w.ClusterPreference = 0.9f;
            return w;
        }

        public static WeaponProfileSO CreateShield()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Shield";
            w.WeaponType = WeaponType.Shield;
            w.Reach = 1.0f;
            w.BaseDamage = 5f;
            w.SpeedMultiplier = 0.9f;
            w.StaminaCostMultiplier = 0.8f;
            w.BlockingBonus = 0.4f;
            w.ChokepointPreference = 0.9f;
            return w;
        }

        public static WeaponProfileSO CreateGreatSword()
        {
            var w = ScriptableObject.CreateInstance<WeaponProfileSO>();
            w.WeaponName = "Great Sword";
            w.WeaponType = WeaponType.GreatSword;
            w.Reach = 2.2f;
            w.BaseDamage = 22f;
            w.SpeedMultiplier = 0.75f;
            w.StaminaCostMultiplier = 1.3f;
            w.PreferredDistanceRatio = 0.85f;
            w.ClusterPreference = 0.5f;
            return w;
        }
    }
}

