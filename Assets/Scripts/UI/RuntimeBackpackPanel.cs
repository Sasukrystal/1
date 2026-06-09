using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeBackpackPanel : MonoBehaviour
{
    private const string PanelObjectName = "BackpackRuntime";
    private const float RecentPickupDuration = 3f;

    public static RuntimeBackpackPanel Instance { get; private set; }

    private sealed class BackpackEntry
    {
        public int SlotIndex;
        public string OwnerName;
        public Item Item;
        public int Amount;
        public bool IsRecentPickup;
    }

    private GameObject panelRoot;
    private Transform listContent;
    private Text detailText;
    private Text headerText;
    private Canvas targetCanvas;
    private bool isVisible;
    private int selectedItemId = -1;

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

    private void OnEnable()
    {
        EnsurePanel();
    }

    private void Update()
    {
        if (panelRoot == null)
        {
            EnsurePanel();
        }

        if (isVisible)
        {
            RefreshPanel();
        }
    }

    public void TogglePanel()
    {
        EnsurePanel();

        if (isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    public void ShowPanel()
    {
        EnsurePanel();

        if (panelRoot != null)
        {
            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);
        }

        isVisible = true;
        RefreshPanel();
    }

    public void HidePanel()
    {
        HidePanelImmediate();
    }

    private void EnsurePanel()
    {
        if (panelRoot != null)
        {
            return;
        }

        targetCanvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panelRoot = new GameObject(PanelObjectName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(740f, 320f);
        rootRect.anchoredPosition = new Vector2(16f, -16f);

        Image background = panelRoot.GetComponent<Image>();
        background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        background.raycastTarget = true;

        GameObject titleObject = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(panelRoot.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.86f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(16f, 8f);
        titleRect.offsetMax = new Vector2(-16f, -8f);

        Text titleText = titleObject.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.text = "背包状态";

        GameObject headerObject = new GameObject("HeaderText", typeof(RectTransform), typeof(Text));
        headerObject.transform.SetParent(panelRoot.transform, false);
        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.73f);
        headerRect.anchorMax = new Vector2(1f, 0.86f);
        headerRect.offsetMin = new Vector2(16f, 4f);
        headerRect.offsetMax = new Vector2(-16f, -6f);

        headerText = headerObject.GetComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 14;
        headerText.color = new Color(0.9f, 0.95f, 1f, 1f);
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.text = string.Empty;

        GameObject listPanel = new GameObject("ListPanel", typeof(RectTransform), typeof(Image));
        listPanel.transform.SetParent(panelRoot.transform, false);
        RectTransform listPanelRect = listPanel.GetComponent<RectTransform>();
        listPanelRect.anchorMin = new Vector2(0f, 0f);
        listPanelRect.anchorMax = new Vector2(0.52f, 0.73f);
        listPanelRect.offsetMin = new Vector2(16f, 12f);
        listPanelRect.offsetMax = new Vector2(-8f, -8f);

        Image listBackground = listPanel.GetComponent<Image>();
        listBackground.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(listPanel.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.02f);

        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        listContent = contentObject.transform;

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(2, 2, 2, 2);
        layout.spacing = 4f;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
        detailPanel.transform.SetParent(panelRoot.transform, false);
        RectTransform detailPanelRect = detailPanel.GetComponent<RectTransform>();
        detailPanelRect.anchorMin = new Vector2(0.54f, 0f);
        detailPanelRect.anchorMax = new Vector2(1f, 0.73f);
        detailPanelRect.offsetMin = new Vector2(8f, 12f);
        detailPanelRect.offsetMax = new Vector2(-16f, -8f);

        Image detailBackground = detailPanel.GetComponent<Image>();
        detailBackground.color = new Color(0.1f, 0.11f, 0.16f, 0.95f);

        GameObject detailTextObject = new GameObject("DetailText", typeof(RectTransform), typeof(Text));
        detailTextObject.transform.SetParent(detailPanel.transform, false);
        RectTransform detailTextRect = detailTextObject.GetComponent<RectTransform>();
        detailTextRect.anchorMin = Vector2.zero;
        detailTextRect.anchorMax = Vector2.one;
        detailTextRect.offsetMin = new Vector2(12f, 12f);
        detailTextRect.offsetMax = new Vector2(-12f, -12f);

        detailText = detailTextObject.GetComponent<Text>();
        detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailText.fontSize = 15;
        detailText.alignment = TextAnchor.UpperLeft;
        detailText.color = Color.white;
        detailText.supportRichText = true;
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;
        detailText.text = "点击左侧物品查看详情。";

        panelRoot.SetActive(false);
    }

    private void RefreshPanel()
    {
        if (headerText == null || detailText == null || listContent == null)
        {
            return;
        }

        InventoryManager inventoryManager = InventoryManager.Instance;
        global::Player legacyPlayer = UnityEngine.Object.FindObjectOfType<global::Player>(true);
        ItemUI pickedItem = inventoryManager != null ? inventoryManager.PickedItem : null;
        Slot[] slots = UnityEngine.Object.FindObjectsOfType<Slot>(true);

        List<BackpackEntry> entries = BuildEntries(slots, inventoryManager);
        if (entries.Count == 0)
        {
            selectedItemId = -1;
        }
        else if (!EntryExists(entries, selectedItemId))
        {
            selectedItemId = entries[0].Item.ID;
        }

        headerText.text = BuildHeaderText(legacyPlayer, pickedItem, slots, entries);
        RebuildList(entries, inventoryManager);

        BackpackEntry selectedEntry = FindSelectedEntry(entries, selectedItemId);
        if (selectedEntry != null)
        {
            detailText.text = BuildDetailText(selectedEntry, inventoryManager);
        }
        else
        {
            detailText.text = "点击左侧物品查看详情。";
        }
    }

    private static string BuildHeaderText(global::Player legacyPlayer, ItemUI pickedItem, Slot[] slots, List<BackpackEntry> entries)
    {
        string coinText = legacyPlayer != null ? legacyPlayer.CoinAmount.ToString() : "0";
        string pickedText = pickedItem != null && pickedItem.Item != null
            ? string.Format("{0} x{1}", pickedItem.Item.Name, pickedItem.Amount)
            : "无";

        int totalItemCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            totalItemCount += Mathf.Max(1, entries[i].Amount);
        }

        int recentCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsRecentPickup)
            {
                recentCount++;
            }
        }

        return string.Format(
            "金币：{0}    当前拾取：{1}    占用槽位：{2}/{3}    条目数：{4}    NEW：{5}",
            coinText,
            pickedText,
            entries.Count,
            slots.Length,
            totalItemCount,
            recentCount);
    }

    private void RebuildList(List<BackpackEntry> entries, InventoryManager inventoryManager)
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            BackpackEntry entry = entries[i];
            GameObject row = new GameObject($"Entry_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(listContent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 30f);

            Image rowBackground = row.GetComponent<Image>();
            bool isSelected = entry.Item.ID == selectedItemId;
            rowBackground.color = isSelected ? new Color(0.2f, 0.35f, 0.65f, 0.95f) : new Color(0.16f, 0.18f, 0.24f, 0.95f);

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 30f;
            layoutElement.minHeight = 30f;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(row.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 2f);
            textRect.offsetMax = new Vector2(-10f, -2f);

            Text rowText = textObject.GetComponent<Text>();
            rowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rowText.fontSize = 14;
            rowText.alignment = TextAnchor.MiddleLeft;
            rowText.color = Color.white;
            rowText.supportRichText = true;
            rowText.text = BuildRowLabel(entry);

            Button button = row.GetComponent<Button>();
            int capturedItemId = entry.Item.ID;
            button.onClick.AddListener(() =>
            {
                selectedItemId = capturedItemId;
                if (isVisible)
                {
                    RefreshPanel();
                }
            });
        }

        if (entries.Count == 0)
        {
            GameObject emptyObject = new GameObject("EmptyEntry", typeof(RectTransform), typeof(Text));
            emptyObject.transform.SetParent(listContent, false);
            RectTransform emptyRect = emptyObject.GetComponent<RectTransform>();
            emptyRect.sizeDelta = new Vector2(0f, 28f);

            Text emptyText = emptyObject.GetComponent<Text>();
            emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyText.fontSize = 14;
            emptyText.alignment = TextAnchor.MiddleLeft;
            emptyText.color = Color.white;
            emptyText.text = "暂无物品";
        }
    }

    private static string BuildRowLabel(BackpackEntry entry)
    {
        string color = GetQualityColor(entry.Item.Quality);
        string qualityText = GetQualityLabel(entry.Item.Quality);
        string newTag = entry.IsRecentPickup ? " <color=#FFD84D>[NEW]</color>" : string.Empty;
        return string.Format("<color={0}>{1}</color> x{2} <size=10>[{3}]</size>{4}", color, entry.Item.Name, entry.Amount, qualityText, newTag);
    }

    private static string BuildDetailText(BackpackEntry entry, InventoryManager inventoryManager)
    {
        string recentTag = entry.IsRecentPickup ? "<color=#FFD84D>最近拾取</color>" : "普通条目";
        string recentInfo = inventoryManager != null && inventoryManager.RecentPickupItemId == entry.Item.ID
            ? string.Format("\n最近拾取计时：{0:0.0}s / {1:0.0}s", inventoryManager.RecentPickupAge, RecentPickupDuration)
            : string.Empty;

        return string.Format(
            "<color={0}>{1}</color>\n品质：{2}\n类型：{3}\n数量：{4}\n位置：{5}\n状态：{6}{7}\n\n{8}",
            GetQualityColor(entry.Item.Quality),
            entry.Item.Name,
            GetQualityLabel(entry.Item.Quality),
            entry.Item.Type,
            entry.Amount,
            entry.OwnerName,
            recentTag,
            recentInfo,
            entry.Item.GetToolTipText());
    }

    private static List<BackpackEntry> BuildEntries(Slot[] slots, InventoryManager inventoryManager)
    {
        List<BackpackEntry> entries = new List<BackpackEntry>();
        if (slots == null)
        {
            return entries;
        }

        int recentPickupId = inventoryManager != null ? inventoryManager.RecentPickupItemId : -1;
        float recentPickupAge = inventoryManager != null ? inventoryManager.RecentPickupAge : float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null || slot.transform.childCount <= 0)
            {
                continue;
            }

            ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
            if (itemUI == null || itemUI.Item == null)
            {
                continue;
            }

            string ownerName = slot.transform.parent != null ? slot.transform.parent.name : "Unknown";
            entries.Add(new BackpackEntry
            {
                SlotIndex = i,
                OwnerName = ownerName,
                Item = itemUI.Item,
                Amount = itemUI.Amount,
                IsRecentPickup = itemUI.Item.ID == recentPickupId && recentPickupAge <= RecentPickupDuration
            });
        }

        entries.Sort((left, right) =>
        {
            int recentCompare = right.IsRecentPickup.CompareTo(left.IsRecentPickup);
            if (recentCompare != 0)
            {
                return recentCompare;
            }

            int qualityCompare = ((int)right.Item.Quality).CompareTo((int)left.Item.Quality);
            if (qualityCompare != 0)
            {
                return qualityCompare;
            }

            return string.Compare(left.Item.Name, right.Item.Name, StringComparison.Ordinal);
        });

        return entries;
    }

    private static bool EntryExists(List<BackpackEntry> entries, int itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Item.ID == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private static BackpackEntry FindSelectedEntry(List<BackpackEntry> entries, int itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Item.ID == itemId)
            {
                return entries[i];
            }
        }

        return null;
    }

    private static string GetQualityLabel(Item.ItemQuality quality)
    {
        switch (quality)
        {
            case Item.ItemQuality.Uncommon:
                return "罕见";
            case Item.ItemQuality.Rare:
                return "稀有";
            case Item.ItemQuality.Epic:
                return "史诗";
            case Item.ItemQuality.Legendary:
                return "传说";
            case Item.ItemQuality.Artifact:
                return "神器";
            default:
                return "普通";
        }
    }

    private static string GetQualityColor(Item.ItemQuality quality)
    {
        switch (quality)
        {
            case Item.ItemQuality.Uncommon:
                return "#7CFF7C";
            case Item.ItemQuality.Rare:
                return "#4F7BFF";
            case Item.ItemQuality.Epic:
                return "#C45DFF";
            case Item.ItemQuality.Legendary:
                return "#FFB34D";
            case Item.ItemQuality.Artifact:
                return "#FF5E5E";
            default:
                return "#FFFFFF";
        }
    }

    private void HidePanelImmediate()
    {
        isVisible = false;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}