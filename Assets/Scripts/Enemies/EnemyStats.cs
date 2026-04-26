using UnityEngine;

namespace Pithox.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyStats",
        menuName = "Pithox/Enemies/Enemy Stats",
        order = 1
    )]
    public class EnemyStats : ScriptableObject
    {
        [Header("Core")]
        public float maxHealth = 20f;
        public float moveSpeed = 3f;

        [Header("Contact Damage")]
        public float touchDamage = 5f;
        public float touchDamageInterval = 0.5f;
        public float touchRange = 1.25f;

        [Header("Chase")]
        public float stopDistance = 1f;
    }
}
