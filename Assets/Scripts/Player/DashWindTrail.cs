using UnityEngine;

namespace Pithox.Player
{
    public class DashWindTrail : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerController playerController;

        [Header("Trail")]
        [SerializeField] int trailCount = 3;
        [SerializeField] float trailHeight = 0.35f;
        [SerializeField] float sideSpacing = 0.28f;
        [SerializeField] float trailTime = 0.18f;
        [SerializeField] float startWidth = 0.28f;
        [SerializeField] float endWidth = 0.02f;
        [SerializeField] Color startColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] Color endColor = new Color(1f, 1f, 1f, 0f);

        [Header("Look")]
        [SerializeField] Material trailMaterial;

        TrailRenderer[] trails;
        Transform[] trailPoints;
        bool wasDashing;

        void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (trailMaterial == null)
                trailMaterial = CreateTrailMaterial();

            CreateTrails();
        }

        void LateUpdate()
        {
            if (playerController == null)
                return;

            Vector3 moveDir = playerController.MoveDirection;
            moveDir.y = 0f;

            if (moveDir.sqrMagnitude < 0.001f)
                moveDir = transform.forward;

            moveDir.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, moveDir).normalized;

            bool isDashing = playerController.IsDashing;

            if (isDashing && !wasDashing)
            {
                PlaceTrailPoints(right);
                ClearTrails();

                for (int i = 0; i < trails.Length; i++)
                    trails[i].emitting = true;
            }

            if (!isDashing && wasDashing)
            {
                for (int i = 0; i < trails.Length; i++)
                    trails[i].emitting = false;
            }

            PlaceTrailPoints(right);

            wasDashing = isDashing;
        }

        void CreateTrails()
        {
            trailCount = Mathf.Max(1, trailCount);

            trails = new TrailRenderer[trailCount];
            trailPoints = new Transform[trailCount];

            for (int i = 0; i < trailCount; i++)
            {
                GameObject point = new GameObject("Dash Wind Trail " + i);
                point.transform.SetParent(transform);

                TrailRenderer trail = point.AddComponent<TrailRenderer>();

                trail.time = trailTime;
                trail.startWidth = startWidth;
                trail.endWidth = endWidth;
                trail.material = trailMaterial;
                trail.emitting = false;
                trail.autodestruct = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.numCapVertices = 4;
                trail.numCornerVertices = 4;
                trail.alignment = LineAlignment.View;
                trail.textureMode = LineTextureMode.Stretch;

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(endColor, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(startColor.a, 0f),
                        new GradientAlphaKey(endColor.a, 1f)
                    }
                );

                trail.colorGradient = gradient;

                trails[i] = trail;
                trailPoints[i] = point.transform;
            }
        }

        void PlaceTrailPoints(Vector3 right)
        {
            if (trailPoints == null)
                return;

            float middle = (trailPoints.Length - 1) * 0.5f;

            for (int i = 0; i < trailPoints.Length; i++)
            {
                float offset = (i - middle) * sideSpacing;

                Vector3 pos = transform.position;
                pos.y += trailHeight;
                pos += right * offset;

                trailPoints[i].position = pos;
            }
        }

        void ClearTrails()
        {
            if (trails == null)
                return;

            for (int i = 0; i < trails.Length; i++)
                trails[i].Clear();
        }

        Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            Material mat = new Material(shader);
            mat.name = "Runtime Dash Wind Trail";
            mat.color = Color.white;
            mat.renderQueue = 3000;

            return mat;
        }
    }
}