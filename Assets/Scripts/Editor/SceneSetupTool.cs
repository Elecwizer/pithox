#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Pithox.Combat;
using Pithox.Game;
using Pithox.Player;
using Pithox.Skills;

namespace Pithox.EditorTools
{
    public static class SceneSetupTool
    {
        const string MenuRoot = "Pithox/";

        [MenuItem(MenuRoot + "Setup Scene (HUD + Managers + Player Wiring)")]
        public static void SetupScene()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Pithox Setup", "No GameObject with tag 'Player' found in the active scene. Tag your player and try again.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Pithox Setup");

            PlayerStats stats = EnsureComponent<PlayerStats>(player);
            WirePlayerComponents(player, stats);

            EnsureNavMeshAgentOnEnemyPrefabs();

            GameObject managersGO = EnsureManagersObject();
            ScoreManager score = EnsureComponent<ScoreManager>(managersGO);
            LevelManager level = EnsureComponent<LevelManager>(managersGO);
            HUD hud = EnsureComponent<HUD>(managersGO);
            UpgradePanel upgrade = EnsureComponent<UpgradePanel>(managersGO);

            SetField(score, "playerStats", stats);
            SetField(level, "scoreManager", score);

            (Canvas canvas, TMP_Text scoreText, TMP_Text levelText, TMP_Text streakText, Image streakBar,
             GameObject panelRoot, Button[] buttons, TMP_Text[] labels) = BuildOrFindUI();

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            (Image healthFill, TMP_Text healthText) = BuildOrFindHealthBar(canvas);

            SetField(hud, "scoreManager", score);
            SetField(hud, "levelManager", level);
            SetField(hud, "playerHealth", playerHealth);
            SetField(hud, "scoreText", scoreText);
            SetField(hud, "levelText", levelText);
            SetField(hud, "streakText", streakText);
            SetField(hud, "streakTimerFill", streakBar);
            SetField(hud, "healthFill", healthFill);
            SetField(hud, "healthText", healthText);

            SetField(upgrade, "playerStats", stats);
            SetField(upgrade, "panelRoot", panelRoot);
            SetField(upgrade, "choiceButtons", buttons);
            SetField(upgrade, "choiceLabels", labels);

            EnsurePostProcessing();
            EnsureSmoothCameraWiring(player);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            EditorUtility.DisplayDialog(
                "Pithox Setup",
                "Setup complete.\n\n" +
                "Now do these manual steps:\n" +
                "1. Select your arena floor → mark it 'Navigation Static' → Window > AI > Navigation > Bake.\n" +
                "2. Make sure tomb prefabs have tag 'Tomb' and the Pot is wired in PlayerTombCarry.\n" +
                "3. Create an 'Enemy' layer and assign it to enemy prefabs; pick that layer in Player's DamageAura.enemyMask.\n" +
                "4. Press Play.",
                "OK");
        }

        static void WirePlayerComponents(GameObject player, PlayerStats stats)
        {
            PlayerMovement move = player.GetComponent<PlayerMovement>();
            if (move != null) SetField(move, "stats", stats);

            PlayerTombCarry carry = player.GetComponent<PlayerTombCarry>();
            if (carry != null)
            {
                SetField(carry, "stats", stats);
                Transform pot = FindPotInScene();
                if (pot != null) SetField(carry, "pot", pot);
            }

            Transform slashPoint = EnsureChildPoint(player.transform, "SlashPoint", new Vector3(0f, 1f, 1f));
            Transform orbitPoint = EnsureChildPoint(player.transform, "OrbitPoint", new Vector3(0f, 1f, 0f));
            Transform beamPoint = EnsureChildPoint(player.transform, "BeamPoint", new Vector3(0f, 1f, 1f));

            GameObject slashPrefab = LoadPrefab("PF_Slash");
            GameObject orbitPrefab = LoadPrefab("PF_OrbitBalls");
            GameObject beamPrefab = LoadPrefab("PF_Beam");

            PlayerCombatController combat = EnsureComponent<PlayerCombatController>(player);
            SetField(combat, "tombCarry", carry);
            SetField(combat, "slashPoint", slashPoint);
            SetField(combat, "slashPrefab", slashPrefab);

            DamageAura aura = EnsureComponent<DamageAura>(player);
            SetField(aura, "stats", stats);
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                SetField(aura, "enemyMask", (LayerMask)(1 << enemyLayer));

            OrbitAutoCaster orbitAuto = EnsureComponent<OrbitAutoCaster>(player);
            SetField(orbitAuto, "stats", stats);
            SetField(orbitAuto, "orbitPoint", orbitPoint);
            SetField(orbitAuto, "orbitPrefab", orbitPrefab);

            BeamAutoCaster beamAuto = EnsureComponent<BeamAutoCaster>(player);
            SetField(beamAuto, "stats", stats);
            SetField(beamAuto, "beamPoint", beamPoint);
            SetField(beamAuto, "beamPrefab", beamPrefab);
        }

        static GameObject EnsureManagersObject()
        {
            GameObject existing = GameObject.Find("_GameManagers");
            if (existing != null) return existing;

            GameObject go = new GameObject("_GameManagers");
            Undo.RegisterCreatedObjectUndo(go, "Create _GameManagers");
            return go;
        }

        static (Canvas, TMP_Text, TMP_Text, TMP_Text, Image, GameObject, Button[], TMP_Text[]) BuildOrFindUI()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD Canvas");
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            Transform hudRoot = canvas.transform.Find("HUD");
            if (hudRoot == null)
            {
                GameObject hudGO = new GameObject("HUD", typeof(RectTransform));
                hudGO.transform.SetParent(canvas.transform, false);
                StretchToFill(hudGO.GetComponent<RectTransform>());
                hudRoot = hudGO.transform;
            }

            TMP_Text scoreText = EnsureText(hudRoot, "ScoreText", new Vector2(1, 1), new Vector2(-20, -20), TextAlignmentOptions.TopRight, "Score: 0", 48, new Vector2(400, 60));
            TMP_Text levelText = EnsureText(hudRoot, "LevelText", new Vector2(0, 1), new Vector2(20, -20), TextAlignmentOptions.TopLeft, "Lv 1", 48, new Vector2(200, 60));
            TMP_Text streakText = EnsureText(hudRoot, "StreakText", new Vector2(0.5f, 0), new Vector2(0, 80), TextAlignmentOptions.Center, "", 56, new Vector2(300, 70));

            Image streakBar = EnsureFilledImage(hudRoot, "StreakBar", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(400, 16));

            (GameObject panelRoot, Button[] buttons, TMP_Text[] labels) = EnsureUpgradePanel(canvas.transform);

            return (canvas, scoreText, levelText, streakText, streakBar, panelRoot, buttons, labels);
        }

        static (GameObject, Button[], TMP_Text[]) EnsureUpgradePanel(Transform canvasRoot)
        {
            Transform existing = canvasRoot.Find("UpgradePanel");
            GameObject panelRoot;
            if (existing != null)
            {
                panelRoot = existing.gameObject;
            }
            else
            {
                panelRoot = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
                panelRoot.transform.SetParent(canvasRoot, false);
                StretchToFill(panelRoot.GetComponent<RectTransform>());
                Image bg = panelRoot.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.75f);
            }

            GameObject layoutGO = panelRoot.transform.Find("Choices")?.gameObject;
            if (layoutGO == null)
            {
                layoutGO = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                layoutGO.transform.SetParent(panelRoot.transform, false);
                RectTransform lrt = layoutGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.5f, 0.5f);
                lrt.anchorMax = new Vector2(0.5f, 0.5f);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.sizeDelta = new Vector2(1200, 300);
                HorizontalLayoutGroup hlg = layoutGO.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 30;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }

            Button[] buttons = new Button[3];
            TMP_Text[] labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                Transform btnT = layoutGO.transform.Find($"Choice{i + 1}");
                GameObject btnGO;
                if (btnT == null)
                {
                    btnGO = new GameObject($"Choice{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGO.transform.SetParent(layoutGO.transform, false);
                    Image img = btnGO.GetComponent<Image>();
                    img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                }
                else
                {
                    btnGO = btnT.gameObject;
                }

                buttons[i] = btnGO.GetComponent<Button>();

                Transform lblT = btnGO.transform.Find("Label");
                GameObject lblGO;
                if (lblT == null)
                {
                    lblGO = new GameObject("Label", typeof(RectTransform));
                    lblGO.transform.SetParent(btnGO.transform, false);
                    StretchToFill(lblGO.GetComponent<RectTransform>());
                }
                else
                {
                    lblGO = lblT.gameObject;
                }

                TextMeshProUGUI tmp = lblGO.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = lblGO.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 36;
                tmp.color = Color.white;
                tmp.text = $"Choice {i + 1}";
                labels[i] = tmp;
            }

            panelRoot.SetActive(false);
            return (panelRoot, buttons, labels);
        }

        static TMP_Text EnsureText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, TextAlignmentOptions align, string text, int size, Vector2 sizeDelta)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = align;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.text = text;
            return tmp;
        }

        static Image EnsureFilledImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = go.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.color = new Color(1f, 0.6f, 0.1f, 1f);
            img.fillAmount = 1f;
            return img;
        }

        static void StretchToFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Transform EnsureChildPoint(Transform parent, string name, Vector3 localPos)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go.transform;
        }

        static Transform FindPotInScene()
        {
            GameObject potObj = GameObject.Find("Pot");
            return potObj != null ? potObj.transform : null;
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            if (existing != null) return existing;
            return Undo.AddComponent<T>(go);
        }

        static void EnsureNavMeshAgentOnEnemyPrefabs()
        {
            string[] prefabs = { "FastEnemy", "NormalEnemy", "ProjectileEnemy" };
            foreach (string p in prefabs)
            {
                GameObject prefab = LoadPrefab(p);
                if (prefab == null) continue;

                string path = AssetDatabase.GetAssetPath(prefab);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                if (root.GetComponent<NavMeshAgent>() == null)
                {
                    NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                    agent.radius = 0.5f;
                    agent.height = 2f;
                    agent.stoppingDistance = 1f;
                    agent.avoidancePriority = 50;
                    changed = true;
                }

                if (root.GetComponent<HitFlash>() == null)
                {
                    root.AddComponent<HitFlash>();
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static (Image, TMP_Text) BuildOrFindHealthBar(Canvas canvas)
        {
            Transform hudRoot = canvas.transform.Find("HUD");
            if (hudRoot == null) return (null, null);

            Transform existing = hudRoot.Find("HealthBar");
            GameObject barGO;
            if (existing == null)
            {
                barGO = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
                barGO.transform.SetParent(hudRoot, false);
                Image bg = barGO.GetComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                RectTransform brt = barGO.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0f);
                brt.anchorMax = new Vector2(0.5f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(0f, 14f);
                brt.sizeDelta = new Vector2(420, 22);
            }
            else
            {
                barGO = existing.gameObject;
            }

            Transform fillT = barGO.transform.Find("Fill");
            GameObject fillGO;
            if (fillT == null)
            {
                fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(barGO.transform, false);
                StretchToFill(fillGO.GetComponent<RectTransform>());
                RectTransform frt = fillGO.GetComponent<RectTransform>();
                frt.offsetMin = new Vector2(2, 2);
                frt.offsetMax = new Vector2(-2, -2);
            }
            else
            {
                fillGO = fillT.gameObject;
            }

            Image fillImg = fillGO.GetComponent<Image>();
            fillImg.color = new Color(0.85f, 0.15f, 0.2f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            Transform txtT = barGO.transform.Find("HealthText");
            GameObject txtGO;
            if (txtT == null)
            {
                txtGO = new GameObject("HealthText", typeof(RectTransform));
                txtGO.transform.SetParent(barGO.transform, false);
                StretchToFill(txtGO.GetComponent<RectTransform>());
            }
            else
            {
                txtGO = txtT.gameObject;
            }

            TextMeshProUGUI tmp = txtGO.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.text = "100 / 100";

            return (fillImg, tmp);
        }

        static void EnsurePostProcessing()
        {
            GameObject volumeGO = GameObject.Find("PithoxVolume");
            if (volumeGO == null)
            {
                volumeGO = new GameObject("PithoxVolume", typeof(Volume));
                Undo.RegisterCreatedObjectUndo(volumeGO, "Create Volume");
            }

            Volume volume = volumeGO.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;

            string profilePath = "Assets/Settings/PithoxPostFX.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                System.IO.Directory.CreateDirectory("Assets/Settings");
                AssetDatabase.CreateAsset(profile, profilePath);

                Bloom bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.6f);
                bloom.threshold.Override(0.95f);
                bloom.scatter.Override(0.7f);

                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.32f);
                vignette.smoothness.Override(0.45f);
                vignette.color.Override(new Color(0.05f, 0.0f, 0.05f));

                ColorAdjustments color = profile.Add<ColorAdjustments>(true);
                color.contrast.Override(8f);
                color.saturation.Override(10f);
                color.postExposure.Override(0.1f);

                AssetDatabase.SaveAssets();
            }

            volume.sharedProfile = profile;

            Camera cam = Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData cd = cam.GetComponent<UniversalAdditionalCameraData>();
                if (cd == null) cd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                cd.renderPostProcessing = true;
            }
        }

        static void EnsureSmoothCameraWiring(GameObject player)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            SmoothMidCamera follower = cam.GetComponent<SmoothMidCamera>();
            if (follower == null) follower = Undo.AddComponent<SmoothMidCamera>(cam.gameObject);

            SerializedObject so = new SerializedObject(follower);
            so.FindProperty("player").objectReferenceValue = player.transform;
            Transform pot = FindPotInScene();
            if (pot != null) so.FindProperty("pot").objectReferenceValue = pot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject LoadPrefab(string nameWithoutExtension)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameWithoutExtension} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == nameWithoutExtension)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        static void SetField(Object target, string fieldName, object value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneSetupTool] Field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            AssignProp(prop, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignProp(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = value as Object;
                    break;
                case SerializedPropertyType.Integer:
                    if (value is int i) prop.intValue = i;
                    else if (value is LayerMask lm) prop.intValue = lm.value;
                    break;
                case SerializedPropertyType.LayerMask:
                    if (value is LayerMask mask) prop.intValue = mask.value;
                    else if (value is int im) prop.intValue = im;
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = (string)value;
                    break;
                default:
                    if (prop.isArray && value is System.Collections.IEnumerable enumerable)
                    {
                        List<Object> list = new List<Object>();
                        foreach (object item in enumerable)
                            if (item is Object uo) list.Add(uo);
                        prop.arraySize = list.Count;
                        for (int idx = 0; idx < list.Count; idx++)
                            prop.GetArrayElementAtIndex(idx).objectReferenceValue = list[idx];
                    }
                    break;
            }
        }
    }

}
#endif
