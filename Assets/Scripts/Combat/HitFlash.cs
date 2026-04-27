using UnityEngine;

namespace Pithox.Combat
{
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] Color flashColor = Color.white;
        [SerializeField] float duration = 0.08f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        float remaining;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
        }

        public void Flash()
        {
            remaining = duration;
            ApplyTint(flashColor);
        }

        void Update()
        {
            if (remaining <= 0f)
                return;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                ClearTint();
        }

        void ApplyTint(Color c)
        {
            if (renderers == null) return;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        void ClearTint()
        {
            if (renderers == null) return;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }
    }
}
