using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    [DefaultExecutionOrder(-700)]
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private GameObject unifiedRoot;
        private GameObject backpackRoot;
        private GameObject characterRoot;
        private GameObject coreRoot;
        private GameObject treasureRoot;
        private GameObject settingsRoot;
        private NewBackpackPanel backpackPanel;
        private NewCharacterPanel characterPanel;
        private NewCorePanel corePanel;
        private NewTreasurePanel treasurePanel;
        private NewSettingsPanel settingsPanel;
        private Canvas modernCanvas;
        private int activeTab;
        private int previousTab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetRuntimeState()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeUiManager()
        {
            if (FindObjectOfType<UIManager>() != null)
            {
                return;
            }

            new GameObject("ModernUIManager").AddComponent<UIManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            EnsureInventory();
            BuildPanels();
            DisableLegacyUiConflicts();
            SetUnifiedVisible(false);
        }

        private void Update()
        {
            if (GameStartMenuPanel.IsVisible)
            {
                return;
            }

            if (GameSettingsService.WasPressed("Inventory"))
            {
                OpenTab(0);
            }

            if (GameSettingsService.WasPressed("MenuToggle") || Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsUnifiedVisible() && Input.GetKeyDown(KeyCode.Escape) && activeTab == 4)
                {
                    if (!UiNavigationHelper.ShouldBlockEscape)
                    {
                        GoBackFromSettings();
                        return;
                    }
                }

                ToggleUnified();
            }

            if (!IsUnifiedVisible() && !RogueUiPause.IsPaused)
            {
                if (GameSettingsService.WasPressed("QuickSlot1"))
                {
                    NewInventorySystem.Instance?.TryUseQuickSlot(0);
                }
                else if (GameSettingsService.WasPressed("QuickSlot2"))
                {
                    NewInventorySystem.Instance?.TryUseQuickSlot(1);
                }
                else if (GameSettingsService.WasPressed("QuickSlot3"))
                {
                    NewInventorySystem.Instance?.TryUseQuickSlot(2);
                }
            }
        }

        public bool IsUnifiedVisible()
        {
            return unifiedRoot != null && unifiedRoot.activeSelf;
        }

        public void ToggleUnified()
        {
            SetUnifiedVisible(!unifiedRoot.activeSelf);
            if (unifiedRoot.activeSelf)
            {
                ShowTab(activeTab);
            }
        }

        public void OpenTab(int tabIndex)
        {
            activeTab = Mathf.Clamp(tabIndex, 0, 4);
            SetUnifiedVisible(true);
            ShowTab(activeTab);
        }

        public void CloseUnified()
        {
            SetUnifiedVisible(false);
        }

        public void GoBackFromSettings()
        {
            if (!IsUnifiedVisible() || activeTab != 4)
            {
                return;
            }

            ShowTab(previousTab);
        }

        private void SetUnifiedVisible(bool visible)
        {
            if (unifiedRoot == null || unifiedRoot.activeSelf == visible)
            {
                return;
            }

            unifiedRoot.SetActive(visible);
        }

        private void EnsureInventory()
        {
            if (NewInventorySystem.Instance != null)
            {
                EnsureRunProgression();
                return;
            }

            GameObject inventoryObject = new GameObject("NewInventorySystem");
            inventoryObject.AddComponent<NewInventorySystem>();
            EnsureRunProgression();
        }

        private void EnsureRunProgression()
        {
            if (RunProgressionSystem.Instance != null)
            {
                return;
            }

            GameObject runObject = new GameObject("RunProgressionSystem");
            runObject.AddComponent<RunProgressionSystem>();
        }

        private void BuildPanels()
        {
            Canvas canvas = EnsureModernCanvas();
            unifiedRoot = CreateRoot(canvas.transform, "UnifiedRoguePanelRoot", new Vector2(980f, 590f));
            unifiedRoot.AddComponent<ModalPauseToken>();
            Image rootImage = unifiedRoot.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(rootImage, new Color(0.018f, 0.022f, 0.032f, 0.96f));
            RuntimeUIVisuals.AddFrame(unifiedRoot.transform, "UnifiedFrame", new Color(0.58f, 0.66f, 0.82f, 0.55f), 0.01f);
            BuildTabs(unifiedRoot.transform);

            backpackRoot = CreateContent(unifiedRoot.transform, "BackpackTabContent");
            backpackPanel = backpackRoot.AddComponent<NewBackpackPanel>();
            backpackPanel.Build(backpackRoot.GetComponent<RectTransform>());
            backpackPanel.RefreshUI();

            characterRoot = CreateContent(unifiedRoot.transform, "CharacterTabContent");
            characterPanel = characterRoot.AddComponent<NewCharacterPanel>();
            characterPanel.Build(characterRoot.GetComponent<RectTransform>());
            characterPanel.RefreshUI();

            coreRoot = CreateContent(unifiedRoot.transform, "CoreTabContent");
            corePanel = coreRoot.AddComponent<NewCorePanel>();
            corePanel.Build(coreRoot.GetComponent<RectTransform>());
            corePanel.RefreshUI();

            treasureRoot = CreateContent(unifiedRoot.transform, "TreasureTabContent");
            treasurePanel = treasureRoot.AddComponent<NewTreasurePanel>();
            treasurePanel.Build(treasureRoot.GetComponent<RectTransform>());
            treasurePanel.RefreshUI();

            settingsRoot = CreateContent(unifiedRoot.transform, "SettingsTabContent");
            settingsPanel = settingsRoot.AddComponent<NewSettingsPanel>();
            settingsPanel.Build(settingsRoot.GetComponent<RectTransform>());

            ShowTab(0);
        }

        private void BuildTabs(Transform parent)
        {
            CreateTabButton(parent, "背包", 0, new Vector2(0.02f, 0.91f), new Vector2(0.155f, 0.985f));
            CreateTabButton(parent, "角色", 1, new Vector2(0.16f, 0.91f), new Vector2(0.295f, 0.985f));
            CreateTabButton(parent, "虫核", 2, new Vector2(0.3f, 0.91f), new Vector2(0.435f, 0.985f));
            CreateTabButton(parent, "宝物", 3, new Vector2(0.44f, 0.91f), new Vector2(0.575f, 0.985f));
            CreateTabButton(parent, "设置", 4, new Vector2(0.58f, 0.91f), new Vector2(0.715f, 0.985f));

            Text hint = CreateText(parent, "UnifiedHint", 14, FontStyle.Normal, TextAnchor.MiddleRight);
            hint.text = "1/2/3 快捷道具    E / ESC 关闭";
            hint.raycastTarget = false;
            SetRect(hint.rectTransform, new Vector2(0.72f, 0.92f), new Vector2(0.965f, 0.98f));
        }

        private Button CreateTabButton(Transform parent, string label, int tabIndex, Vector2 min, Vector2 max)
        {
            GameObject obj = new GameObject("Tab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            Image image = obj.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.09f, 0.12f, 0.95f));
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => ShowTab(tabIndex));
            EventTrigger trigger = obj.AddComponent<EventTrigger>();
            EventTrigger.Entry click = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            click.callback.AddListener(_ => ShowTab(tabIndex));
            trigger.triggers.Add(click);
            Text text = CreateText(obj.transform, "Text", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = label;
            text.raycastTarget = false;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private GameObject CreateContent(Transform parent, string name)
        {
            GameObject content = new GameObject(name, typeof(RectTransform), typeof(Image));
            content.transform.SetParent(parent, false);
            SetRect(content.GetComponent<RectTransform>(), new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.895f));
            Image image = content.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.03f, 0.038f, 0.052f, 0.92f));
            image.raycastTarget = false;
            return content;
        }

        private void ShowTab(int tabIndex)
        {
            int nextTab = Mathf.Clamp(tabIndex, 0, 4);
            if (nextTab == 4 && activeTab != 4)
            {
                previousTab = activeTab;
            }

            activeTab = nextTab;
            if (backpackRoot != null)
            {
                backpackRoot.SetActive(activeTab == 0);
            }

            if (characterRoot != null)
            {
                characterRoot.SetActive(activeTab == 1);
            }

            if (coreRoot != null)
            {
                coreRoot.SetActive(activeTab == 2);
            }

            if (treasureRoot != null)
            {
                treasureRoot.SetActive(activeTab == 3);
            }

            if (settingsRoot != null)
            {
                settingsRoot.SetActive(activeTab == 4);
            }

            RefreshActiveTab();
        }

        private void RefreshActiveTab()
        {
            switch (activeTab)
            {
                case 0:
                    if (backpackPanel != null)
                    {
                        backpackPanel.RefreshUI();
                    }
                    break;
                case 1:
                    if (characterPanel != null)
                    {
                        characterPanel.RefreshUI();
                    }
                    break;
                case 2:
                    if (corePanel != null)
                    {
                        corePanel.RefreshUI();
                    }
                    break;
                case 3:
                    if (treasurePanel != null)
                    {
                        treasurePanel.RefreshUI();
                    }
                    break;
                case 4:
                    if (settingsPanel != null)
                    {
                        settingsPanel.RefreshUI();
                    }
                    break;
            }
        }

        private Canvas EnsureModernCanvas()
        {
            if (modernCanvas != null)
            {
                return modernCanvas;
            }

            GameObject existing = GameObject.Find("ModernRogueUICanvas");
            if (existing != null)
            {
                modernCanvas = existing.GetComponent<Canvas>();
                if (modernCanvas != null)
                {
                    ConfigureModalCanvas(modernCanvas);
                    return modernCanvas;
                }
            }

            GameObject canvasObject = new GameObject("ModernRogueUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            modernCanvas = canvasObject.GetComponent<Canvas>();
            ConfigureModalCanvas(modernCanvas);
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return modernCanvas;
        }

        private static void ConfigureModalCanvas(Canvas targetCanvas)
        {
            if (targetCanvas == null)
            {
                return;
            }

            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = 500;
            targetCanvas.worldCamera = null;
            targetCanvas.planeDistance = 100f;
        }

        private static GameObject CreateRoot(Transform parent, string name, Vector2 size)
        {
            Transform old = parent.Find(name);
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return root;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(true);
                }

                StandaloneInputModule module = existing.GetComponent<StandaloneInputModule>();
                if (module == null)
                {
                    existing.gameObject.AddComponent<StandaloneInputModule>();
                }

                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void DisableLegacyUiConflicts()
        {
            RuntimeBackpackPanel oldBackpack = FindObjectOfType<RuntimeBackpackPanel>(true);
            if (oldBackpack != null)
            {
                oldBackpack.gameObject.SetActive(false);
                oldBackpack.enabled = false;
            }

            Bagsys.RogueLike.EnhancedBackpackPanel enhanced = FindObjectOfType<Bagsys.RogueLike.EnhancedBackpackPanel>(true);
            if (enhanced != null)
            {
                enhanced.gameObject.SetActive(false);
                enhanced.enabled = false;
            }

            Bagsys.RogueLike.CharacterProgressionPanel progression = FindObjectOfType<Bagsys.RogueLike.CharacterProgressionPanel>(true);
            if (progression != null)
            {
                progression.gameObject.SetActive(false);
                progression.enabled = false;
            }

            Bagsys.RogueLike.CharacterPanelController controller = FindObjectOfType<Bagsys.RogueLike.CharacterPanelController>(true);
            if (controller != null)
            {
                controller.gameObject.SetActive(false);
                controller.enabled = false;
            }
        }
    }

    [DisallowMultipleComponent]
    public class NewSettingsPanel : MonoBehaviour
    {
        private RectTransform contentRoot;

        public void Build(RectTransform root)
        {
            ClearChildren(root);
            Image rootImage = root.gameObject.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(rootImage, new Color(0.025f, 0.032f, 0.045f, 0.94f));
            RuntimeUIVisuals.CreateBlock(root, "HeaderBar", new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.985f), new Color(0.05f, 0.06f, 0.08f, 0.92f));

            Text title = CreateText(root, "Title", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = "游戏设置";
            SetRect(title.rectTransform, new Vector2(0.035f, 0.905f), new Vector2(0.36f, 0.98f));

            UiNavigationHelper.CreateBackButton(
                root,
                () => UIManager.Instance?.GoBackFromSettings(),
                new Vector2(0.82f, 0.905f),
                new Vector2(0.965f, 0.98f));

            GameObject body = new GameObject("SettingsBody", typeof(RectTransform));
            body.transform.SetParent(root, false);
            contentRoot = body.GetComponent<RectTransform>();
            SetRect(contentRoot, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.86f));
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (contentRoot == null)
            {
                return;
            }

            GameSettingsPanelBuilder.Build(contentRoot, new GameSettingsPanelBuilder.Options
            {
                showRunActions = true,
                onRestartRun = () => SoulKnightDirector.Instance?.AbandonRunAndRestart(),
                onReturnToMainMenu = () => SoulKnightDirector.Instance?.AbandonRunAndReturnToMainMenu(),
                onRebuild = RefreshUI
            });
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.94f, 0.96f, 1f, 1f);
            text.supportRichText = true;
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
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }

    [DisallowMultipleComponent]
    public class NewCorePanel : MonoBehaviour
    {
        private Transform slotsRoot;
        private Transform bagRoot;
        private Text summaryText;
        private Text detailText;
        private GameObject contextMenu;
        private NewInventorySystem inventory;

        private void OnEnable()
        {
            inventory = NewInventorySystem.Instance;
            if (inventory != null)
            {
                inventory.Changed += RefreshUI;
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.Changed -= RefreshUI;
            }
        }

        public void Build(RectTransform root)
        {
            ClearChildren(root);
            Image rootImage = root.gameObject.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(rootImage, new Color(0.025f, 0.032f, 0.045f, 0.94f));
            RuntimeUIVisuals.CreateBlock(root, "HeaderBar", new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.985f), new Color(0.05f, 0.06f, 0.08f, 0.92f));

            Text title = CreateText(root, "Title", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = "虫核镶嵌";
            SetRect(title.rectTransform, new Vector2(0.035f, 0.905f), new Vector2(0.36f, 0.98f));

            summaryText = CreateText(root, "Summary", 14, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(summaryText.rectTransform, new Vector2(0.035f, 0.76f), new Vector2(0.46f, 0.875f));

            GameObject leftPane = new GameObject("EquippedPane", typeof(RectTransform), typeof(Image));
            leftPane.transform.SetParent(root, false);
            RuntimeUIVisuals.StyleImage(leftPane.GetComponent<Image>(), new Color(0.018f, 0.023f, 0.032f, 0.82f));
            SetRect(leftPane.GetComponent<RectTransform>(), new Vector2(0.035f, 0.1f), new Vector2(0.46f, 0.74f));

            GameObject grid = new GameObject("CoreSlots", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(leftPane.transform, false);
            SetRect(grid.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(132f, 92f);
            layout.spacing = new Vector2(12f, 12f);
            slotsRoot = grid.transform;

            Text bagTitle = CreateText(root, "BagTitle", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            bagTitle.text = "虫核背包";
            SetRect(bagTitle.rectTransform, new Vector2(0.52f, 0.78f), new Vector2(0.74f, 0.865f));

            GameObject rightPane = new GameObject("BagPane", typeof(RectTransform), typeof(Image));
            rightPane.transform.SetParent(root, false);
            RuntimeUIVisuals.StyleImage(rightPane.GetComponent<Image>(), new Color(0.018f, 0.023f, 0.032f, 0.82f));
            SetRect(rightPane.GetComponent<RectTransform>(), new Vector2(0.52f, 0.36f), new Vector2(0.965f, 0.76f));

            GameObject bag = new GameObject("CoreBag", typeof(RectTransform), typeof(GridLayoutGroup));
            bag.transform.SetParent(rightPane.transform, false);
            SetRect(bag.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            GridLayoutGroup bagLayout = bag.GetComponent<GridLayoutGroup>();
            bagLayout.cellSize = new Vector2(96f, 72f);
            bagLayout.spacing = new Vector2(10f, 10f);
            bagRoot = bag.transform;

            detailText = CreateText(root, "Detail", 15, FontStyle.Normal, TextAnchor.UpperLeft);
            detailText.text = "鼠标悬停虫核查看详情；右键虫核选择装载或卸载。";
            SetRect(detailText.rectTransform, new Vector2(0.52f, 0.08f), new Vector2(0.965f, 0.32f));
            RefreshUI();
        }

        public void RefreshUI()
        {
            inventory = NewInventorySystem.Instance;
            if (inventory == null || slotsRoot == null || bagRoot == null || summaryText == null)
            {
                return;
            }

            ClearChildren(slotsRoot);
            ClearChildren(bagRoot);
            int capacity = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CoreSlotCapacity : 6;
            int wind = inventory.CountEquippedCores(CoreElement.Wind);
            int fire = inventory.CountEquippedCores(CoreElement.Fire);
            int thunder = inventory.CountEquippedCores(CoreElement.Thunder);
            int water = inventory.CountEquippedCores(CoreElement.Water);
            int metal = inventory.CountEquippedCores(CoreElement.Metal);
            summaryText.text =
                "镶嵌：" + inventory.EquippedCores.Count + "/" + capacity + "\n" +
                "风：" + wind + "  火：" + fire + "  雷：" + thunder + "  水：" + water + "  金：" + metal + "\n" +
                "水：水渍/浸润/增伤  火：环绕火球/灼烧\n风：移速/暴击/额外闪避  雷：感电/链电/储伤  金：金币/折扣/落金";

            for (int i = 0; i < capacity; i++)
            {
                CreateEquippedSlot(i);
            }

            for (int i = 0; i < inventory.CoreBag.Count; i++)
            {
                CreateBagSlot(inventory.CoreBag[i]);
            }
        }

        private void CreateEquippedSlot(int index)
        {
            GameObject slot = new GameObject("CoreSlot_" + index, typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(slotsRoot, false);
            Image image = slot.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.08f, 0.095f, 0.92f));

            Text label = CreateText(slot.transform, "Label", 14, FontStyle.Bold, TextAnchor.LowerCenter);
            SetRect(label.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.34f));
            if (index < inventory.EquippedCores.Count)
            {
                CoreInstance instance = inventory.EquippedCores[index];
                CoreData core = GameDataModel.GetCore(instance.templateId);
                RuntimeUIVisuals.AddFrame(slot.transform, "CoreFrame", GameDataModel.GetCoreQualityColor(instance.quality), 0.06f);
                CreateCoreIcon(slot.transform, core, instance.quality);
                label.text = core != null ? core.coreName + "\n" + ResolveElementName(core.element) : "未知虫核";
                CoreCellHandler handler = slot.AddComponent<CoreCellHandler>();
                handler.Initialize(this, instance.instanceId, true);
            }
            else
            {
                label.text = "空槽";
                label.alignment = TextAnchor.MiddleCenter;
                SetRect(label.rectTransform, Vector2.zero, Vector2.one);
            }
        }

        private void CreateBagSlot(CoreInstance instance)
        {
            GameObject slot = new GameObject("CoreBagSlot_" + instance.instanceId, typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(bagRoot, false);
            Image image = slot.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.08f, 0.095f, 0.92f));
            CoreData core = GameDataModel.GetCore(instance.templateId);
            RuntimeUIVisuals.AddFrame(slot.transform, "CoreFrame", GameDataModel.GetCoreQualityColor(instance.quality), 0.06f);
            CreateCoreIcon(slot.transform, core, instance.quality);
            Text label = CreateText(slot.transform, "Label", 13, FontStyle.Bold, TextAnchor.LowerCenter);
            SetRect(label.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.36f));
            label.text = core != null ? core.coreName + "\n" + GameDataModel.GetCoreQualityName(instance.quality) : "未知";
            CoreCellHandler handler = slot.AddComponent<CoreCellHandler>();
            handler.Initialize(this, instance.instanceId, false);
        }

        private static void CreateCoreIcon(Transform parent, CoreData core, CoreQuality quality)
        {
            if (core == null)
            {
                return;
            }

            Sprite icon = Art2DUtility.LoadCoreSprite(core.element, quality);
            if (icon == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("CoreIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.96f));
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
        }

        private CoreInstance FindCore(int instanceId, bool equipped)
        {
            IReadOnlyList<CoreInstance> list = equipped ? inventory.EquippedCores : inventory.CoreBag;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].instanceId == instanceId)
                {
                    return list[i];
                }
            }

            return null;
        }

        private void ShowCoreDetail(int instanceId, bool equipped)
        {
            CoreInstance instance = FindCore(instanceId, equipped);
            if (instance != null && detailText != null)
            {
                detailText.text = instance.GetDescription();
            }
        }

        private void HideCoreDetail()
        {
            if (detailText != null)
            {
                detailText.text = "鼠标悬停虫核查看详情；右键虫核选择装载或卸载。";
            }
        }

        private void ShowCoreMenu(int instanceId, bool equipped, Vector2 screenPosition)
        {
            if (contextMenu != null)
            {
                Destroy(contextMenu);
            }

            contextMenu = new GameObject("CoreContextMenu", typeof(RectTransform), typeof(Image));
            contextMenu.transform.SetParent(transform, false);
            RectTransform rect = contextMenu.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 42f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPosition, null, out Vector2 local);
            rect.anchoredPosition = local;
            contextMenu.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.98f);

            Button button = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(contextMenu.transform, false);
            SetRect(button.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            button.GetComponent<Image>().color = equipped ? new Color(0.36f, 0.16f, 0.16f, 1f) : new Color(0.15f, 0.32f, 0.48f, 1f);
            Text text = CreateText(button.transform, "Text", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            text.text = equipped ? "卸载" : "装载";
            button.onClick.AddListener(() =>
            {
                if (equipped)
                {
                    inventory.UnequipCoreInstance(instanceId);
                }
                else
                {
                    inventory.EquipCoreInstance(instanceId);
                }

                if (contextMenu != null)
                {
                    Destroy(contextMenu);
                }
            });
        }

        private static string ResolveElementName(CoreElement element)
        {
            switch (element)
            {
                case CoreElement.Wind:
                    return "风";
                case CoreElement.Fire:
                    return "火";
                case CoreElement.Thunder:
                    return "雷";
                case CoreElement.Earth:
                    return "土";
                case CoreElement.Water:
                    return "水";
                case CoreElement.Metal:
                    return "金";
                default:
                    return "";
            }
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.94f, 0.96f, 1f, 1f);
            text.supportRichText = true;
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
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private sealed class CoreCellHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            private NewCorePanel panel;
            private int instanceId;
            private bool equipped;

            public void Initialize(NewCorePanel owner, int coreInstanceId, bool isEquipped)
            {
                panel = owner;
                instanceId = coreInstanceId;
                equipped = isEquipped;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                panel.ShowCoreDetail(instanceId, equipped);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                panel.HideCoreDetail();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    panel.ShowCoreMenu(instanceId, equipped, eventData.position);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public class NewTreasurePanel : MonoBehaviour
    {
        private Transform listRoot;
        private Text summaryText;
        private RunProgressionSystem run;

        private void OnEnable()
        {
            run = RunProgressionSystem.Instance;
            if (run != null)
            {
                run.Changed += RefreshUI;
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (run != null)
            {
                run.Changed -= RefreshUI;
            }
        }

        public void Build(RectTransform root)
        {
            ClearChildren(root);
            Image rootImage = root.gameObject.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(rootImage, new Color(0.025f, 0.032f, 0.045f, 0.94f));
            RuntimeUIVisuals.CreateBlock(root, "HeaderBar", new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.985f), new Color(0.05f, 0.06f, 0.08f, 0.92f));

            Text title = CreateText(root, "Title", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = "宝物收藏";
            SetRect(title.rectTransform, new Vector2(0.035f, 0.905f), new Vector2(0.36f, 0.98f));

            RuntimeUIVisuals.CreateBlock(root, "SummaryBar", new Vector2(0.035f, 0.74f), new Vector2(0.965f, 0.865f), new Color(0.018f, 0.023f, 0.032f, 0.82f));
            summaryText = CreateText(root, "Summary", 14, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(summaryText.rectTransform, new Vector2(0.05f, 0.745f), new Vector2(0.95f, 0.86f));

            GameObject list = new GameObject("TreasureList", typeof(RectTransform), typeof(GridLayoutGroup));
            list.transform.SetParent(root, false);
            SetRect(list.GetComponent<RectTransform>(), new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.72f));
            GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280f, 86f);
            grid.spacing = new Vector2(12f, 12f);
            listRoot = list.transform;
            RefreshUI();
        }

        public void RefreshUI()
        {
            run = RunProgressionSystem.Instance;
            if (run == null || summaryText == null || listRoot == null)
            {
                return;
            }

            ClearChildren(listRoot);
            summaryText.text =
                "职业：" + RunProgressionSystem.GetClassName(run.CurrentClass) +
                "    Boss 精华：" + run.MetaBossCurrency +
                "    商店刷新：" + run.ShopRefreshes +
                "    升级刷新：" + run.UpgradeRerolls +
                "    虫核槽：" + run.CoreSlotCapacity;

            if (run.Treasures.Count == 0)
            {
                CreateTreasureCard(null, "暂无宝物", "宝物房、商店和 Boss 会提供宝物。");
                return;
            }

            for (int i = 0; i < run.Treasures.Count; i++)
            {
                TreasureData treasure = run.Treasures[i];
                if (treasure != null)
                {
                    CreateTreasureCard(treasure, treasure.treasureName, treasure.description);
                }
            }
        }

        private void CreateTreasureCard(TreasureData treasure, string title, string description)
        {
            GameObject card = new GameObject("TreasureCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(listRoot, false);
            Image image = card.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.08f, 0.095f, 0.92f));
            RuntimeUIVisuals.AddFrame(card.transform, "TreasureFrame", new Color(0.92f, 0.72f, 0.22f, 0.55f), 0.04f);

            Sprite icon = treasure != null ? Art2DUtility.LoadTreasureSprite(treasure) : null;
            if (icon != null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(card.transform, false);
                SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.22f, 0.88f));
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
            }

            Text text = CreateText(card.transform, "Text", 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.alignment = TextAnchor.MiddleLeft;
            SetRect(text.rectTransform, new Vector2(icon != null ? 0.24f : 0.06f, 0.06f), new Vector2(0.96f, 0.94f));
            text.text = title + "\n<size=12>" + description + "</size>";
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.94f, 0.96f, 1f, 1f);
            text.supportRichText = true;
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
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
