using System.Collections.Generic;
using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    public class ChainManager
    {
        readonly List<ActiveSkill> usedSkills = new();

        float chainTimer;
        readonly float chainWindowDuration;

        public bool IsChainActive => usedSkills.Count > 0;
        public int CurrentChainCount => usedSkills.Count;

        public ChainManager(float chainWindowDuration)
        {
            this.chainWindowDuration = chainWindowDuration;
        }

        public bool CanUseSkill(ActiveSkill skill)
        {
            if (skill == null) return false;
            if (!skill.Cooldown.IsReady) return false;
            if (usedSkills.Count >= 3) return false;
            if (usedSkills.Contains(skill)) return false;

            return true;
        }

        public int RegisterSkillUse(ActiveSkill skill)
        {
            usedSkills.Add(skill);
            chainTimer = chainWindowDuration;

            return usedSkills.Count;
        }

        public void Tick(float deltaTime)
        {
            if (usedSkills.Count == 0) return;

            chainTimer -= deltaTime;

            if (chainTimer <= 0f)
            {
                EndChain();
            }
        }

        private void EndChain()
        {
            foreach (ActiveSkill skill in usedSkills)
            {
                skill.Cooldown.StartCooldown();
            }

            usedSkills.Clear();
            chainTimer = 0f;

            Debug.Log("Chain ended. Cooldowns started.");
        }
    }
}