using UnityEngine;

namespace Pithox.Skills
{
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

        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject orbit = Object.Instantiate(
                orbitPrefab,
                orbitPoint.position,
                Quaternion.identity
            );

            orbit.transform.SetParent(playerTransform);

            Object.Destroy(orbit, 3f);

            Debug.Log($"Used Orbit Balls at chain position {chainPosition}");
        }
    }
}