using UnityEngine;
using Pithox.Player;

namespace Pithox.Skills
{
    public class OrbitAutoCaster : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform orbitPoint;
        [SerializeField] GameObject orbitPrefab;

        OrbitBallsSkill skill;

        void Awake()
        {
            skill = new OrbitBallsSkill(orbitPrefab, orbitPoint);
        }

        void Update()
        {
            skill.Cooldown.Tick(Time.deltaTime);

            if (stats == null || !stats.OrbitUnlocked)
                return;

            if (!skill.Cooldown.IsReady)
                return;

            skill.Execute(transform, 1);
            skill.Cooldown.StartCooldown();
        }
    }
}
