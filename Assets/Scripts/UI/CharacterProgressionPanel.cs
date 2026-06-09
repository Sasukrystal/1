using UnityEngine;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class CharacterProgressionPanel : MonoBehaviour
    {
        public static CharacterProgressionPanel Instance { get; private set; }

        private GameObject root;
        private Text statText;
        private Text upgradeText;
        private Canvas canvas;
        private bool visible;
        private int strengthLevel;
        private int agilityLevel;
        private int soulShards;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsurePanel();
            HidePanelImmediate();
        }

        private void Update()
        {
            if (visible)
            {
                Refresh();
            }
        }

        public void TogglePanel()
        {
            if (visible)
            {
                HidePanelImmediate();
            }
            else
            {
                ShowPanel();
            }
        }

        public void ShowPanel()
        {
            EnsurePanel();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            visible = true;
            Refresh();
        }

        public void UpgradeStat(string statId)
        {
            if (soulShards <= 0)
            {
                upgradeText.text = "灵魂碎片不足。";
                return;
            }

            soulShards--;
            if (statId == "strength")
            {
                strengthLevel++;
            }
            else if (statId == "agility")
            {
                agilityLevel++;
            }

            Refresh();
        }

        public void AddSoulShards(int amount)
        {
            soulShards += Mathf.Max(0, amount);
        }

        private void EnsurePanel()
        {
            if (root != null)
            {
                return;
            }

            canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            root = new GameObject("CharacterProgressionPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(780f, 460f);
            root.GetComponent<Image>().color = new Color(0.045f, 0.05f, 0.07f, 0.96f);

            Text title = CreateText(root.transform, "Title", new Vector2(22f, -56f), new Vector2(-22f, -14f), 24, FontStyle.Bold);
            title.text = "人物养成";

            GameObject statsPanel = CreatePanel(root.transform, "StatsPanel", new Vector2(0f, 0f), new Vector2(0.58f, 0.86f), new Vector2(22f, 22f), new Vector2(-10f, -62f));
            GameObject upgradePanel = CreatePanel(root.transform, "UpgradePanel", new Vector2(0.58f, 0f), new Vector2(1f, 0.86f), new Vector2(10f, 22f), new Vector2(-22f, -62f));

            statText = CreateText(statsPanel.transform, "StatText", new Vector2(18f, 18f), new Vector2(-18f, -18f), 16, FontStyle.Normal);
            upgradeText = CreateText(upgradePanel.transform, "UpgradeText", new Vector2(18f, 120f), new Vector2(-18f, -18f), 15, FontStyle.Normal);
            CreateButton(upgradePanel.transform, "StrengthButton", "强化力量", new Vector2(18f, -72f), () => UpgradeStat("strength"));
            CreateButton(upgradePanel.transform, "AgilityButton", "强化敏捷", new Vector2(18f, -128f), () => UpgradeStat("agility"));
            CreateButton(upgradePanel.transform, "TalentButton", "天赋树预留", new Vector2(18f, -184f), () => { upgradeText.text = "天赋树接口已预留。"; });
        }

        private void Refresh()
        {
            PlayerStats stats = Object.FindObjectOfType<PlayerStats>(true);
            PlayerController controller = Object.FindObjectOfType<PlayerController>(true);
            PlayerAttack attack = Object.FindObjectOfType<PlayerAttack>(true);

            int baseAtk = stats != null ? stats.ATK : 0;
            int defense = stats != null ? stats.DEF : 0;
            int hp = stats != null ? stats.CurrentHP : 0;
            int maxHp = stats != null ? stats.MaxHP : 0;
            float moveSpeed = GetPrivateFloat(controller, "moveSpeed", 0f);
            float attackCooldown = GetPrivateFloat(attack, "attackCooldown", 0.3f);
            float attackSpeed = attackCooldown > 0f ? 1f / attackCooldown : 0f;
            float critChance = Mathf.Clamp(0.05f + agilityLevel * 0.015f, 0f, 0.65f);

            statText.text = string.Format(
                "生命：{0}/{1}\n基础 ATK：{2}\n装备/成长 ATK 加成：+{3}\n防御：{4}\n暴击率：{5:0}%\n移动速度：{6:0.0}\n攻击速度：{7:0.00}/s\n\n成长等级\n力量：Lv.{8}\n敏捷：Lv.{9}",
                hp,
                maxHp,
                Mathf.Max(0, baseAtk - strengthLevel * 2),
                strengthLevel * 2,
                defense,
                critChance * 100f,
                moveSpeed,
                attackSpeed,
                strengthLevel,
                agilityLevel);

            upgradeText.text = string.Format("灵魂碎片：{0}\n\n接口预留：\nUpgradeStat(\"strength\")\nUpgradeStat(\"agility\")\n后续可接天赋树、局外强化、Boss 掉落货币。", soulShards);
        }

        private static float GetPrivateFloat(Object target, string fieldName, float fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return field != null ? (float)field.GetValue(target) : fallback;
        }

        private void HidePanelImmediate()
        {
            visible = false;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            panel.GetComponent<Image>().color = new Color(0.075f, 0.085f, 0.115f, 0.92f);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, int size, FontStyle style)
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
            text.fontSize = size;
            text.fontStyle = style;
            text.color = new Color(0.93f, 0.96f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(-36f, 42f);
            buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.24f, 0.36f, 0.95f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);

            Text text = CreateText(buttonObject.transform, "Label", new Vector2(8f, 4f), new Vector2(-8f, -4f), 15, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
        }
    }
}
