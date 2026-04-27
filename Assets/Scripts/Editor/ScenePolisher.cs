#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Pithox.Combat;
using Pithox.Visual;

namespace Pithox.EditorTools
{
    public static class ScenePolisher
    {
        const string MatDir = "Assets/Material/Generated";

        [MenuItem("Pithox/Polish Visuals")]
        public static void Polish()
        {
            Directory.CreateDirectory(MatDir);
            AssetDatabase.Refresh();

            Materials mats = GenerateAllMaterials();

            ApplyEnemyPrefabs(mats);
            ApplySkillPrefabs(mats);
            ApplyPotPrefab(mats);
            ApplyTombPrefab(mats);
            ApplyToScenePlayerAndProps(mats);

            EnsureLightingAndFog();
            EnsureCameraSolidColor();
            EnsureFloorAndProps(mats);
            EnsureVfxSingleton();
            EnsurePotPulseInScene();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Pithox Polish",
                "Visuals polished.\n\nNow do these:\n1) Window > AI > Navigation > Bake (the new floor is Navigation Static)\n2) Press Play.",
                "OK");
        }

        struct Materials
        {
            public Material Player, NormalEnemy, FastEnemy, ProjectileEnemy;
            public Material Pot, PotLiquid, Tomb, Floor, Pillar;
            public Material Slash, Beam, Orbit, Pulse;
        }

        static Materials GenerateAllMaterials()
        {
            return new Materials
            {
                Player = MakeMat("mat_player", new Color(0.12f, 0.45f, 0.55f), new Color(0.3f, 0.8f, 1f) * 0.5f, 0.5f),
                NormalEnemy = MakeMat("mat_normal_enemy", new Color(0.5f, 0.1f, 0.1f), new Color(1f, 0.2f, 0.2f) * 0.4f, 0.3f),
                FastEnemy = MakeMat("mat_fast_enemy", new Color(0.6f, 0.5f, 0.1f), new Color(1f, 0.9f, 0.2f) * 0.6f, 0.3f),
                ProjectileEnemy = MakeMat("mat_projectile_enemy", new Color(0.4f, 0.15f, 0.5f), new Color(0.8f, 0.3f, 1f) * 0.5f, 0.3f),
                Pot = MakeMat("mat_pot", new Color(0.15f, 0.13f, 0.15f), Color.black, 0.4f),
                PotLiquid = MakeMat("mat_pot_liquid", new Color(0.35f, 0.1f, 0.5f), new Color(0.8f, 0.2f, 1f) * 1.2f, 0.7f),
                Tomb = MakeMat("mat_tomb", new Color(0.6f, 0.5f, 0.2f), new Color(1f, 0.9f, 0.4f) * 0.4f, 0.3f),
                Floor = MakeMat("mat_floor", new Color(0.07f, 0.06f, 0.08f), Color.black, 0.2f),
                Pillar = MakeMat("mat_pillar", new Color(0.1f, 0.09f, 0.11f), Color.black, 0.3f),
                Slash = MakeMat("mat_slash", new Color(1f, 1f, 1f), new Color(1f, 0.95f, 0.8f) * 1.5f, 0.6f),
                Beam = MakeMat("mat_beam", new Color(0.6f, 0.9f, 1f), new Color(0.4f, 0.8f, 1f) * 2f, 0.6f),
                Orbit = MakeMat("mat_orbit", new Color(1f, 0.6f, 0.2f), new Color(1f, 0.5f, 0.1f) * 1.2f, 0.5f),
                Pulse = MakeMat("mat_pulse", new Color(1f, 1f, 1f), new Color(1f, 0.95f, 0.9f) * 1.2f, 0.4f),
            };
        }

        static Material MakeMat(string name, Color baseColor, Color emission, float smoothness)
        {
            string path = $"{MatDir}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (m == null)
            {
                m = new Material(urpLit);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = urpLit;
            }

            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);

            bool emit = emission.maxColorComponent > 0.01f;
            m.SetColor("_EmissionColor", emission);
            if (emit)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(m);
            return m;
        }

        static void ApplyEnemyPrefabs(Materials mats)
        {
            ApplyPrefabRigidbodyAndMaterial("NormalEnemy", mats.NormalEnemy);
            ApplyPrefabRigidbodyAndMaterial("FastEnemy", mats.FastEnemy);
            ApplyPrefabRigidbodyAndMaterial("ProjectileEnemy", mats.ProjectileEnemy);
        }

        static void ApplyPrefabRigidbodyAndMaterial(string prefabName, Material material)
        {
            GameObject prefab = LoadPrefabByName(prefabName);
            if (prefab == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            if (root.GetComponent<HitFlash>() == null)
                root.AddComponent<HitFlash>();

            ApplyMaterialRecursive(root.transform, material);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ApplySkillPrefabs(Materials mats)
        {
            ApplyMaterialToPrefab("PF_Slash", mats.Slash, expandSlashCollider: true, slashLifetimeNote: true);
            ApplyMaterialToPrefab("PF_Beam", mats.Beam);
            ApplyMaterialToPrefab("PF_OrbitBalls", mats.Orbit);
            ApplyMaterialToPrefab("PF_PulsePassive", mats.Pulse);
        }

        static void ApplyMaterialToPrefab(string prefabName, Material material, bool expandSlashCollider = false, bool slashLifetimeNote = false)
        {
            GameObject prefab = LoadPrefabByName(prefabName);
            if (prefab == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            ApplyMaterialRecursive(root.transform, material);

            if (expandSlashCollider)
            {
                BoxCollider box = root.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Vector3 sz = box.size;
                    sz.y = Mathf.Max(sz.y, 1.5f);
                    box.size = sz;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ApplyPotPrefab(Materials mats)
        {
            GameObject prefab = LoadPrefabByName("Pot");
            if (prefab == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("liquid")) r.sharedMaterial = mats.PotLiquid;
                else r.sharedMaterial = mats.Pot;
            }

            Light potLight = root.GetComponentInChildren<Light>();
            if (potLight == null)
            {
                GameObject lightGO = new GameObject("PotLight");
                lightGO.transform.SetParent(root.transform, false);
                lightGO.transform.localPosition = new Vector3(0f, 1f, 0f);
                potLight = lightGO.AddComponent<Light>();
                potLight.type = LightType.Point;
                potLight.color = new Color(0.7f, 0.3f, 1f);
                potLight.intensity = 4f;
                potLight.range = 8f;
            }

            PotPulse pulse = root.GetComponent<PotPulse>();
            if (pulse == null) pulse = root.AddComponent<PotPulse>();

            SerializedObject so = new SerializedObject(pulse);
            so.FindProperty("potLight").objectReferenceValue = potLight;

            Renderer liquidRend = null;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                if (r.name.ToLowerInvariant().Contains("liquid")) { liquidRend = r; break; }
            if (liquidRend != null) so.FindProperty("liquidRenderer").objectReferenceValue = liquidRend;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ApplyTombPrefab(Materials mats)
        {
            GameObject prefab = LoadPrefabByName("Tomb");
            if (prefab == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            ApplyMaterialRecursive(root.transform, mats.Tomb);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ApplyToScenePlayerAndProps(Materials mats)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                ApplyMaterialToVisualOnly(player.transform, mats.Player);

            GameObject pot = GameObject.Find("Pot");
            if (pot != null)
            {
                foreach (Renderer r in pot.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.name.ToLowerInvariant();
                    if (n.Contains("liquid")) r.sharedMaterial = mats.PotLiquid;
                    else r.sharedMaterial = mats.Pot;
                }

                if (pot.GetComponentInChildren<Light>() == null)
                {
                    GameObject lightGO = new GameObject("PotLight");
                    lightGO.transform.SetParent(pot.transform, false);
                    lightGO.transform.localPosition = new Vector3(0f, 1f, 0f);
                    Light l = lightGO.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(0.7f, 0.3f, 1f);
                    l.intensity = 4f;
                    l.range = 8f;
                }
            }
        }

        static void ApplyMaterialRecursive(Transform root, Material material)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = material;
                r.sharedMaterials = arr;
            }
        }

        static void ApplyMaterialToVisualOnly(Transform root, Material material)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponent<MeshFilter>() == null) continue;
                Material[] arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = material;
                r.sharedMaterials = arr;
            }
        }

        static void EnsureLightingAndFog()
        {
            Light dl = null;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { dl = l; break; }

            if (dl == null)
            {
                GameObject go = new GameObject("DirectionalLight");
                Undo.RegisterCreatedObjectUndo(go, "Create Directional Light");
                dl = go.AddComponent<Light>();
                dl.type = LightType.Directional;
            }

            dl.color = new Color(1f, 0.85f, 0.7f);
            dl.intensity = 1.2f;
            dl.shadows = LightShadows.Soft;
            dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.05f, 0.04f, 0.10f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.05f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.03f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.015f;
            RenderSettings.fogColor = new Color(0.08f, 0.05f, 0.12f);

            RenderSettings.sun = dl;
        }

        static void EnsureCameraSolidColor()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.03f, 0.10f);
        }

        static void EnsureFloorAndProps(Materials mats)
        {
            GameObject floor = GameObject.Find("PithoxFloor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
                floor.name = "PithoxFloor";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(8f, 1f, 8f);
                GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
            }
            floor.GetComponent<Renderer>().sharedMaterial = mats.Floor;

            Transform pillarsRoot = GameObject.Find("PithoxPillars")?.transform;
            if (pillarsRoot == null)
            {
                GameObject root = new GameObject("PithoxPillars");
                Undo.RegisterCreatedObjectUndo(root, "Create Pillars");
                pillarsRoot = root.transform;

                int count = 8;
                float radius = 32f;
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2f / count;
                    GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.name = $"Pillar{i}";
                    p.transform.SetParent(pillarsRoot, false);
                    p.transform.localPosition = new Vector3(Mathf.Cos(a) * radius, 2f, Mathf.Sin(a) * radius);
                    p.transform.localScale = new Vector3(1.2f, 4f, 1.2f);
                    p.GetComponent<Renderer>().sharedMaterial = mats.Pillar;
                    GameObjectUtility.SetStaticEditorFlags(p, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
                }
            }

            Transform tombsRoot = GameObject.Find("PithoxTombstones")?.transform;
            if (tombsRoot == null)
            {
                GameObject root = new GameObject("PithoxTombstones");
                Undo.RegisterCreatedObjectUndo(root, "Create Tombstones");
                tombsRoot = root.transform;

                Random.InitState(7842);
                for (int i = 0; i < 10; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Random.Range(8f, 26f);
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    GameObject parent = new GameObject($"Tombstone{i}");
                    parent.transform.SetParent(tombsRoot, false);
                    parent.transform.position = pos;
                    parent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stone.transform.SetParent(parent.transform, false);
                    stone.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    stone.transform.localScale = new Vector3(0.6f, 1f, 0.15f);
                    stone.GetComponent<Renderer>().sharedMaterial = mats.Pillar;

                    GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    baseGO.transform.SetParent(parent.transform, false);
                    baseGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    baseGO.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
                    baseGO.GetComponent<Renderer>().sharedMaterial = mats.Pillar;
                }
            }
        }

        static void EnsureVfxSingleton()
        {
            HitVFX existing = Object.FindFirstObjectByType<HitVFX>();
            if (existing != null) return;

            GameObject go = new GameObject("_VFX");
            Undo.RegisterCreatedObjectUndo(go, "Create VFX");
            go.AddComponent<HitVFX>();
        }

        static void EnsurePotPulseInScene()
        {
            GameObject pot = GameObject.Find("Pot");
            if (pot == null) return;

            PotPulse pulse = pot.GetComponent<PotPulse>();
            if (pulse == null) pulse = pot.AddComponent<PotPulse>();

            Light potLight = pot.GetComponentInChildren<Light>();
            Renderer liquid = null;
            foreach (Renderer r in pot.GetComponentsInChildren<Renderer>(true))
                if (r.name.ToLowerInvariant().Contains("liquid")) { liquid = r; break; }

            SerializedObject so = new SerializedObject(pulse);
            if (potLight != null) so.FindProperty("potLight").objectReferenceValue = potLight;
            if (liquid != null) so.FindProperty("liquidRenderer").objectReferenceValue = liquid;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject LoadPrefabByName(string nameWithoutExtension)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameWithoutExtension} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == nameWithoutExtension)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }
    }
}
#endif
