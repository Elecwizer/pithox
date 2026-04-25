using UnityEngine;

namespace Pithox.Combat
{
    public struct DamageData
    {
        public float Amount;
        public GameObject Source;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public int ChainPosition;

        public DamageData(
            float amount,
            GameObject source,
            Vector3 hitPoint,
            Vector3 hitDirection,
            int chainPosition
        )
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            ChainPosition = chainPosition;
        }
    }
}