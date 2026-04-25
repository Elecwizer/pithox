using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    // Handles player skill input and communicates with chain system
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Skill Points")]
        [SerializeField] Transform slashPoint;
        [SerializeField] Transform beamPoint;
        [SerializeField] Transform orbitPoint;

        [Header("Prefabs")]
        [SerializeField] GameObject slashPrefab;
        [SerializeField] GameObject beamPrefab;
        [SerializeField] GameObject orbitPrefab;

        [SerializeField] float chainWindowDuration = 1.2f;

        ChainManager chainManager;

        ActiveSkill skillU;
        ActiveSkill skillI;
        ActiveSkill skillO;

        void Awake()
        {
            chainManager = new ChainManager(chainWindowDuration);

            skillU = new ArcSlashSkill(slashPrefab, slashPoint);
            skillI = new BeamSkill(beamPrefab, beamPoint);
            skillO = new OrbitBallsSkill(orbitPrefab, orbitPoint);
        }

        void Update()
        {
            TickCooldowns();
            chainManager.Tick(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
                TryUsePrimaryAttack();

            if (Input.GetKeyDown(KeyCode.U)) TryUseSkill(skillU);
            if (Input.GetKeyDown(KeyCode.I)) TryUseSkill(skillI);
            if (Input.GetKeyDown(KeyCode.O)) TryUseSkill(skillO);
        }

        void TryUsePrimaryAttack()
        {
            if (!skillU.Cooldown.IsReady)
                return;

            skillU.Execute(transform, 1);
            skillU.Cooldown.StartCooldown();
        }

        // Attempts to use a skill and apply chain logic
        void TryUseSkill(ActiveSkill skill)
        {
            if (!chainManager.CanUseSkill(skill))
                return;

            int chainPosition = chainManager.RegisterSkillUse(skill);

            ApplyChainFeedback(chainPosition);
            skill.Execute(transform, chainPosition);
        }

        // Prints simple feedback for chain state
        void ApplyChainFeedback(int chainPosition)
        {
            if (chainPosition == 1) Debug.Log("CHAIN 1");
            if (chainPosition == 2) Debug.Log("CHAIN 2");
            if (chainPosition == 3) Debug.Log("CHAIN 3");
        }

        // Updates cooldown timers
        void TickCooldowns()
        {
            skillU.Cooldown.Tick(Time.deltaTime);
            skillI.Cooldown.Tick(Time.deltaTime);
            skillO.Cooldown.Tick(Time.deltaTime);
        }
    }
}