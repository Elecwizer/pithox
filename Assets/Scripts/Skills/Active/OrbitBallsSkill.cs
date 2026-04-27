using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

namespace Pithox.Skills
{
    // Spawns orbiting damage balls around the player
    public class OrbitBallsSkill : ActiveSkill
    {
        readonly GameObject orbitPrefab;
        readonly Transform orbitPoint;

        public OrbitBallsSkill(GameObject orbitPrefab, Transform orbitPoint)
            : base("orbit_balls", "Orbit Balls", 5f)
        {
            this.orbitPrefab = orbitPrefab;
            this.orbitPoint = orbitPoint;
        }

        // Executes orbit balls and initializes damage
        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject orbit = Object.Instantiate(
                orbitPrefab,
                orbitPoint.position,
                Quaternion.identity
            );

            orbit.transform.SetParent(playerTransform);

            DamageDealer[] damageDealers = orbit.GetComponentsInChildren<DamageDealer>();

            float scaled = 7f * GetScaledDamage(playerTransform);

            foreach (DamageDealer damageDealer in damageDealers)
            {
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, scaled);
            }

            Object.Destroy(orbit, 3f);
        }

        float GetScaledDamage(Transform playerTransform)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();

            if (stats == null)
                return 1f;

            return stats.DamageMultiplier * (1f + stats.OrbitDamageBonus);
        }
    }
}