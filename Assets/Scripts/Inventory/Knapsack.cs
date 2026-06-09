using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Knapsack : Inventory
{

    #region 单例模式
    private static Knapsack _instance;
    public static Knapsack Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    private Text summaryText;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public override void Start()
    {
        base.Start();
        EnsureSummaryText();
        RefreshSummaryText();
    }

    protected override void Update()
    {
        base.Update();
        RefreshSummaryText();
    }

    private void EnsureSummaryText()
    {
        if (summaryText != null)
        {
            return;
        }

        Transform existing = transform.Find("SummaryPanel/SummaryText");
        if (existing != null)
        {
            summaryText = existing.GetComponent<Text>();
            if (summaryText != null)
            {
                return;
            }
        }

        GameObject panelObject = new GameObject("SummaryPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(280f, 120f);
        panelRect.anchoredPosition = new Vector2(-12f, -12f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject textObject = new GameObject("SummaryText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        summaryText = textObject.GetComponent<Text>();
        summaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        summaryText.fontSize = 14;
        summaryText.color = Color.white;
        summaryText.alignment = TextAnchor.UpperLeft;
        summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        summaryText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void RefreshSummaryText()
    {
        if (summaryText == null)
        {
            return;
        }

        Slot[] slots = GetComponentsInChildren<Slot>(true);
        int occupiedSlots = 0;
        int totalItemCount = 0;
        string topItemName = "无";

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null || slot.transform.childCount <= 0)
            {
                continue;
            }

            occupiedSlots++;
            ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
            if (itemUI != null && itemUI.Item != null)
            {
                totalItemCount += Mathf.Max(1, itemUI.Amount);
                if (topItemName == "无")
                {
                    topItemName = itemUI.Item.Name;
                }
            }
        }

        summaryText.text = string.Format(
            "背包概览\n已占用：{0}/{1}\n物品总数：{2}\n首个物品：{3}\n\n提示：\n左键拖拽整理\nF 直接拾取地面物品",
            occupiedSlots,
            slots.Length,
            totalItemCount,
            topItemName);
    }
}
