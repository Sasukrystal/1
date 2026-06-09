using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class EnhancedBackpackPanel : MonoBehaviour
    {
        public static EnhancedBackpackPanel Instance { get; private set; }

        private GameObject root;
        private Text statsText;
        private Text equipmentText;
        private Text detailText;
        private Text coinText;
        private Transform gridRoot;
        private Canvas canvas;
        private bool visible;
        private int selectedItemId = -1;

        private sealed class Entry
        {
            public Slot Slot;
            public ItemUI ItemUI;
            public Item Item;
            public int Amount;
        }

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

            root = new GameObject("EnhancedBackpackPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(980f, 560f);
            root.GetComponent<Image>().color = new Color(0.045f, 0.052f, 0.07f, 0.96f);

            Text title = CreateText(root.transform, "Title", new Vector2(18f, -54f), new Vector2(-18f, -12f), 24, FontStyle.Bold);
            title.text = "背包 / 装备";

            GameObject left = CreatePanel(root.transform, "LeftStatusAndEquipment", new Vector2(0f, 0f), new Vector2(0.38f, 0.88f), new Vector2(18f, 18f), new Vector2(-8f, -62f));
            GameObject right = CreatePanel(root.transform, "RightInventoryGrid", new Vector2(0.38f, 0f), new Vector2(1f, 0.88f), new Vector2(8f, 18f), new Vector2(-18f, -62f));

            Text preview = CreateText(left.transform, "PreviewBox", new Vector2(16f, -130f), new Vector2(-16f, -16f), 18, FontStyle.Bold);
            preview.text = "2.5D 角色预览\nPlayer_Front / Player_Side";
            preview.alignment = TextAnchor.MiddleCenter;

            statsText = CreateText(left.transform, "StatsText", new Vector2(16f, -260f), new Vector2(-16f, -142f), 15, FontStyle.Normal);
            equipmentText = CreateText(left.transform, "EquipmentText", new Vector2(16f, -480f), new Vector2(-16f, -272f), 14, FontStyle.Normal);
            detailText = CreateText(right.transform, "DetailText", new Vector2(16f, 16f), new Vector2(-16f, 110f), 14, FontStyle.Normal);
            coinText = CreateText(right.transform, "CoinText", new Vector2(16f, -44f), new Vector2(-16f, -12f), 16, FontStyle.Bold);
            coinText.alignment = TextAnchor.MiddleRight;

            GameObject gridPanel = CreatePanel(right.transform, "GridPanel", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 118f), new Vector2(-16f, -54f));
            GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(gridPanel.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(10f, 10f);
            gridRect.offsetMax = new Vector2(-10f, -10f);
            GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(92f, 54f);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridRoot = grid.transform;
        }

        private void Refresh()
        {
            List<Entry> entries = BuildEntries();
            if (selectedItemId < 0 && entries.Count > 0)
            {
                selectedItemId = entries[0].Item.ID;
            }

            RefreshStats();
            RefreshEquipment();
            RefreshGrid(entries);
            RefreshDetail(entries);
        }

        private List<Entry> BuildEntries()
        {
            List<Entry> entries = new List<Entry>();
            Knapsack knapsack = Knapsack.Instance != null ? Knapsack.Instance : Object.FindObjectOfType<Knapsack>(true);
            if (knapsack == null)
            {
                return entries;
            }

            Slot[] slots = knapsack.GetComponentsInChildren<Slot>(true);
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

                entries.Add(new Entry { Slot = slot, ItemUI = itemUI, Item = itemUI.Item, Amount = itemUI.Amount });
            }

            entries.Sort((left, right) => ((int)right.Item.Quality).CompareTo((int)left.Item.Quality));
            return entries;
        }

        private void RefreshStats()
        {
            PlayerStats playerStats = Object.FindObjectOfType<PlayerStats>(true);
            int hp = playerStats != null ? playerStats.CurrentHP : 0;
            int maxHp = playerStats != null ? playerStats.MaxHP : 0;
            int atk = playerStats != null ? playerStats.ATK : 0;
            int def = playerStats != null ? playerStats.DEF : 0;
            int coins = playerStats != null ? playerStats.CoinAmount : 0;
            coinText.text = "金币 " + coins;
            statsText.text = string.Format("生命 {0}/{1}\n攻击 {2}\n防御 {3}\n移速 来自 PlayerController\n攻速 来自 PlayerAttack\n\n右侧装备/武器：点击或右键可尝试装备", hp, maxHp, atk, def);
        }

        private void RefreshEquipment()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("装备栏");
            CharacterPanel panel = CharacterPanel.Instance != null ? CharacterPanel.Instance : Object.FindObjectOfType<CharacterPanel>(true);
            if (panel == null)
            {
                builder.AppendLine("未找到 Legacy CharacterPanel");
                equipmentText.text = builder.ToString();
                return;
            }

            EquipmentSlot[] slots = panel.GetComponentsInChildren<EquipmentSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                EquipmentSlot slot = slots[i];
                string label = slot.wpType != Weapon.WeaponType.None ? slot.wpType.ToString() : slot.equipType.ToString();
                string itemName = "空";
                if (slot.transform.childCount > 0)
                {
                    ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
                    if (itemUI != null && itemUI.Item != null)
                    {
                        itemName = itemUI.Item.Name;
                    }
                }

                builder.AppendLine(label + " : " + itemName);
            }

            equipmentText.text = builder.ToString();
        }

        private void RefreshGrid(List<Entry> entries)
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                GameObject cell = new GameObject("ItemCell", typeof(RectTransform), typeof(Image), typeof(BackpackCellClickHandler));
                cell.transform.SetParent(gridRoot, false);
                cell.GetComponent<Image>().color = entry.Item.ID == selectedItemId ? new Color(0.18f, 0.32f, 0.56f, 0.95f) : new Color(0.11f, 0.13f, 0.18f, 0.95f);
                BackpackCellClickHandler handler = cell.GetComponent<BackpackCellClickHandler>();
                handler.Owner = this;
                handler.Entry = entry;

                Text label = CreateText(cell.transform, "Label", new Vector2(6f, 4f), new Vector2(-6f, -4f), 12, FontStyle.Bold);
                label.alignment = TextAnchor.MiddleCenter;
                label.text = string.Format("<color={0}>{1}</color>\nx{2}", GetQualityColor(entry.Item.Quality), entry.Item.Name, entry.Amount);
            }
        }

        private void RefreshDetail(List<Entry> entries)
        {
            Entry selected = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Item.ID == selectedItemId)
                {
                    selected = entries[i];
                    break;
                }
            }

            detailText.text = selected != null
                ? selected.Item.GetToolTipText() + "\n\n操作：左键选中；右键/再次点击装备武器或装备。"
                : "暂无物品。";
        }

        private void SelectOrEquip(Entry entry, bool equip)
        {
            if (entry == null || entry.Item == null)
            {
                return;
            }

            selectedItemId = entry.Item.ID;
            if (equip)
            {
                TryEquip(entry);
            }

            Refresh();
        }

        private void TryEquip(Entry entry)
        {
            if (!(entry.Item is Weapon) && !(entry.Item is Equipment))
            {
                return;
            }

            CharacterPanel panel = CharacterPanel.Instance != null ? CharacterPanel.Instance : Object.FindObjectOfType<CharacterPanel>(true);
            if (panel == null || entry.ItemUI == null)
            {
                return;
            }

            Item item = entry.Item;
            entry.ItemUI.ReduceAmount(1);
            if (entry.ItemUI.Amount <= 0)
            {
                Destroy(entry.ItemUI.gameObject);
            }

            panel.PutOn(item);
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
            text.supportRichText = true;
            text.color = new Color(0.93f, 0.96f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static string GetQualityColor(Item.ItemQuality quality)
        {
            switch (quality)
            {
                case Item.ItemQuality.Uncommon: return "#7CFF7C";
                case Item.ItemQuality.Rare: return "#4F8BFF";
                case Item.ItemQuality.Epic: return "#C45DFF";
                case Item.ItemQuality.Legendary: return "#FFB34D";
                case Item.ItemQuality.Artifact: return "#FF5E5E";
                default: return "#FFFFFF";
            }
        }

        private sealed class BackpackCellClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public EnhancedBackpackPanel Owner;
            public Entry Entry;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (Owner != null)
                {
                    Owner.SelectOrEquip(Entry, eventData.button == PointerEventData.InputButton.Right);
                }
            }
        }
    }
}
