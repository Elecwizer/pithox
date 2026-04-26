using UnityEngine;
using Pithox.Player;

namespace Pithox.Skills
{
    // Gives the player a temporary damage boost automatically
    public class DamageBoostPassiveSkill : PassiveSkill
    {
        readonly GameObject buffVfxPrefab;
        readonly float damageMultiplier;
        readonly float duration;

        public DamageBoostPassiveSkill(GameObject buffVfxPrefab, float damageMultiplier, float duration)
            : base("damage_boost_passive", "Damage Boost Passive", 25f)
        {
            this.buffVfxPrefab = buffVfxPrefab;
            this.damageMultiplier = damageMultiplier;
            this.duration = duration;
        }

        // Applies damage boost and spawns buff VFX
        public override void Execute(Transform playerTransform)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();

            if (stats != null)
                stats.ApplyDamageBoost(damageMultiplier, duration);

            if (buffVfxPrefab != null)
            {
                GameObject vfx = Object.Instantiate(
                    buffVfxPrefab,
                    playerTransform.position,
                    Quaternion.identity
                );

                vfx.transform.SetParent(playerTransform);
                Object.Destroy(vfx, duration);
            }

            Debug.Log("Passive triggered: Damage Boost");
        }
    }
}