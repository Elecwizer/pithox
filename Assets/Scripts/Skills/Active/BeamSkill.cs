using UnityEngine;

namespace Pithox.Skills
{
    public class BeamSkill : ActiveSkill
    {
        readonly GameObject beamPrefab;
        readonly Transform beamPoint;

        public BeamSkill(GameObject beamPrefab, Transform beamPoint)
            : base("beam", "Beam", 4f)
        {
            this.beamPrefab = beamPrefab;
            this.beamPoint = beamPoint;
        }

        public override void Execute(Transform playerTransform, int chainPosition)
        {
            GameObject beam = Object.Instantiate(
                beamPrefab,
                beamPoint.position,
                playerTransform.rotation
            );

            beam.transform.SetParent(playerTransform);

            Object.Destroy(beam, 1.2f);

            Debug.Log($"Used Beam at chain position {chainPosition}");
        }
    }
}