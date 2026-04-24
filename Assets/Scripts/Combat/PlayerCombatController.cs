using UnityEngine;
using Pithox.Skills;

namespace Pithox.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Skill Spawn Points")]
        [SerializeField] Transform slashPoint;
        [SerializeField] Transform beamPoint;
        [SerializeField] Transform orbitPoint;

        [Header("Skill Prefabs")]
        [SerializeField] GameObject slashPrefab;
        [SerializeField] GameObject beamPrefab;
        [SerializeField] GameObject orbitPrefab;

        [Header("Chain Settings")]
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

            if (Input.GetKeyDown(KeyCode.U))
            {
                TryUseSkill(skillU);
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                TryUseSkill(skillI);
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                TryUseSkill(skillO);
            }
        }

        void TryUseSkill(ActiveSkill skill)
        {
            if (!chainManager.CanUseSkill(skill))
            {
                Debug.Log($"Cannot use {skill.DisplayName}");
                return;
            }

            int chainPosition = chainManager.RegisterSkillUse(skill);
            skill.Execute(transform, chainPosition);
        }

        void TickCooldowns()
        {
            skillU.Cooldown.Tick(Time.deltaTime);
            skillI.Cooldown.Tick(Time.deltaTime);
            skillO.Cooldown.Tick(Time.deltaTime);
        }
    }
}