using System.Collections.Generic;
using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    // Controls skill chaining logic and cooldown triggering
    public class ChainManager
    {
        readonly List<ActiveSkill> usedSkills = new();

        float chainTimer;
        float chainWindowDuration;

        public ChainManager(float chainWindowDuration)
        {
            this.chainWindowDuration = chainWindowDuration;
        }

        // Checks if a skill can be used in the current chain
        public bool CanUseSkill(ActiveSkill skill)
        {
            if (!skill.Cooldown.IsReady) return false;
            if (usedSkills.Count >= 3) return false;
            if (usedSkills.Contains(skill)) return false;

            return true;
        }

        // Registers skill usage and returns chain position
        public int RegisterSkillUse(ActiveSkill skill)
        {
            usedSkills.Add(skill);
            chainTimer = chainWindowDuration;

            return usedSkills.Count;
        }

        // Updates chain timer and ends chain if expired
        public void Tick(float deltaTime)
        {
            if (usedSkills.Count == 0) return;

            chainTimer -= deltaTime;

            if (chainTimer <= 0f)
            {
                EndChain();
            }
        }

        // Ends chain and starts cooldowns
        void EndChain()
        {
            foreach (var skill in usedSkills)
            {
                skill.Cooldown.StartCooldown();
            }

            usedSkills.Clear();
            chainTimer = 0f;

            Debug.Log("Chain ended");
        }
    }
}