using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    public abstract class LobbyRightClickInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRange = 3.2f;

        protected virtual void Update()
        {
            if (!Input.GetMouseButtonDown(1) || !IsMouseOverMe())
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector2.Distance(player.transform.position, transform.position) > interactRange)
            {
                SoulKnightDirector.Instance?.ShowPickupTip("再靠近一点后右键互动");
                return;
            }

            Interact();
        }

        protected abstract void Interact();

        private bool IsMouseOverMe()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].transform == transform)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class LobbyClassStatueInteractable : LobbyRightClickInteractable
    {
        protected override void Interact()
        {
            ClassSelectionPanel.Open();
        }
    }

    public sealed class LobbyWeaponForgeInteractable : LobbyRightClickInteractable
    {
        protected override void Interact()
        {
            if (RunProgressionSystem.Instance == null)
            {
                return;
            }

            WeaponForgePanel.Open();
        }
    }

    public sealed class ClassSelectionPanel : MonoBehaviour
    {
        private GameObject root;

        public static void Open()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                return;
            }

            GameObject old = GameObject.Find("ClassSelectionPanel");
            if (old != null)
            {
                Destroy(old);
            }

            GameObject panel = new GameObject("ClassSelectionPanel", typeof(RectTransform), typeof(Image), typeof(ClassSelectionPanel), typeof(ModalPauseToken));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820f, 460f);
            rect.anchoredPosition = Vector2.zero;
            RuntimeUIVisuals.StyleImage(panel.GetComponent<Image>(), new Color(0.025f, 0.03f, 0.045f, 0.98f));
            RuntimeUIVisuals.AddFrame(panel.transform, "ClassFrame", new Color(0.72f, 0.52f, 0.18f, 0.85f), 0.012f);
            panel.GetComponent<ClassSelectionPanel>().root = panel;
            ModalPanelNavigation navigation = panel.AddComponent<ModalPanelNavigation>();
            navigation.Initialize(panel);
            panel.GetComponent<ClassSelectionPanel>().Build(navigation);
        }

        private void Build(ModalPanelNavigation navigation)
        {
            HeroClass current = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;

            RuntimeUIVisuals.CreateBlock(transform, "HeaderBar", new Vector2(0.03f, 0.84f), new Vector2(0.97f, 0.96f), new Color(0.1f, 0.07f, 0.04f, 0.98f));
            Text title = CreateText(transform, "Title", "选择职业", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 0.85f), new Vector2(1f, 0.96f));
            Text subtitle = CreateText(transform, "Subtitle", "当前： " + RunProgressionSystem.GetClassName(current) + "    仅开放战士与弓箭手", 15, FontStyle.Normal, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.82f, 0.86f, 0.94f, 0.92f);
            SetRect(subtitle.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.84f));

            UiNavigationHelper.CreateBackButton(
                transform,
                navigation.Close,
                new Vector2(0.82f, 0.86f),
                new Vector2(0.97f, 0.96f));

            CreateClassButton(HeroClass.Warrior, 0, "战士", "单手剑与盾\n左键弧形近战，右键格挡\n稳定、容错高", current == HeroClass.Warrior);
            CreateClassButton(HeroClass.Archer, 1, "弓箭手", "固定短弓\n左键射击，长按蓄力穿透\n闪避距离更长", current == HeroClass.Archer);

            Button close = CreateButton(transform, "Close", "返回上一级");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.42f, 0.06f), new Vector2(0.58f, 0.14f));
            close.onClick.AddListener(navigation.Close);
        }

        private void CreateClassButton(HeroClass heroClass, int index, string title, string body, bool isCurrent)
        {
            GameObject obj = new GameObject("Class_" + title, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(transform, false);
            float left = index == 0 ? 0.06f : 0.52f;
            SetRect(obj.GetComponent<RectTransform>(), new Vector2(left, 0.2f), new Vector2(left + 0.42f, 0.76f));
            RuntimeUIVisuals.StyleImage(obj.GetComponent<Image>(), ResolveClassColor(heroClass));
            Color frameColor = isCurrent ? new Color(1f, 0.86f, 0.38f, 0.95f) : new Color(1f, 1f, 1f, 0.18f);
            RuntimeUIVisuals.AddFrame(obj.transform, "CardFrame", frameColor, isCurrent ? 0.028f : 0.018f);

            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                RunProgressionSystem.Instance?.SelectClass(heroClass);
                SoulKnightDirector.Instance?.ShowToast("职业已切换：" + RunProgressionSystem.GetClassName(heroClass));
                Destroy(root);
            });

            Text badge = CreateText(obj.transform, "Badge", isCurrent ? "当前职业" : "点击切换", 13, FontStyle.Bold, TextAnchor.UpperLeft);
            badge.color = isCurrent ? new Color(1f, 0.9f, 0.55f, 1f) : new Color(0.82f, 0.88f, 0.96f, 0.88f);
            SetRect(badge.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.98f));

            Text text = CreateText(obj.transform, "Text", title + "\n\n" + body, 17, FontStyle.Bold, TextAnchor.LowerCenter);
            SetRect(text.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.92f));
        }

        private static Color ResolveClassColor(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Archer:
                    return new Color(0.08f, 0.34f, 0.18f, 0.98f);
                case HeroClass.Mage:
                    return new Color(0.08f, 0.16f, 0.38f, 0.98f);
                default:
                    return new Color(0.36f, 0.12f, 0.1f, 0.98f);
            }
        }

        private static Canvas ResolveCanvas()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            Canvas[] canvases = FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvases[i];
                }
            }

            GameObject obj = new GameObject("ClassSelectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 540;
            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), new Color(0.18f, 0.34f, 0.58f, 1f));
            Text text = CreateText(obj.transform, "Text", label, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            return obj.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.text = value;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
