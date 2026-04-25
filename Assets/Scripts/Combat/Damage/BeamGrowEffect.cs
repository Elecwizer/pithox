using UnityEngine;
using Pithox.Player;

namespace Pithox.Skills
{
    // Handles beam growth and temporary player slow
    public class BeamGrowEffect : MonoBehaviour
    {
        [SerializeField] float maxLength = 8f;
        [SerializeField] float growDuration = 0.35f;
        [SerializeField] float playerSlow = 0.55f;

        Transform owner;
        PlayerMovement playerMovement;

        float timer;

        // Initializes beam behavior
        public void Initialize(Transform ownerTransform)
        {
            owner = ownerTransform;
            playerMovement = owner.GetComponent<PlayerMovement>();

            if (playerMovement != null)
                playerMovement.SetSpeedMultiplier(playerSlow);

            timer = 0f;
        }

        // Grows beam over time
        void Update()
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / growDuration);
            float length = Mathf.Lerp(0.01f, maxLength, t);

            transform.localScale = new Vector3(0.35f, 0.35f, length);
            transform.localPosition = new Vector3(0f, 1f, length * 0.5f);

            if (t >= 1f && playerMovement != null)
            {
                playerMovement.ResetSpeedMultiplier();
                playerMovement = null;
            }
        }

        // Ensures player speed resets if destroyed early
        void OnDestroy()
        {
            if (playerMovement != null)
                playerMovement.ResetSpeedMultiplier();
        }
    }
}