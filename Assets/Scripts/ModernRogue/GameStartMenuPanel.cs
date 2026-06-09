using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    public sealed class GameStartMenuPanel : MonoBehaviour
    {
        private const float InputGraceSeconds = 1.0f;

        public static bool IsVisible { get; private set; }

        private static GameStartMenuPanel activeInstance;
        private static Canvas menuCanvas;

        private SoulKnightDirector director;
        private GameObject root;
        private GameObject subPanelRoot;
        private Button continueButton;
        private CanvasGroup canvasGroup;
        private float inputEnableTime;
        private Coroutine enableInputRoutine;
        private Text helpContentText;
        private ScrollRect helpScrollRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsVisible = false;
            activeInstance = null;
            menuCanvas = null;
        }

        public static void Show(SoulKnightDirector owner)
        {
            if (owner == null)
            {
                return;
            }

            if (activeInstance != null && activeInstance.root != null)
            {
                activeInstance.director = owner;
                activeInstance.root.SetActive(true);
                if (menuCanvas != null)
                {
                    menuCanvas.gameObject.SetActive(true);
                }

                activeInstance.BeginInputGracePeriod();
                IsVisible = true;
                owner.SetHudVisible(false);
                ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(true);
                return;
            }

            GameAudioService.Ensure();
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                return;
            }

            canvas.gameObject.SetActive(true);

            GameObject panel = new GameObject("GameStartMenuPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(GameStartMenuPanel));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RuntimeUIVisuals.StyleImage(panel.GetComponent<Image>(), new Color(0.015f, 0.02f, 0.035f, 0.98f));
            RuntimeUIVisuals.AddFrame(panel.transform, "TitleFrame", new Color(0.72f, 0.52f, 0.18f, 0.55f), 0.006f);

            GameStartMenuPanel component = panel.GetComponent<GameStartMenuPanel>();
            component.director = owner;
            component.root = panel;
            component.canvasGroup = panel.GetComponent<CanvasGroup>();
            activeInstance = component;
            component.BeginInputGracePeriod();
            component.BuildMainMenu();
            IsVisible = true;
            owner.SetHudVisible(false);
            ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(true);
            ClearUiSelection();
        }

        private void BeginInputGracePeriod()
        {
            inputEnableTime = Time.unscaledTime + InputGraceSeconds;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            SetAllMenuButtonsInteractable(false);
            if (enableInputRoutine != null)
            {
                StopCoroutine(enableInputRoutine);
            }

            enableInputRoutine = StartCoroutine(EnableInputAfterGrace());
        }

        private IEnumerator EnableInputAfterGrace()
        {
            while (Time.unscaledTime < inputEnableTime)
            {
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            SetAllMenuButtonsInteractable(true);
            if (continueButton != null)
            {
                RefreshContinueButton();
            }

            enableInputRoutine = null;
        }

        private void SetAllMenuButtonsInteractable(bool interactable)
        {
            if (root == null)
            {
                return;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].interactable = interactable;
                }
            }
        }

        private static void ClearUiSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
                IsVisible = false;
            }
        }

        private void Update()
        {
            if (!IsVisible || subPanelRoot == null || !CanAcceptInput())
            {
                return;
            }

            UiNavigationHelper.TryConsumeEscape(BuildMainMenu);
        }

        private bool CanAcceptInput()
        {
            return Time.unscaledTime >= inputEnableTime;
        }

        private void BuildMainMenu()
        {
            ClearSubPanel();
            SetMainMenuVisible(true);
            helpContentText = null;
            helpScrollRect = null;

            Text title = CreateText(transform, "Title", "黑暗地牢", 46, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = new Color(1f, 0.9f, 0.62f, 1f);
            SetRect(title.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.9f));

            Text subtitle = CreateText(transform, "Subtitle", "Roguelike 地牢冒险 · 局内构筑 · 局外强化", 18, FontStyle.Normal, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.82f, 0.86f, 0.94f, 0.9f);
            SetRect(subtitle.rectTransform, new Vector2(0.15f, 0.66f), new Vector2(0.85f, 0.72f));

            CreateMenuButton("NewGame", "新游戏", "进入基地，开始新的冒险", new Vector2(0.32f, 0.54f), new Vector2(0.68f, 0.62f), StartNewGame);
            continueButton = CreateMenuButton("Continue", "继续上次游戏", "继续上次未完成的冒险", new Vector2(0.32f, 0.445f), new Vector2(0.68f, 0.525f), ContinueGame);
            CreateMenuButton("Help", "游戏说明", "查看各系统玩法说明", new Vector2(0.32f, 0.35f), new Vector2(0.68f, 0.43f), ShowHelp);
            CreateMenuButton("Settings", "设置", "按键与音乐音量", new Vector2(0.32f, 0.255f), new Vector2(0.68f, 0.335f), ShowSettings);
            CreateMenuButton("ExitGame", "退出游戏", "关闭游戏并返回桌面", new Vector2(0.32f, 0.16f), new Vector2(0.68f, 0.24f), ExitGame);

            RefreshContinueButton();
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
            {
                return;
            }

            bool hasSave = GameSaveService.HasRunSave();
            continueButton.interactable = hasSave;
            Text label = continueButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = hasSave ? "继续上次游戏\n<size=14>继续上次未完成的冒险</size>" : "继续上次游戏\n<size=14><color=#888888>暂无未完成冒险</color></size>";
            }
        }

        private void StartNewGame()
        {
            if (!CanAcceptInput())
            {
                return;
            }

            GameSaveService.ClearRunSave();
            CloseAndEnter(() => director.LoadLobby());
        }

        private void ContinueGame()
        {
            if (!CanAcceptInput())
            {
                return;
            }

            if (!GameSaveService.HasRunSave())
            {
                director?.ShowToast("没有可继续的冒险存档");
                return;
            }

            if (!GameSaveService.TryRestoreRun(director))
            {
                director?.ShowToast("存档读取失败");
                return;
            }

            CloseAndEnter(null);
        }

        private void CloseAndEnter(System.Action afterClose)
        {
            IsVisible = false;
            activeInstance = null;
            ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(false);
            director?.SetHudVisible(true);
            afterClose?.Invoke();
            Destroy(root);
        }

        private void ShowHelp()
        {
            SetMainMenuVisible(false);
            BuildCategoryHelpPanel();
        }

        private void BuildCategoryHelpPanel()
        {
            ClearSubPanel();
            HelpSection[] sections = GetHelpSections();
            subPanelRoot = CreateSubPanelShell("HelpPanel", "游戏说明", BuildMainMenu);

            GameObject sidebar = new GameObject("Sidebar", typeof(RectTransform), typeof(VerticalLayoutGroup));
            sidebar.transform.SetParent(subPanelRoot.transform, false);
            SetRect(sidebar.GetComponent<RectTransform>(), new Vector2(0.04f, 0.06f), new Vector2(0.26f, 0.86f));
            VerticalLayoutGroup sidebarLayout = sidebar.GetComponent<VerticalLayoutGroup>();
            sidebarLayout.spacing = 8f;
            sidebarLayout.childForceExpandHeight = false;
            sidebarLayout.childForceExpandWidth = true;
            sidebarLayout.padding = new RectOffset(0, 0, 4, 4);

            helpScrollRect = BuildScrollViewport(subPanelRoot.transform, new Vector2(0.28f, 0.06f), new Vector2(0.96f, 0.86f), out helpContentText);

            helpContentText.fontSize = 15;
            helpContentText.lineSpacing = 1.15f;

            for (int i = 0; i < sections.Length; i++)
            {
                int sectionIndex = i;
                Button tab = CreateSidebarButton(sidebar.transform, sections[i].title);
                tab.onClick.AddListener(() => SelectHelpSection(sectionIndex));
            }

            SelectHelpSection(0);
        }

        private void SelectHelpSection(int index)
        {
            HelpSection[] sections = GetHelpSections();
            if (helpContentText == null || index < 0 || index >= sections.Length)
            {
                return;
            }

            HelpSection section = sections[index];
            helpContentText.text = "<b><size=18>" + section.title + "</size></b>\n\n" + section.body;
            Canvas.ForceUpdateCanvases();
            RectTransform bodyRect = helpContentText.rectTransform;
            bodyRect.sizeDelta = new Vector2(0f, helpContentText.preferredHeight + 32f);
            if (helpScrollRect != null && helpScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(helpScrollRect.content);
                helpScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static Button CreateSidebarButton(Transform parent, string label)
        {
            GameObject obj = new GameObject("Tab_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<LayoutElement>().preferredHeight = 38f;
            RuntimeUIVisuals.StyleImage(obj.GetComponent<Image>(), new Color(0.08f, 0.1f, 0.14f, 0.96f));
            RuntimeUIVisuals.AddFrame(obj.transform, "TabFrame", new Color(0.58f, 0.66f, 0.82f, 0.45f), 0.04f);
            Text text = CreateText(obj.transform, "Label", label, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            return obj.GetComponent<Button>();
        }

        private void ShowSettings()
        {
            SetMainMenuVisible(false);
            ClearSubPanel();
            subPanelRoot = CreateSubPanelShell("SettingsPanel", "设置", BuildMainMenu);
            GameSettingsPanelBuilder.Build(subPanelRoot.transform, new GameSettingsPanelBuilder.Options
            {
                onRebuild = ShowSettings
            });
        }

        private void ExitGame()
        {
            if (!CanAcceptInput())
            {
                return;
            }

            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private GameObject CreateSubPanelShell(string panelName, string titleText, UnityEngine.Events.UnityAction onBack)
        {
            GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            RuntimeUIVisuals.StyleImage(panel.GetComponent<Image>(), new Color(0.025f, 0.03f, 0.045f, 0.99f));
            RuntimeUIVisuals.AddFrame(panel.transform, "SubFrame", new Color(0.72f, 0.52f, 0.18f, 0.55f), 0.008f);

            Text title = CreateText(panel.transform, "Title", titleText, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.color = new Color(1f, 0.9f, 0.62f, 1f);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.9f), new Vector2(0.7f, 0.98f));

            Button back = CreateSmallButton(panel.transform, "Back", "返回上一级", new Vector2(0.74f, 0.915f), new Vector2(0.95f, 0.975f));
            back.onClick.AddListener(onBack);
            return panel;
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

        private void SetMainMenuVisible(bool visible)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.gameObject == subPanelRoot)
                {
                    continue;
                }

                child.gameObject.SetActive(visible);
            }
        }

        private static HelpSection[] GetHelpSections()
        {
            return new[]
            {
                new HelpSection(
                    "基地大厅",
                    "· 出征宝箱：领取补给后，靠近传送门开始 1-1 冒险\n" +
                    "· 职业雕像：右键切换战士 / 弓箭手\n" +
                    "· 武器铺：局外成长入口，消耗 Boss 精华升级装备"),
                new HelpSection(
                    "局内成长",
                    "· <b>经验 / 等级</b>：清怪与清房获得经验，升级可触发三选一强化\n" +
                    "· <b>宝物</b>：宝物房、商人、Boss 掉落；获得即生效，可叠加\n" +
                    "· <b>虫核</b>：按风/火/雷/水/金凑套装，获得移速、灼烧、感电等特效\n" +
                    "· 同属性宝物碎片满 5 枚可额外解锁 2 个虫核槽\n" +
                    "· 局内武器 / 护甲固定；商人只卖消耗品、材料、虫核与宝物"),
                new HelpSection(
                    "局外成长",
                    "· 击败 Boss 获得 <b>Boss 精华</b>，在基地武器铺消耗精华升级\n" +
                    "· <b>武器</b>：每升 1 级攻击 +1；每 5 级获得 1 条稀有词条\n" +
                    "· <b>防具</b>：每升 1 级防御 +1、生命 +3；Lv.5 解锁护甲护盾\n" +
                    "· 局外等级越高，下次出征的基础属性与护盾越强"),
                new HelpSection(
                    "冒险流程",
                    "· 共 5 层关卡，清房获得金币、药剂与虫核\n" +
                    "· 每层末有 Boss；第 5 层为双 Boss 挑战\n" +
                    "· 1-1 废墟前厅 → 1-2 箭廊墓道 → 1-3 腐潮深井 → 1-4 熔炉回廊 → 1-5 王座外环"),
                new HelpSection(
                    "战斗操作",
                    "· 战士：左键近战，右键格挡\n" +
                    "· 弓箭手：左键射击，长按蓄力穿透\n" +
                    "· 空格闪避；1 / 2 / 3 使用背包前三格消耗品"),
                new HelpSection(
                    "面板与存档",
                    "· B 开背包，E / ESC 开关总面板\n" +
                    "· 背包 / 角色 / 虫核 / 宝物 / 设置 五个页签\n" +
                    "· 设置页可改键位、结束本局返回基地或主菜单\n" +
                    "· 角色页可花费金币做少量永久属性 +1（本局有效）\n" +
                    "· 冒险进行中自动存档，可在开始界面「继续上次游戏」"),
                new HelpSection(
                    "Boss 介绍",
                    "<b>地牢巨像（1-1）</b>\n" +
                    "· 层数：废墟前厅\n" +
                    "· 特点：重击与落石，适合熟悉 Boss 节奏\n" +
                    "· 二阶段：体型增大，压迫感更强\n\n" +
                    "<b>风暴守卫（1-2）</b>\n" +
                    "· 层数：箭廊墓道\n" +
                    "· 特点：冲锋预警后高速突进，并释放十字闪电\n" +
                    "· 二阶段：冲锋与落雷频率提高\n\n" +
                    "<b>巢穴母虫（1-3）</b>\n" +
                    "· 层数：腐潮深井\n" +
                    "· 特点：召唤小怪并喷射毒液，控场压力高\n" +
                    "· 二阶段：召唤与毒区更密集\n\n" +
                    "<b>烬火术士（1-4）</b>\n" +
                    "· 层数：熔炉回廊\n" +
                    "· 特点：扇形火球与地面火焰符文，需持续走位\n" +
                    "· 二阶段：施法间隔缩短，火力更猛\n\n" +
                    "<b>终局双 Boss（1-5）</b>\n" +
                    "· 层数：王座外环\n" +
                    "· 特点：两名 Boss 同场，从巨像、风暴守卫、母虫、烬火术士中随机组合\n" +
                    "· 建议：优先处理高威胁远程目标，并保留闪避应对合击")
            };
        }

        private void ClearSubPanel()
        {
            if (subPanelRoot != null)
            {
                Destroy(subPanelRoot);
                subPanelRoot = null;
            }

            helpContentText = null;
            helpScrollRect = null;
        }

        private Button CreateMenuButton(string name, string title, string subtitle, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(transform, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleImage(obj.GetComponent<Image>(), new Color(0.08f, 0.1f, 0.14f, 0.96f));
            RuntimeUIVisuals.AddFrame(obj.transform, "BtnFrame", new Color(0.72f, 0.52f, 0.18f, 0.65f), 0.02f);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);

            Text text = CreateText(obj.transform, "Label", title + "\n<size=14>" + subtitle + "</size>", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            return button;
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

        private static Canvas ResolveCanvas()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            if (menuCanvas != null)
            {
                ConfigureMenuCanvas(menuCanvas);
                menuCanvas.gameObject.SetActive(true);
                return menuCanvas;
            }

            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate != null && candidate.name == "GameStartMenuCanvas")
                {
                    menuCanvas = candidate;
                    ConfigureMenuCanvas(menuCanvas);
                    menuCanvas.gameObject.SetActive(true);
                    Object.DontDestroyOnLoad(menuCanvas.gameObject);
                    return menuCanvas;
                }
            }

            GameObject obj = new GameObject("GameStartMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(obj);
            menuCanvas = obj.GetComponent<Canvas>();
            ConfigureMenuCanvas(menuCanvas);
            return menuCanvas;
        }

        private static void ConfigureMenuCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
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

        private sealed class HelpSection
        {
            public readonly string title;
            public readonly string body;

            public HelpSection(string titleValue, string bodyValue)
            {
                title = titleValue;
                body = bodyValue;
            }
        }

    }
}
