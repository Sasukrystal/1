using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModernRogue
{
    public static class GameSettingsPanelBuilder
    {
        public static bool IsKeyRebindActive { get; private set; }

        public sealed class Options
        {
            public bool showRunActions;
            public bool showEscapeHint = true;
            public UnityAction onRestartRun;
            public UnityAction onReturnToMainMenu;
            public UnityAction onRebuild;
        }

        public static void Build(Transform parent, Options options)
        {
            options ??= new Options();
            ClearChildren(parent);

            Text musicLabel = CreateText(parent, "MusicLabel", "音乐音量", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(musicLabel.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.28f, 0.88f));
            CreateSlider(parent, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.81f), GameSettingsService.MusicVolume, value =>
            {
                GameSettingsService.MusicVolume = value;
                GameAudioService.Ensure()?.ApplyVolume();
            });

            Text sfxLabel = CreateText(parent, "SfxLabel", "音效音量", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(sfxLabel.rectTransform, new Vector2(0.04f, 0.69f), new Vector2(0.28f, 0.75f));
            CreateSlider(parent, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.68f), GameSettingsService.SfxVolume, value =>
            {
                GameSettingsService.SfxVolume = value;
                GameAudioService.Ensure()?.ApplyVolume();
            });

            Text keyTitle = CreateText(parent, "KeyTitle", "按键绑定（点击后按下新键）", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(keyTitle.rectTransform, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.62f));

            float scrollMax = options.showRunActions ? 0.55f : 0.55f;
            float scrollMin = options.showRunActions ? 0.28f : 0.14f;
            ScrollRect settingsScroll = BuildScrollViewport(parent, new Vector2(0.04f, scrollMin), new Vector2(0.96f, scrollMax), out Text placeholderBody);
            Object.Destroy(placeholderBody.gameObject);
            RectTransform scrollContent = settingsScroll.content;

            GameObject list = new GameObject("KeyList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(scrollContent, false);
            RectTransform listRect = list.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 4);
            ContentSizeFitter fitter = list.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string[] actions = GameSettingsService.BindableActions;
            for (int i = 0; i < actions.Length; i++)
            {
                CreateKeyBindRow(list.transform, actions[i]);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

            if (options.showRunActions)
            {
                Text runTitle = CreateText(parent, "RunTitle", "冒险控制", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
                SetRect(runTitle.rectTransform, new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.29f));

                Button restartButton = CreateActionButton(parent, "RestartRun", "返回基地重新开始",
                    "结束本局并结算，随后回到基地开启全新冒险", new Vector2(0.04f, 0.14f), new Vector2(0.48f, 0.22f),
                    new Color(0.58f, 0.24f, 0.18f, 1f));
                restartButton.onClick.AddListener(() => options.onRestartRun?.Invoke());

                Button menuButton = CreateActionButton(parent, "ReturnMainMenu", "返回主菜单",
                    "结束本局并结算，随后回到开始界面", new Vector2(0.52f, 0.14f), new Vector2(0.96f, 0.22f),
                    new Color(0.18f, 0.34f, 0.58f, 1f));
                menuButton.onClick.AddListener(() => options.onReturnToMainMenu?.Invoke());
            }

            Button reset = CreateSmallButton(parent, "Reset", "恢复默认", new Vector2(0.04f, 0.04f), new Vector2(0.2f, 0.11f));
            reset.onClick.AddListener(() =>
            {
                GameSettingsService.ResetKeyBindings();
                options.onRebuild?.Invoke();
            });

            if (options.showEscapeHint)
            {
                string hint = options.showRunActions
                    ? "移动固定为 WASD / 方向键；设置页 ESC 返回上一级，其他页签 ESC 关闭总面板。"
                    : "移动固定为 WASD / 方向键；ESC 或右上角按钮返回上一级。";
                Text moveHint = CreateText(parent, "MoveHint", hint, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
                moveHint.color = new Color(0.78f, 0.82f, 0.9f, 0.9f);
                SetRect(moveHint.rectTransform, new Vector2(0.22f, 0.04f), new Vector2(0.96f, 0.11f));
            }
        }

        private static void CreateKeyBindRow(Transform parent, string actionId)
        {
            GameObject row = new GameObject("KeyRow_" + actionId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.spacing = 10f;
            rowLayout.padding = new RectOffset(4, 4, 2, 2);

            Text name = CreateText(row.transform, "Name", GameSettingsService.GetActionDisplayName(actionId), 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            LayoutElement nameLayout = name.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.preferredWidth = 180f;

            GameObject buttonObj = new GameObject("Bind", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(row.transform, false);
            buttonObj.GetComponent<LayoutElement>().preferredWidth = 150f;
            RuntimeUIVisuals.StyleAccent(buttonObj.GetComponent<Image>(), new Color(0.18f, 0.34f, 0.58f, 1f));
            Text bindLabel = CreateText(buttonObj.transform, "Text", GameSettingsService.GetKeyDisplayName(actionId), 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(bindLabel.rectTransform, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f));
            Button button = buttonObj.GetComponent<Button>();
            KeyBindHandler handler = buttonObj.AddComponent<KeyBindHandler>();
            handler.Initialize(actionId, button);
        }

        private static ScrollRect BuildScrollViewport(Transform parent, Vector2 min, Vector2 max, out Text bodyText)
        {
            GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(parent, false);
            SetRect(scrollObj.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleImage(scrollObj.GetComponent<Image>(), new Color(0.018f, 0.023f, 0.032f, 0.82f));
            RuntimeUIVisuals.AddFrame(scrollObj.transform, "ScrollFrame", new Color(0.58f, 0.66f, 0.82f, 0.35f), 0.012f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            SetRect(viewport.GetComponent<RectTransform>(), new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.98f));
            Image viewportImage = viewport.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(viewportImage, new Color(1f, 1f, 1f, 0.02f));
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = new Vector2(12f, 0f);
            contentRect.offsetMax = new Vector2(-12f, 0f);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bodyText = CreateText(content.transform, "Body", string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft);
            bodyText.supportRichText = true;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0f, 120f);

            ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            return scroll;
        }

        private static Slider CreateSlider(Transform parent, Vector2 min, Vector2 max, float value, UnityAction<float> onChanged)
        {
            GameObject obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            Slider slider = obj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(onChanged);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(obj.transform, false);
            SetRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            RuntimeUIVisuals.StyleImage(background.GetComponent<Image>(), new Color(0.12f, 0.14f, 0.18f, 1f));

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(obj.transform, false);
            SetRect(fillArea.GetComponent<RectTransform>(), new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.82f));

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            SetRect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            RuntimeUIVisuals.StyleImage(fill.GetComponent<Image>(), new Color(0.72f, 0.52f, 0.18f, 1f));

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }

        private static Button CreateSmallButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), new Color(0.18f, 0.34f, 0.58f, 1f));
            Text text = CreateText(obj.transform, "Text", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            return obj.GetComponent<Button>();
        }

        private static Button CreateActionButton(Transform parent, string name, string title, string subtitle, Vector2 min, Vector2 max, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), color);
            RuntimeUIVisuals.AddFrame(obj.transform, "ActionFrame", new Color(1f, 0.92f, 0.68f, 0.45f), 0.05f);
            Text text = CreateText(obj.transform, "Text", title + "\n<size=12>" + subtitle + "</size>", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f));
            return obj.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        private sealed class KeyBindHandler : MonoBehaviour
        {
            private string actionId;
            private Button button;
            private bool waiting;

            public void Initialize(string id, Button target)
            {
                actionId = id;
                button = target;
                button.onClick.AddListener(BeginRebind);
            }

            private void BeginRebind()
            {
                waiting = true;
                IsKeyRebindActive = true;
                Text text = button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = "按下新键…";
                }
            }

            private void Update()
            {
                if (!waiting)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    waiting = false;
                    IsKeyRebindActive = false;
                    RefreshLabel();
                    return;
                }

                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (key == KeyCode.None || !Input.GetKeyDown(key))
                    {
                        continue;
                    }

                    GameSettingsService.SetKey(actionId, key);
                    waiting = false;
                    IsKeyRebindActive = false;
                    RefreshLabel();
                    return;
                }
            }

            private void OnDisable()
            {
                if (waiting)
                {
                    waiting = false;
                    IsKeyRebindActive = false;
                }
            }

            private void RefreshLabel()
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = GameSettingsService.GetKeyDisplayName(actionId);
                }
            }
        }
    }
}
