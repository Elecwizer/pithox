using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

namespace Pithox.Skills
{
    // Spawns a forward beam skill
    public class BeamSkill : ActiveSkill
    {
        readonly GameObject beamPrefab;
        readonly Transform beamPoint;

        public BeamSkill(GameObject beamPrefab, Transform beamPoint)
            : base("beam", "Beam", 4f)
        {
            this.beamPrefab = beamPrefab;
            this.beamPoint = beamPoint;
        }

        // Executes beam and initializes damage
        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject beam = Object.Instantiate(
                beamPrefab,
                beamPoint.position,
                playerTransform.rotation
            );

            beam.transform.SetParent(playerTransform);
            beam.transform.localRotation = Quaternion.identity;

            BeamGrowEffect growEffect = beam.GetComponent<BeamGrowEffect>();

            if (growEffect != null)
                growEffect.Initialize(playerTransform);

            DamageDealer damageDealer = beam.GetComponent<DamageDealer>();

            if (damageDealer != null)
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 5f * GetScaledDamage(playerTransform));

            Object.Destroy(beam, 1.2f);
        }

        float GetScaledDamage(Transform playerTransform)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();

            if (stats == null)
                return 1f;

            return stats.DamageMultiplier * (1f + stats.BeamDamageBonus);
        }
    }
}