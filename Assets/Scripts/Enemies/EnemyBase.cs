using Pithox.Combat;
using UnityEngine;

namespace Pithox.Enemies
{
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Core Stats")]
        [SerializeField] protected float maxHealth = 20f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float stopDistance = 1f;

        [Header("Touch Damage")]
        [SerializeField] protected float touchDamage = 5f;
        [SerializeField] protected float touchDamageInterval = 0.5f;
        [SerializeField] protected float touchRange = 1.25f;

        [SerializeField] protected string playerTag = "Player";

        protected Transform playerTarget;
        float currentHealth;
        float nextTouchDamageTime;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        protected virtual void Start()
        {
            FindPlayerTarget();
        }

        protected virtual void Update()
        {
            if (playerTarget == null)
            {
                FindPlayerTarget();
                return;
            }

            TickMovement();
            TickTouchDamage();
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            currentHealth -= damageData.Amount;
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        protected virtual void TickMovement()
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            float sqrDistance = toPlayer.sqrMagnitude;
            if (sqrDistance <= stopDistance * stopDistance)
                return;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Vector3 moveDirection = toPlayer.normalized;
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                transform.forward = moveDirection;
            }
        }

        protected virtual void TickTouchDamage()
        {
            if (Time.time < nextTouchDamageTime)
                return;

            if (!IsInTouchRange())
                return;

            if (playerTarget.TryGetComponent<IDamageable>(out IDamageable playerDamageable))
            {
                Vector3 hitDirection = (playerTarget.position - transform.position).normalized;
                playerDamageable.TakeDamage(new DamageData(
                    touchDamage,
                    gameObject,
                    playerTarget.position,
                    hitDirection,
                    0
                ));
                nextTouchDamageTime = Time.time + Mathf.Max(0.01f, touchDamageInterval);
            }
        }

        bool IsInTouchRange()
        {
            Vector3 offset = playerTarget.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= touchRange * touchRange;
        }

        void FindPlayerTarget()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            playerTarget = playerObject != null ? playerObject.transform : null;
        }
    }
}
