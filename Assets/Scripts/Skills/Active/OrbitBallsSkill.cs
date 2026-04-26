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

            foreach (DamageDealer damageDealer in damageDealers)
            {
                damageDealer.Initialize(playerTransform.gameObject, chainPosition, 7f * GetDamageMultiplier(playerTransform));
            }

            Object.Destroy(orbit, 3f);

            Debug.Log($"Used Orbit Balls at chain position {chainPosition}");
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