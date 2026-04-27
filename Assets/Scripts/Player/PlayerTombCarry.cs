using System;
using UnityEngine;

namespace Pithox.Player
{
    public class PlayerTombCarry : MonoBehaviour
    {
        [SerializeField] KeyCode pickupKey = KeyCode.Space;
        [SerializeField] float pickupRange = 2f;
        [SerializeField] float potRange = 3f;
        [SerializeField] string tombTag = "Tomb";

        [SerializeField] Transform pot;
        [SerializeField] GameObject carriedTombVisual;
        [SerializeField] PlayerStats stats;

        [Header("SFX")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip pickupSfx;
        [SerializeField] AudioClip placeSfx;
        [SerializeField] float pickupVolume = 4f;
        [SerializeField] float placeVolume = 4f;

        bool hasTomb;

        public bool IsCarrying => hasTomb;

        public static event Action OnTombPickedUp;
        public static event Action OnTombPlaced;

        void Awake()
        {
            if (stats == null)
                stats = GetComponent<PlayerStats>();
        }

        void Start()
        {
            if (carriedTombVisual != null)
                carriedTombVisual.SetActive(false);

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();

                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
            sfxSource.mute = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(pickupKey))
                TryUseTomb();
        }

        public void TryUseTomb()
        {
            if (hasTomb)
                TryPlaceInPot();
            else
                TryPickUpTomb();
        }

        void TryPickUpTomb()
        {
            float effectiveRange = pickupRange + (stats != null ? stats.PickupRangeBonus : 0f);

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                effectiveRange,
                ~0,
                QueryTriggerInteraction.Collide
            );

            Collider closestTomb = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag(tombTag))
                    continue;

                Vector3 playerPos = transform.position;
                Vector3 tombPos = hit.transform.position;

                playerPos.y = 0f;
                tombPos.y = 0f;

                float distance = Vector3.Distance(playerPos, tombPos);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTomb = hit;
                }
            }

            if (closestTomb == null)
                return;

            hasTomb = true;

            if (carriedTombVisual != null)
                carriedTombVisual.SetActive(true);

            PlaySfx(pickupSfx, pickupVolume);

            Destroy(closestTomb.gameObject);

            OnTombPickedUp?.Invoke();
        }

        void TryPlaceInPot()
        {
            if (pot == null)
                return;

            Vector3 playerPos = transform.position;
            Vector3 potPos = pot.position;

            playerPos.y = 0f;
            potPos.y = 0f;

            float distance = Vector3.Distance(playerPos, potPos);

            if (distance > potRange)
                return;

            hasTomb = false;

            if (carriedTombVisual != null)
                carriedTombVisual.SetActive(false);

            PlaySfx(placeSfx, placeVolume);

            Debug.Log("Tomb collected");

            OnTombPlaced?.Invoke();
        }

        void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, volume);
        }
    }
}