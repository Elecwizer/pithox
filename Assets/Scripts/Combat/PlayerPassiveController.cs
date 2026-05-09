using System;
using System.Collections.Generic;
using UnityEngine;
using Pithox.Player;

namespace Pithox.Combat
{
    public class PlayerPassiveController : MonoBehaviour
    {
        public enum PassiveType
        {
            MoveSpeedPercent,
            AttackDamagePercent,
            PickupRangeFlat,
            StreakWindowFlat,
            DashSpeedPercent,
            DashCooldownReduction,
            HealPercent
        }

        [Serializable]
        public class PassiveUpgradeOption
        {
            public string label;
            public string groupKey;
            public PassiveType passiveType;
            public float amount;
            public string rarity;
        }

        [SerializeField] PlayerStats stats;
        [SerializeField] PlayerHealth health;

        [Header("Passive Upgrade Pool")]
        [SerializeField]
        PassiveUpgradeOption[] passiveOptions =
        {
            new PassiveUpgradeOption { label = "Speed +5%", groupKey = "speed", passiveType = PassiveType.MoveSpeedPercent, amount = 0.05f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Speed +7.5%", groupKey = "speed", passiveType = PassiveType.MoveSpeedPercent, amount = 0.075f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Speed +10%", groupKey = "speed", passiveType = PassiveType.MoveSpeedPercent, amount = 0.10f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Damage +5%", groupKey = "damage", passiveType = PassiveType.AttackDamagePercent, amount = 0.05f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Damage +7.5%", groupKey = "damage", passiveType = PassiveType.AttackDamagePercent, amount = 0.075f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Damage +10%", groupKey = "damage", passiveType = PassiveType.AttackDamagePercent, amount = 0.10f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Dash Speed +10%", groupKey = "dash_speed", passiveType = PassiveType.DashSpeedPercent, amount = 0.10f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Dash Speed +15%", groupKey = "dash_speed", passiveType = PassiveType.DashSpeedPercent, amount = 0.15f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Dash Speed +20%", groupKey = "dash_speed", passiveType = PassiveType.DashSpeedPercent, amount = 0.20f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Dash Cooldown -10%", groupKey = "dash_cooldown", passiveType = PassiveType.DashCooldownReduction, amount = 0.10f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Dash Cooldown -15%", groupKey = "dash_cooldown", passiveType = PassiveType.DashCooldownReduction, amount = 0.15f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Dash Cooldown -20%", groupKey = "dash_cooldown", passiveType = PassiveType.DashCooldownReduction, amount = 0.20f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Pickup Range +0.5", groupKey = "pickup_range", passiveType = PassiveType.PickupRangeFlat, amount = 0.5f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Pickup Range +1.0", groupKey = "pickup_range", passiveType = PassiveType.PickupRangeFlat, amount = 1f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Pickup Range +1.5", groupKey = "pickup_range", passiveType = PassiveType.PickupRangeFlat, amount = 1.5f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Combo Time +1s", groupKey = "streak_time", passiveType = PassiveType.StreakWindowFlat, amount = 1f, rarity = "Common" },
            new PassiveUpgradeOption { label = "Combo Time +1.5s", groupKey = "streak_time", passiveType = PassiveType.StreakWindowFlat, amount = 1.5f, rarity = "Rare" },
            new PassiveUpgradeOption { label = "Combo Time +2s", groupKey = "streak_time", passiveType = PassiveType.StreakWindowFlat, amount = 2f, rarity = "Epic" },

            new PassiveUpgradeOption { label = "Instant Heal 10%", groupKey = "instant_heal", passiveType = PassiveType.HealPercent, amount = 0.10f, rarity = "Common" }
        };

        readonly Dictionary<string, float> pickedGroupAmounts = new Dictionary<string, float>();

        public PassiveUpgradeOption[] PassiveOptions => passiveOptions;

        void Awake()
        {
            if (stats == null)
                stats = GetComponent<PlayerStats>();

            if (health == null)
                health = GetComponent<PlayerHealth>();
        }

        public List<PassiveUpgradeOption> GetAvailablePassives()
        {
            return GetAvailablePassives(null);
        }

        public List<PassiveUpgradeOption> GetAvailablePassives(HashSet<PassiveUpgradeOption> excluded)
        {
            List<PassiveUpgradeOption> available = new List<PassiveUpgradeOption>();

            for (int i = 0; i < passiveOptions.Length; i++)
            {
                PassiveUpgradeOption option = passiveOptions[i];

                if (option == null)
                    continue;

                if (excluded != null && excluded.Contains(option))
                    continue;

                if (!IsPassiveAvailable(option))
                    continue;

                available.Add(option);
            }

            return available;
        }

        public bool IsPassiveAvailable(PassiveUpgradeOption option)
        {
            if (option == null)
                return false;

            string group = GetGroup(option);

            if (!pickedGroupAmounts.TryGetValue(group, out float currentAmount))
                return true;

            return option.amount > currentAmount;
        }

        public bool ApplyPassive(PassiveUpgradeOption option)
        {
            if (!IsPassiveAvailable(option))
                return false;

            string group = GetGroup(option);
            float currentAmount = pickedGroupAmounts.TryGetValue(group, out float oldAmount) ? oldAmount : 0f;
            float delta = option.amount - currentAmount;

            if (delta <= 0f)
                return false;

            pickedGroupAmounts[group] = option.amount;

            switch (option.passiveType)
            {
                case PassiveType.MoveSpeedPercent:
                    if (stats != null)
                        stats.AddMoveSpeedPercent(delta);
                    break;

                case PassiveType.AttackDamagePercent:
                    if (stats != null)
                        stats.AddAttackDamagePercent(delta);
                    break;

                case PassiveType.PickupRangeFlat:
                    if (stats != null)
                        stats.AddPickupRange(delta);
                    break;

                case PassiveType.StreakWindowFlat:
                    if (stats != null)
                        stats.AddStreakWindow(delta);
                    break;

                case PassiveType.DashSpeedPercent:
                    if (stats != null)
                        stats.AddDashSpeedPercent(delta);
                    break;

                case PassiveType.DashCooldownReduction:
                    if (stats != null)
                        stats.AddDashCooldownReduction(delta);
                    break;

                case PassiveType.HealPercent:
                    if (health != null)
                        health.HealPercent(option.amount);
                    break;
            }

            return true;
        }

        string GetGroup(PassiveUpgradeOption option)
        {
            if (option == null)
                return "";

            if (!string.IsNullOrEmpty(option.groupKey))
                return option.groupKey;

            return option.passiveType.ToString();
        }
    }
}