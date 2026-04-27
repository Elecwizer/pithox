using UnityEngine;

namespace Pithox.Combat
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AttackArcPreview : MonoBehaviour
    {
        [SerializeField] int segments = 32;
        [SerializeField] float extraYOffset = 0f;
        [SerializeField] Color arcColor = new Color(1f, 0.4f, 0.05f, 0.8f);

        Mesh mesh;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Material runtimeMaterial;
        float hideTimer;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            mesh = new Mesh();
            mesh.name = "Attack Arc Mesh";
            meshFilter.sharedMesh = mesh;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            runtimeMaterial = new Material(shader);
            runtimeMaterial.color = arcColor;
            runtimeMaterial.renderQueue = 3000;

            meshRenderer.sharedMaterial = runtimeMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
        }

        void Update()
        {
            if (hideTimer <= 0f)
                return;

            hideTimer -= Time.deltaTime;

            if (hideTimer <= 0f)
                meshRenderer.enabled = false;
        }

        public void Show(Vector3 origin, Vector3 direction, float range, float arcDegrees, float duration, Color color)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;

            float currentHeight = transform.position.y;

            origin.y = currentHeight + extraYOffset;

            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            BuildMesh(range, arcDegrees);

            runtimeMaterial.color = color;

            meshRenderer.enabled = true;
            hideTimer = duration;
        }

        void BuildMesh(float range, float arcDegrees)
        {
            segments = Mathf.Max(3, segments);

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;

            float startAngle = -arcDegrees * 0.5f;
            float step = arcDegrees / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + step * i;
                float radians = angle * Mathf.Deg2Rad;

                float x = Mathf.Sin(radians) * range;
                float z = Mathf.Cos(radians) * range;

                vertices[i + 1] = new Vector3(x, 0f, z);
            }

            int t = 0;

            for (int i = 1; i <= segments; i++)
            {
                triangles[t] = 0;
                triangles[t + 1] = i;
                triangles[t + 2] = i + 1;

                t += 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}