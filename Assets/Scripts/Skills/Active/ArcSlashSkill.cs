using UnityEngine;
using Pithox.Combat;

namespace Pithox.Skills
{
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

        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject slash = Object.Instantiate(
                slashPrefab,
                slashPoint.position,
                playerTransform.rotation
            );

            DamageDealer damageDealer = slash.GetComponent<DamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 10f);
            }

            Object.Destroy(slash, 0.25f);

            Debug.Log($"Used Arc Slash at chain position {chainPosition}");
        }
    }
}