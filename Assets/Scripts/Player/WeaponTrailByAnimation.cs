using System.Collections;
using UnityEngine;

namespace Pithox.Combat
{
    [RequireComponent(typeof(TrailRenderer))]
    public class WeaponTrailByAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject player;
        [SerializeField] Animator animator;
        [SerializeField] TrailRenderer trail;

        [Header("Animator State Names")]
        [SerializeField] string lightAttackStateName = "BlackWizard_Attack";
        [SerializeField] string heavyAttackStateName = "BlackWizard_ChargedAttack";
        [SerializeField] int animatorLayer = 0;

        [Header("Light Trail")]
        [SerializeField] float lightStartDelay = 0.03f;
        [SerializeField] float lightDuration = 0.22f;

        [Header("Heavy Trail")]
        [SerializeField] float heavyStartDelay = 0.08f;
        [SerializeField] float heavyDuration = 0.35f;

        int lastStateHash;
        Coroutine trailRoutine;

        void Awake()
        {
            if (trail == null)
                trail = GetComponent<TrailRenderer>();

            if (animator == null && player != null)
                animator = player.GetComponentInChildren<Animator>();

            trail.emitting = false;
            trail.Clear();
        }

        void Update()
        {
            if (animator == null)
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(animatorLayer);

            if (state.fullPathHash == lastStateHash)
                return;

            lastStateHash = state.fullPathHash;

            if (state.IsName(lightAttackStateName))
                PlayTrail(lightStartDelay, lightDuration);
            else if (state.IsName(heavyAttackStateName))
                PlayTrail(heavyStartDelay, heavyDuration);
        }

        void PlayTrail(float delay, float duration)
        {
            if (trailRoutine != null)
                StopCoroutine(trailRoutine);

            trailRoutine = StartCoroutine(TrailRoutine(delay, duration));
        }

        IEnumerator TrailRoutine(float delay, float duration)
        {
            trail.emitting = false;
            trail.Clear();

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            trail.Clear();
            trail.emitting = true;

            yield return new WaitForSeconds(duration);

            trail.emitting = false;
            trailRoutine = null;
        }
    }
}