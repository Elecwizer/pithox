using UnityEngine;
using Pithox.Combat;

namespace Pithox.Skills
{
    // Creates a damage pulse around the player automatically
    public class PulsePassiveSkill : PassiveSkill
    {
        readonly GameObject pulsePrefab;

        public PulsePassiveSkill(GameObject pulsePrefab)
            : base("pulse_passive", "Pulse Passive", 3f)
        {
            this.pulsePrefab = pulsePrefab;
        }

        // Spawns pulse damage area at player position
        public override void Execute(Transform playerTransform)
        {
            GameObject pulse = Object.Instantiate(
                pulsePrefab,
                playerTransform.position + Vector3.up,
                Quaternion.identity
            );

            DamageDealer damageDealer = pulse.GetComponent<DamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.Initialize(playerTransform.gameObject, 0, 8f);
            }

            Object.Destroy(pulse, 0.4f);

            Debug.Log("Passive triggered: Pulse");
        }
    }
}