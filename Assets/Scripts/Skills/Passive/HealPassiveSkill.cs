using UnityEngine;
using Pithox.Player;

namespace Pithox.Skills
{
    // Heals the player automatically and spawns healing particles
    public class HealPassiveSkill : PassiveSkill
    {
        readonly GameObject healVfxPrefab;
        readonly float healAmount;

        public HealPassiveSkill(GameObject healVfxPrefab, float healAmount)
            : base("heal_passive", "Heal Passive", 30f)
        {
            this.healVfxPrefab = healVfxPrefab;
            this.healAmount = healAmount;
        }

        // Heals player and spawns heal VFX
        public override void Execute(Transform playerTransform)
        {
            PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();

            if (health != null)
                health.Heal(healAmount);

            if (healVfxPrefab != null)
            {
                GameObject vfx = Object.Instantiate(
                    healVfxPrefab,
                    playerTransform.position,
                    Quaternion.identity
                );

                Object.Destroy(vfx, 2f);
            }

            Debug.Log("Passive triggered: Heal");
        }
    }
}