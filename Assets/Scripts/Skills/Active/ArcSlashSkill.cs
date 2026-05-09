using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

namespace Pithox.Skills
{
    public class ArcSlashSkill : ActiveSkill
    {
        readonly GameObject slashPrefab;
        readonly Transform slashPoint;

        public ArcSlashSkill(GameObject slashPrefab, Transform slashPoint, float cooldown = 0.6f)
            : base("arc_slash", "Arc Slash", cooldown)
        {
            this.slashPrefab = slashPrefab;
            this.slashPoint = slashPoint;
        }

        public override void Execute(Transform playerTransform, int chainPosition)
        {
            if (slashPrefab == null || slashPoint == null)
                return;

            GameObject slash = Object.Instantiate(slashPrefab, slashPoint.position, slashPoint.rotation);

            DamageDealer damageDealer = slash.GetComponent<DamageDealer>();
            if (damageDealer != null)
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 10f * GetScaledDamage(playerTransform));

            Object.Destroy(slash, 0.25f);
        }

        float GetScaledDamage(Transform playerTransform)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();
            if (stats == null) return 1f;
            return stats.DamageMultiplier * (1f + stats.AttackDamageBonus);
        }
    }
}
