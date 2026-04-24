using UnityEngine;

namespace Pithox.Skills
{
    public abstract class ActiveSkill
    {
        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillCooldown Cooldown { get; }

        protected ActiveSkill(string skillId, string displayName, float cooldown)
        {
            SkillId = skillId;
            DisplayName = displayName;
            Cooldown = new SkillCooldown(cooldown);
        }

        public abstract void Execute(Transform playerTransform, int chainPosition);
    }
}