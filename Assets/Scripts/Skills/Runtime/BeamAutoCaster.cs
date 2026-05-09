using UnityEngine;
using Pithox.Player;

namespace Pithox.Skills
{
    public class BeamAutoCaster : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform beamPoint;
        [SerializeField] GameObject beamPrefab;

        BeamSkill skill;

        void Awake()
        {
            skill = new BeamSkill(beamPrefab, beamPoint);
        }

        void Update()
        {
            skill.Cooldown.Tick(Time.deltaTime);

            if (stats == null || !stats.BeamUnlocked)
                return;

            if (!skill.Cooldown.IsReady)
                return;

            skill.Execute(transform, 1);
            skill.Cooldown.StartCooldown();
        }
    }
}
