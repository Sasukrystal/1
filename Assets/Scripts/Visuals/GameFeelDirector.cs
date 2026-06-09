using UnityEngine;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public class GameFeelDirector : MonoBehaviour
    {
        public static GameFeelDirector Instance { get; private set; }

        private const string HudRootName = "RunHud";

        private Text hpText;
        private Text runText;
        private Text objectiveText;
        private Text combatText;
        private Image hpFill;
        private Image vignette;
        private Canvas canvas;
        private Camera mainCamera;
        private PlayerStats playerStats;
        private global::Player legacyPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyAtmosphere();
            EnsureHud();
        }

        private void Start()
        {
            ApplyAtmosphere();
            EnsureHud();
            BeautifyCharacters();
        }

        private void Update()
        {
            CacheReferences();
            RefreshHud();
            BeautifyCharacters();
        }

        private void ApplyAtmosphere()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.21f, 0.28f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.08f, 0.1f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.03f, 0.035f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.055f, 0.065f, 0.085f, 1f);
            RenderSettings.fogDensity = 0.014f;

            mainCamera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.045f, 0.055f, 0.075f, 1f);
                mainCamera.fieldOfView = 48f;
            }

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                GameObject sunObject = GameObject.Find("Directional Light");
                sun = sunObject != null ? sunObject.GetComponent<Light>() : null;
            }

            if (sun != null)
            {
                sun.name = "Key Light";
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.82f, 0.58f, 1f);
                sun.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 18f);
                RenderSettings.sun = sun;
            }

            EnsureAccentLight("Moon Fill Light", new Vector3(-5f, 8f, 2f), new Color(0.25f, 0.42f, 0.9f, 1f), 0.55f);
            EnsureAccentLight("Warm Rim Light", new Vector3(5f, 5f, -3f), new Color(1f, 0.45f, 0.24f, 1f), 0.35f);
        }

        private static void EnsureAccentLight(string name, Vector3 position, Color color, float intensity)
        {
            GameObject lightObject = GameObject.Find(name);
            Light light = lightObject != null ? lightObject.GetComponent<Light>() : null;
            if (light == null)
            {
                lightObject = new GameObject(name);
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.transform.position = position;
            light.transform.LookAt(Vector3.zero);
        }

        private void EnsureHud()
        {
            canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existing = canvas.transform.Find(HudRootName);
            if (existing != null)
            {
                CacheHud(existing);
                if (objectiveText == null || combatText == null)
                {
                    EnsureObjectivePanel(existing);
                    CacheHud(existing);
                }

                return;
            }

            GameObject root = new GameObject(HudRootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject vignetteObject = new GameObject("SoftVignette", typeof(RectTransform), typeof(Image));
            vignetteObject.transform.SetParent(root.transform, false);
            RectTransform vignetteRect = vignetteObject.GetComponent<RectTransform>();
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;
            vignette = vignetteObject.GetComponent<Image>();
            vignette.color = new Color(0f, 0f, 0f, 0.16f);
            vignette.raycastTarget = false;

            GameObject panel = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(360f, 92f);
            panelRect.anchoredPosition = new Vector2(18f, -18f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.055f, 0.82f);

            GameObject hpBack = new GameObject("HpBack", typeof(RectTransform), typeof(Image));
            hpBack.transform.SetParent(panel.transform, false);
            RectTransform hpBackRect = hpBack.GetComponent<RectTransform>();
            hpBackRect.anchorMin = new Vector2(0f, 1f);
            hpBackRect.anchorMax = new Vector2(1f, 1f);
            hpBackRect.pivot = new Vector2(0f, 1f);
            hpBackRect.offsetMin = new Vector2(16f, -42f);
            hpBackRect.offsetMax = new Vector2(-16f, -18f);
            hpBack.GetComponent<Image>().color = new Color(0.16f, 0.025f, 0.035f, 0.95f);

            GameObject hpFillObject = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFillObject.transform.SetParent(hpBack.transform, false);
            RectTransform hpFillRect = hpFillObject.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = new Vector2(3f, 3f);
            hpFillRect.offsetMax = new Vector2(-3f, -3f);
            hpFill = hpFillObject.GetComponent<Image>();
            hpFill.color = new Color(0.78f, 0.08f, 0.11f, 1f);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;

            hpText = CreateHudText(panel.transform, "HpText", -16f, 22f, 15, FontStyle.Bold);
            runText = CreateHudText(panel.transform, "RunText", -50f, 28f, 14, FontStyle.Normal);

            EnsureObjectivePanel(root.transform);
            CacheHud(root.transform);
        }

        private void EnsureObjectivePanel(Transform root)
        {
            if (root == null || root.Find("ObjectivePanel") != null)
            {
                return;
            }

            GameObject objectivePanel = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(Image));
            objectivePanel.transform.SetParent(root, false);
            RectTransform objectiveRect = objectivePanel.GetComponent<RectTransform>();
            objectiveRect.anchorMin = new Vector2(1f, 1f);
            objectiveRect.anchorMax = new Vector2(1f, 1f);
            objectiveRect.pivot = new Vector2(1f, 1f);
            objectiveRect.sizeDelta = new Vector2(330f, 116f);
            objectiveRect.anchoredPosition = new Vector2(-18f, -18f);
            objectivePanel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 0.82f);

            objectiveText = CreatePanelText(objectivePanel.transform, "ObjectiveText", new Vector2(14f, 48f), new Vector2(-14f, -12f), 14, FontStyle.Bold);
            combatText = CreatePanelText(objectivePanel.transform, "CombatText", new Vector2(14f, 12f), new Vector2(-14f, -58f), 13, FontStyle.Normal);
        }

        private static Text CreateHudText(Transform parent, string name, float topOffset, float height, int fontSize, FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topOffset);
            rect.sizeDelta = new Vector2(-32f, height);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Text CreatePanelText(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = new Color(0.92f, 0.96f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void CacheHud(Transform root)
        {
            hpText = root.Find("StatusPanel/HpText")?.GetComponent<Text>();
            runText = root.Find("StatusPanel/RunText")?.GetComponent<Text>();
            objectiveText = root.Find("ObjectivePanel/ObjectiveText")?.GetComponent<Text>();
            combatText = root.Find("ObjectivePanel/CombatText")?.GetComponent<Text>();
            hpFill = root.Find("StatusPanel/HpBack/HpFill")?.GetComponent<Image>();
            vignette = root.Find("SoftVignette")?.GetComponent<Image>();
        }

        private void CacheReferences()
        {
            if (playerStats == null)
            {
                playerStats = Object.FindObjectOfType<PlayerStats>(true);
            }

            if (legacyPlayer == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    legacyPlayer = playerObject.GetComponent<global::Player>();
                }
            }
        }

        private void RefreshHud()
        {
            if (hpText == null || runText == null)
            {
                return;
            }

            int currentHp = playerStats != null ? playerStats.CurrentHP : 0;
            int maxHp = playerStats != null ? Mathf.Max(1, playerStats.MaxHP) : 1;
            int coins = playerStats != null ? playerStats.CoinAmount : (legacyPlayer != null ? legacyPlayer.CoinAmount : 0);
            int rooms = Object.FindObjectsOfType<Room>(true).Length;
            int enemies = Object.FindObjectsOfType<EnemyStats>(true).Length;
            int unclearedRooms = 0;
            Room[] roomObjects = Object.FindObjectsOfType<Room>(true);
            for (int i = 0; i < roomObjects.Length; i++)
            {
                if (roomObjects[i] != null && !roomObjects[i].isCleared)
                {
                    unclearedRooms++;
                }
            }

            hpText.text = string.Format("HP {0}/{1}    Coins {2}", currentHp, maxHp, coins);
            runText.text = string.Format("Rooms {0}    Uncleared {1}    Hostiles {2}    B Backpack    E Character    F Loot", rooms, unclearedRooms, enemies);

            if (objectiveText != null)
            {
                objectiveText.text = enemies > 0
                    ? string.Format("目标：清理当前房间\n敌人剩余：{0}", enemies)
                    : "目标：探索下一间房\n清理房间会掉落战利品";
            }

            if (combatText != null)
            {
                string status = currentHp <= maxHp * 0.35f ? "危险：优先走位和拾取补给" : "状态：可推进";
                combatText.text = string.Format("{0}\n左键攻击  WASD移动  鼠标朝向", status);
            }

            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(currentHp / (float)maxHp);
            }

            if (vignette != null)
            {
                float danger = 1f - Mathf.Clamp01(currentHp / (float)maxHp);
                vignette.color = new Color(0.08f, 0f, 0f, Mathf.Lerp(0.12f, 0.32f, danger));
            }
        }

        private static void BeautifyCharacters()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null && playerObject.GetComponent<RuntimeCharacterVisual>() == null)
            {
                playerObject.AddComponent<RuntimeCharacterVisual>().Configure(RuntimeCharacterVisualStyle.Player);
            }

            if (playerObject != null && playerObject.GetComponent<ProceduralCharacterAnimator>() == null)
            {
                playerObject.AddComponent<ProceduralCharacterAnimator>().Configure(RuntimeCharacterVisualStyle.Player);
            }

            if (playerObject != null && playerObject.GetComponent<FloatingHealthBar>() == null)
            {
                playerObject.AddComponent<FloatingHealthBar>();
            }

            EnemyStats[] enemies = Object.FindObjectsOfType<EnemyStats>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyStats enemy = enemies[i];
                if (enemy != null && enemy.GetComponent<RuntimeCharacterVisual>() == null)
                {
                    enemy.gameObject.AddComponent<RuntimeCharacterVisual>().Configure(RuntimeCharacterVisualStyle.Enemy);
                }

                if (enemy != null && enemy.GetComponent<ProceduralCharacterAnimator>() == null)
                {
                    enemy.gameObject.AddComponent<ProceduralCharacterAnimator>().Configure(RuntimeCharacterVisualStyle.Enemy);
                }

                if (enemy != null && enemy.GetComponent<FloatingHealthBar>() == null)
                {
                    enemy.gameObject.AddComponent<FloatingHealthBar>();
                }

                if (enemy != null && enemy.GetComponent<ModernRogue.SmoothWorldHealthBar>() == null)
                {
                    enemy.gameObject.AddComponent<ModernRogue.SmoothWorldHealthBar>();
                }
            }
        }
    }
}
