using UnityEngine;

namespace Pithox.Player
{
    public class PlayerStats : MonoBehaviour
    {
        float damageMultiplier = 1f;
        float damageBuffTimer;

        float pickupRangeBonus;
        float streakWindowBonus;
        float attackDamageBonus;
        float moveSpeedBonus;
        float orbitDamageBonus;
        float beamDamageBonus;

        bool orbitUnlocked;
        bool beamUnlocked;
        bool damageAuraEnabled;

        public float DamageMultiplier => damageMultiplier;
        public float PickupRangeBonus => pickupRangeBonus;
        public float StreakWindowBonus => streakWindowBonus;
        public float AttackDamageBonus => attackDamageBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float OrbitDamageBonus => orbitDamageBonus;
        public float BeamDamageBonus => beamDamageBonus;
        public bool OrbitUnlocked => orbitUnlocked;
        public bool BeamUnlocked => beamUnlocked;
        public bool DamageAuraEnabled => damageAuraEnabled;

        void Update()
        {
            if (damageBuffTimer <= 0f)
                return;

            damageBuffTimer -= Time.deltaTime;

            if (damageBuffTimer <= 0f)
            {
                damageMultiplier = 1f;
                Debug.Log("Damage buff ended");
            }
        }

        public void ApplyDamageBoost(float multiplier, float duration)
        {
            damageMultiplier = multiplier;
            damageBuffTimer = duration;

            Debug.Log($"Damage buff applied: x{multiplier}");
        }

        public void AddPickupRange(float amount) { pickupRangeBonus += amount; }
        public void AddStreakWindow(float amount) { streakWindowBonus += amount; }
        public void AddAttackDamage(float amount) { attackDamageBonus += amount; }
        public void AddMoveSpeed(float amount) { moveSpeedBonus += amount; }
        public void AddOrbitDamage(float amount) { orbitDamageBonus += amount; }
        public void AddBeamDamage(float amount) { beamDamageBonus += amount; }

        public void UnlockOrbit() { orbitUnlocked = true; }
        public void UnlockBeam() { beamUnlocked = true; }
        public void EnableDamageAura() { damageAuraEnabled = true; }
    }
}
