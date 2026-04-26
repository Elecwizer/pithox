using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    // Handles equipped passive skills and triggers them automatically
    public class PlayerPassiveController : MonoBehaviour
    {
        [Header("Passive Prefabs")]
        [SerializeField] GameObject pulsePrefab;
        [SerializeField] GameObject healVfxPrefab;
        [SerializeField] GameObject damageBuffVfxPrefab;

        PassiveSkill[] passiveSkills;

        void Awake()
        {
            passiveSkills = new PassiveSkill[]
            {
                new PulsePassiveSkill(pulsePrefab),
                new HealPassiveSkill(healVfxPrefab, 20f),
                new DamageBoostPassiveSkill(damageBuffVfxPrefab, 1.5f, 6f)
            };
        }

        void Update()
        {
            foreach (PassiveSkill skill in passiveSkills)
            {
                TickPassive(skill);
            }
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