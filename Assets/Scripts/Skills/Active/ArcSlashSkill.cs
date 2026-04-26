using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

namespace Pithox.Skills
{
    // Spawns a simple forward slash skill
    public class ArcSlashSkill : ActiveSkill
    {
        readonly GameObject slashPrefab;
        readonly Transform slashPoint;

        public ArcSlashSkill(GameObject slashPrefab, Transform slashPoint)
            : base("arc_slash", "Arc Slash", 2f)
        {
            this.slashPrefab = slashPrefab;
            this.slashPoint = slashPoint;
        }

        // Executes slash and initializes damage
        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject slash = Object.Instantiate(
                slashPrefab,
                slashPoint.position,
                playerTransform.rotation
            );

            DamageDealer damageDealer = slash.GetComponent<DamageDealer>();

            if (damageDealer != null)
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 10f * GetDamageMultiplier(playerTransform));

            Object.Destroy(slash, 0.25f);

            Debug.Log($"Used Arc Slash at chain position {chainPosition}");
        }

        // Gets current player damage multiplier
        float GetDamageMultiplier(Transform playerTransform)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();

            if (stats == null)
                return 1f;

            return stats.DamageMultiplier;
        }
    }
}