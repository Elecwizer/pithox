using UnityEngine;

namespace Pithox.Skills
{
    // Base class for passive skills that trigger automatically
    public abstract class PassiveSkill
    {
        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillCooldown Cooldown { get; }

        protected PassiveSkill(string skillId, string displayName, float cooldown)
        {
            SkillId = skillId;
            DisplayName = displayName;
            Cooldown = new SkillCooldown(cooldown);
        }

        // Called when passive cooldown is ready
        public abstract void Execute(Transform playerTransform);
    }
}