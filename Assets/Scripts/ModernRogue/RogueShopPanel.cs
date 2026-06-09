using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    public sealed class RogueShopPanel : MonoBehaviour
    {
        private readonly List<ShopEntry> merchantGoods = new List<ShopEntry>();
        private RectTransform playerDropZone;
        private RectTransform merchantDropZone;
        private Transform playerList;
        private Transform merchantList;
        private Text goldText;
        private Text detailText;
        private SoulKnightDirector director;

        public static void Open(SoulKnightDirector owner)
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                return;
            }

            GameObject old = GameObject.Find("RogueShopPanel");
            if (old != null)
            {
                Destroy(old);
            }

            GameObject obj = new GameObject("RogueShopPanel", typeof(RectTransform), typeof(Image), typeof(RogueShopPanel), typeof(ModalPauseToken));
            obj.transform.SetParent(canvas.transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1040f, 610f);
            rect.anchoredPosition = Vector2.zero;
            Image rootImage = obj.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(rootImage, new Color(0.06f, 0.07f, 0.1f, 0.98f));
            RuntimeUIVisuals.AddFrame(obj.transform, "ShopFrame", new Color(0.72f, 0.52f, 0.18f, 0.85f), 0.012f);

            RogueShopPanel panel = obj.GetComponent<RogueShopPanel>();
            panel.director = owner;
            ModalPanelNavigation navigation = obj.AddComponent<ModalPanelNavigation>();
            navigation.Initialize(obj);
            panel.Build(navigation);
        }

        private void Build(ModalPanelNavigation navigation)
        {
            EnsureGoods();
            RuntimeUIVisuals.CreateBlock(transform, "HeaderBar", new Vector2(0.02f, 0.9f), new Vector2(0.98f, 0.985f), new Color(0.1f, 0.07f, 0.04f, 0.98f));
            Text title = CreateText(transform, "Title", "牢骚商人的货摊", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.color = new Color(1f, 0.9f, 0.62f, 1f);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.905f), new Vector2(0.46f, 0.98f));

            goldText = CreateText(transform, "Gold", "", 20, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(goldText.rectTransform, new Vector2(0.55f, 0.91f), new Vector2(0.84f, 0.985f));

            Button close = CreateButton(transform, "Close", "返回上一级");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.86f, 0.92f), new Vector2(0.965f, 0.98f));
            close.onClick.AddListener(navigation.Close);

            playerDropZone = CreateColumn("我的物品", new Vector2(0.035f, 0.28f), new Vector2(0.47f, 0.88f), out playerList);
            merchantDropZone = CreateColumn("商人货架", new Vector2(0.53f, 0.28f), new Vector2(0.965f, 0.88f), out merchantList);

            detailText = CreateText(transform, "Detail", "仅售消耗品、材料、虫核与宝物。高级装备请回基地武器铺升级。\n鼠标悬停查看详情，右键买卖，或拖到对侧完成交易。", 15, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(detailText.rectTransform, new Vector2(0.035f, 0.03f), new Vector2(0.965f, 0.22f));

            Refresh();
        }

        private RectTransform CreateColumn(string title, Vector2 min, Vector2 max, out Transform listRoot)
        {
            GameObject column = new GameObject(title, typeof(RectTransform), typeof(Image));
            column.transform.SetParent(transform, false);
            SetRect(column.GetComponent<RectTransform>(), min, max);
            Image columnImage = column.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(columnImage, new Color(0.05f, 0.06f, 0.09f, 0.92f));

            Text label = CreateText(column.transform, "Title", title, 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.99f));

            GameObject list = new GameObject("List", typeof(RectTransform), typeof(GridLayoutGroup));
            list.transform.SetParent(column.transform, false);
            SetRect(list.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.88f));
            GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(124f, 74f);
            grid.spacing = new Vector2(10f, 10f);
            listRoot = list.transform;
            return column.GetComponent<RectTransform>();
        }

        private void Refresh()
        {
            UpdateGold();
            Clear(playerList);
            Clear(merchantList);

            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                ItemSlot slot = inventory.Slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                ItemData item = GameDataModel.GetItem(slot.itemId);
                if (item != null)
                {
                    CreateCell(playerList, ShopEntry.PlayerItem(i, item, slot.count), false);
                }
            }

            for (int i = 0; i < inventory.CoreBag.Count; i++)
            {
                if (inventory.CoreBag[i] != null)
                {
                    CreateCell(playerList, ShopEntry.PlayerCore(inventory.CoreBag[i], false), false);
                }
            }

            for (int i = 0; i < inventory.EquippedCores.Count; i++)
            {
                if (inventory.EquippedCores[i] != null)
                {
                    CreateCell(playerList, ShopEntry.PlayerCore(inventory.EquippedCores[i], true), false);
                }
            }

            for (int i = 0; i < merchantGoods.Count; i++)
            {
                CreateCell(merchantList, merchantGoods[i], true);
            }
        }

        private void CreateCell(Transform parent, ShopEntry entry, bool merchantSide)
        {
            GameObject obj = new GameObject("ShopCell", typeof(RectTransform), typeof(Image), typeof(ShopCellHandler));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, merchantSide ? new Color(0.16f, 0.2f, 0.28f, 0.96f) : new Color(0.12f, 0.18f, 0.24f, 0.9f));

            if (entry.kind == ShopEntryKind.Core)
            {
                CoreData core = entry.coreId > 0
                    ? GameDataModel.GetCore(entry.coreId)
                    : ResolvePlayerCoreData(entry.coreInstanceId);
                CoreQuality quality = ResolveEntryCoreQuality(entry);
                if (core != null)
                {
                    Sprite icon = Art2DUtility.LoadCoreSprite(core.element, quality);
                    if (icon != null)
                    {
                        GameObject iconObject = new GameObject("CoreIcon", typeof(RectTransform), typeof(Image));
                        iconObject.transform.SetParent(obj.transform, false);
                        SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.34f, 0.96f));
                        Image iconImage = iconObject.GetComponent<Image>();
                        iconImage.sprite = icon;
                        iconImage.color = Color.white;
                        iconImage.preserveAspect = true;
                    }
                }
            }
            else if (entry.kind == ShopEntryKind.Treasure)
            {
                Sprite icon = Art2DUtility.LoadTreasureSprite("Treasures/Economy/Treasure_GoldenPurse");
                if (icon != null)
                {
                    GameObject iconObject = new GameObject("TreasureIcon", typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(obj.transform, false);
                    SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.34f, 0.96f));
                    Image iconImage = iconObject.GetComponent<Image>();
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
            }
            else if (entry.kind == ShopEntryKind.Item)
            {
                ItemData item = GameDataModel.GetItem(entry.itemId);
                Sprite icon = Art2DUtility.LoadItemSprite(item);
                if (icon != null)
                {
                    GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(obj.transform, false);
                    SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.34f, 0.96f));
                    Image iconImage = iconObject.GetComponent<Image>();
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
            }

            Text text = CreateText(obj.transform, "Text", entry.Label + "\n" + (merchantSide ? "买 " : "卖 ") + entry.Price + " 金", entry.kind == ShopEntryKind.Item ? 13 : 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.36f, 0.04f), new Vector2(0.96f, 0.96f));
            obj.GetComponent<ShopCellHandler>().Initialize(this, entry, merchantSide);
        }

        private static CoreData ResolvePlayerCoreData(int instanceId)
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return null;
            }

            for (int i = 0; i < inventory.CoreBag.Count; i++)
            {
                CoreInstance core = inventory.CoreBag[i];
                if (core != null && core.instanceId == instanceId)
                {
                    return GameDataModel.GetCore(core.templateId);
                }
            }

            for (int i = 0; i < inventory.EquippedCores.Count; i++)
            {
                CoreInstance core = inventory.EquippedCores[i];
                if (core != null && core.instanceId == instanceId)
                {
                    return GameDataModel.GetCore(core.templateId);
                }
            }

            return null;
        }

        private static CoreQuality ResolveEntryCoreQuality(ShopEntry entry)
        {
            if (entry.coreInstanceId <= 0)
            {
                return CoreQuality.Rare;
            }

            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return CoreQuality.Rare;
            }

            for (int i = 0; i < inventory.CoreBag.Count; i++)
            {
                CoreInstance core = inventory.CoreBag[i];
                if (core != null && core.instanceId == entry.coreInstanceId)
                {
                    return core.quality;
                }
            }

            for (int i = 0; i < inventory.EquippedCores.Count; i++)
            {
                CoreInstance core = inventory.EquippedCores[i];
                if (core != null && core.instanceId == entry.coreInstanceId)
                {
                    return core.quality;
                }
            }

            return CoreQuality.Rare;
        }

        public void ShowDetail(ShopEntry entry, bool merchantSide)
        {
            string action = merchantSide ? "右键购买，或拖到左侧。" : "右键卖出，或拖到右侧。";
            detailText.text = entry.Description + "\n\n价格：" + entry.Price + " 金    " + action;
        }

        public void ClearDetail()
        {
            detailText.text = "鼠标悬停查看详情。右键买卖，或拖到对侧完成交易。";
        }

        public void TryTrade(ShopEntry entry, bool merchantSide)
        {
            if (merchantSide)
            {
                Buy(entry);
            }
            else
            {
                Sell(entry);
            }
        }

        public bool IsOverOppositeZone(Vector2 screenPoint, bool merchantSide)
        {
            RectTransform target = merchantSide ? playerDropZone : merchantDropZone;
            return target != null && RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, null);
        }

        private void Buy(ShopEntry entry)
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null || !inventory.SpendCoins(entry.Price))
            {
                director?.ShowPickupTip("金币不足");
                UpdateGold();
                return;
            }

            if (entry.kind == ShopEntryKind.Item)
            {
                inventory.AddItem(entry.itemId, Mathf.Max(1, entry.count));
            }
            else if (entry.kind == ShopEntryKind.Core)
            {
                inventory.AddCore(entry.coreId, CoreQuality.Rare);
            }
            else if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.AddRandomTreasure();
            }

            merchantGoods.Remove(entry);
            director?.ShowPickupTip("购买：" + entry.Label);
            Refresh();
        }

        private void Sell(ShopEntry entry)
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            bool removed = false;
            if (entry.kind == ShopEntryKind.Item)
            {
                inventory.RemoveItem(entry.slotIndex);
                removed = true;
            }
            else if (entry.kind == ShopEntryKind.Core)
            {
                removed = inventory.RemoveCoreInstance(entry.coreInstanceId);
            }

            if (!removed)
            {
                return;
            }

            inventory.EarnCoins(entry.Price);
            director?.ShowPickupTip("卖出：" + entry.Label + " +" + entry.Price + " 金");
            Refresh();
        }

        private void EnsureGoods()
        {
            if (merchantGoods.Count > 0)
            {
                return;
            }

            merchantGoods.Add(ShopEntry.MerchantItem(2, 1, 26));
            merchantGoods.Add(ShopEntry.MerchantItem(1, 8, 18));
            merchantGoods.Add(ShopEntry.MerchantCore(Random.Range(101, 106), 72));
            merchantGoods.Add(ShopEntry.MerchantCore(Random.Range(101, 106), 72));
            merchantGoods.Add(ShopEntry.MerchantTreasure(110));

            for (int i = merchantGoods.Count - 1; i >= 0; i--)
            {
                ShopEntry entry = merchantGoods[i];
                if (entry.kind != ShopEntryKind.Item)
                {
                    continue;
                }

                ItemData item = GameDataModel.GetItem(entry.itemId);
                if (!GameDataModel.CanAppearInRunMerchantLoot(item))
                {
                    merchantGoods.RemoveAt(i);
                }
            }

            for (int i = 0; i < merchantGoods.Count; i++)
            {
                merchantGoods[i].Price = ElementalCoreCombatSystem.Instance != null ? ElementalCoreCombatSystem.Instance.ResolveShopPrice(merchantGoods[i].Price) : merchantGoods[i].Price;
            }
        }

        private void UpdateGold()
        {
            int coins = NewInventorySystem.Instance != null ? NewInventorySystem.Instance.PlayerStats.coins : 0;
            if (goldText != null)
            {
                goldText.text = "金币：" + coins;
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

            GameObject obj = new GameObject("RogueShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 550;
            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.18f, 0.32f, 0.58f, 1f));
            Text text = CreateText(obj.transform, "Text", label, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
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

        private static void Clear(Transform root)
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

    public enum ShopEntryKind
    {
        Item,
        Core,
        Treasure
    }

    public sealed class ShopEntry
    {
        public ShopEntryKind kind;
        public int itemId;
        public int coreId;
        public int coreInstanceId;
        public int slotIndex = -1;
        public int count = 1;
        public bool equippedCore;
        public string Label;
        public string Description;
        public int Price;

        public static ShopEntry MerchantItem(int itemId, int count, int price)
        {
            ItemData item = GameDataModel.GetItem(itemId);
            return new ShopEntry
            {
                kind = ShopEntryKind.Item,
                itemId = itemId,
                count = Mathf.Max(1, count),
                Label = item != null ? item.itemName : "未知物品",
                Description = item != null ? item.itemName + "\n" + item.description : "未知物品",
                Price = price
            };
        }

        public static ShopEntry MerchantCore(int coreId, int price)
        {
            CoreData core = GameDataModel.GetCore(coreId);
            return new ShopEntry
            {
                kind = ShopEntryKind.Core,
                coreId = coreId,
                Label = core != null ? core.coreName : "未知虫核",
                Description = core != null ? core.coreName + "\n" + core.description + "\n品质：蓝色" : "未知虫核",
                Price = price
            };
        }

        public static ShopEntry MerchantTreasure(int price)
        {
            return new ShopEntry
            {
                kind = ShopEntryKind.Treasure,
                Label = "随机宝物",
                Description = "购买后立刻获得一件随机宝物。宝物无需安装，获得即生效。",
                Price = price
            };
        }

        public static ShopEntry PlayerItem(int slotIndex, ItemData item, int count)
        {
            int price = Mathf.Max(4, ResolveRarityBase(item.rarity) * Mathf.Max(1, count) / 2);
            return new ShopEntry
            {
                kind = ShopEntryKind.Item,
                itemId = item.id,
                slotIndex = slotIndex,
                count = count,
                Label = item.itemName + " x" + count,
                Description = item.itemName + "\n" + item.description,
                Price = price
            };
        }

        public static ShopEntry PlayerCore(CoreInstance core, bool equipped)
        {
            int price = core.quality == CoreQuality.Legendary ? 90 : core.quality == CoreQuality.Rare ? 42 : 18;
            return new ShopEntry
            {
                kind = ShopEntryKind.Core,
                coreInstanceId = core.instanceId,
                equippedCore = equipped,
                Label = core.DisplayName + (equipped ? "\n已镶嵌" : ""),
                Description = core.GetDescription(),
                Price = price
            };
        }

        private static int ResolveRarityBase(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Legendary:
                    return 120;
                case Rarity.Epic:
                    return 70;
                case Rarity.Rare:
                    return 34;
                default:
                    return 12;
            }
        }
    }

    public sealed class ShopCellHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RogueShopPanel panel;
        private ShopEntry entry;
        private bool merchantSide;

        public void Initialize(RogueShopPanel owner, ShopEntry shopEntry, bool isMerchantSide)
        {
            panel = owner;
            entry = shopEntry;
            merchantSide = isMerchantSide;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            panel?.ShowDetail(entry, merchantSide);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            panel?.ClearDetail();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                panel?.TryTrade(entry, merchantSide);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (panel != null && panel.IsOverOppositeZone(eventData.position, merchantSide))
            {
                panel.TryTrade(entry, merchantSide);
            }
        }
    }
}
