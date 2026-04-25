using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    // Handles equipped passive skills and triggers them automatically
    public class PlayerPassiveController : MonoBehaviour
    {
        [Header("Passive Prefabs")]
        [SerializeField] GameObject pulsePrefab;

        PassiveSkill passiveSkill;

        void Awake()
        {
            passiveSkill = new PulsePassiveSkill(pulsePrefab);
        }

        void Update()
        {
            TickPassive(passiveSkill);
        }

        // Updates passive cooldown and triggers when ready
        void TickPassive(PassiveSkill skill)
        {
            if (skill == null)
                return;

            skill.Cooldown.Tick(Time.deltaTime);

            if (!skill.Cooldown.IsReady)
                return;

            skill.Execute(transform);
            skill.Cooldown.StartCooldown();
        }
    }
}