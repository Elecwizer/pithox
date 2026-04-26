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
        const float SlashForwardOffset = 0.6f;
        const float SlashLifetime = 0.35f;
        const float SlashHeightOffset = 0.55f;
        const float SlashColliderHeight = 6f;

        public ArcSlashSkill(GameObject slashPrefab, Transform slashPoint)
            : base("arc_slash", "Arc Slash", 2f)
        {
            this.slashPrefab = slashPrefab;
            this.slashPoint = slashPoint;
        }

        // Executes slash and initializes damage
        public override void Execute(Transform playerTransform, int chainPosition)
        {
            Vector3 spawnPosition = slashPoint.position + playerTransform.forward * SlashForwardOffset;
            spawnPosition.y = playerTransform.position.y + SlashHeightOffset;
            GameObject slash = Object.Instantiate(
                slashPrefab,
                spawnPosition,
                playerTransform.rotation
            );

            if (slash.TryGetComponent<BoxCollider>(out BoxCollider slashCollider))
            {
                Vector3 size = slashCollider.size;
                size.y = Mathf.Max(size.y, SlashColliderHeight);
                slashCollider.size = size;
            }

            DamageDealer damageDealer = slash.GetComponent<DamageDealer>();

            if (damageDealer != null)
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 10f * GetDamageMultiplier(playerTransform));

            Object.Destroy(slash, SlashLifetime);

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