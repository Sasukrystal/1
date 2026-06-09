using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class NewBackpackPanel : MonoBehaviour
    {
        private NewInventorySystem inventory;
        private Text statText;
        private Text coinText;
        private Transform gridRoot;
        private Transform equipRoot;

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
            Image background = root.gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = root.gameObject.AddComponent<Image>();
            }

            RuntimeUIVisuals.StyleImage(background, new Color(0.025f, 0.032f, 0.045f, 0.94f));
            RuntimeUIVisuals.CreateBlock(root, "HeaderBar", new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.985f), new Color(0.05f, 0.06f, 0.08f, 0.92f));

            Text title = CreateText(root, "Title", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = "背包与装备";
            SetRect(title.rectTransform, new Vector2(0.035f, 0.905f), new Vector2(0.4f, 0.98f), Vector2.zero, Vector2.zero);

            Text hint = CreateText(root, "Hint", 14, FontStyle.Normal, TextAnchor.MiddleRight);
            hint.text = "1/2/3 使用前三格消耗品    B 关闭";
            SetRect(hint.rectTransform, new Vector2(0.42f, 0.905f), new Vector2(0.965f, 0.98f), Vector2.zero, Vector2.zero);

            GameObject left = CreatePanel(root, "StatusAndEquip", new Vector2(0.035f, 0.07f), new Vector2(0.34f, 0.9f));
            Text leftTitle = CreateText(left.transform, "SectionTitle", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            leftTitle.text = "角色状态";
            SetRect(leftTitle.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            statText = CreateText(left.transform, "Stats", 15, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(statText.rectTransform, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.83f), Vector2.zero, Vector2.zero);

            equipRoot = new GameObject("EquipSlots", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            equipRoot.SetParent(left.transform, false);
            SetRect(equipRoot.GetComponent<RectTransform>(), new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.49f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup equipGroup = equipRoot.GetComponent<VerticalLayoutGroup>();
            equipGroup.spacing = 12f;
            equipGroup.childForceExpandWidth = true;
            equipGroup.childForceExpandHeight = false;

            GameObject right = CreatePanel(root, "ItemGridColumn", new Vector2(0.365f, 0.07f), new Vector2(0.965f, 0.9f));
            Text gridTitle = CreateText(right.transform, "GridTitle", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            gridTitle.text = "物品栏";
            SetRect(gridTitle.rectTransform, new Vector2(0.04f, 0.9f), new Vector2(0.5f, 0.98f), Vector2.zero, Vector2.zero);

            coinText = CreateText(right.transform, "Coins", 18, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(coinText.rectTransform, new Vector2(0.52f, 0.9f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            Text gridHint = CreateText(right.transform, "GridHint", 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            gridHint.color = new Color(0.82f, 0.86f, 0.94f, 0.88f);
            gridHint.text = "左键消耗品使用 / 点击装备位卸下（局内武器护甲固定）";
            SetRect(gridHint.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.055f), Vector2.zero, Vector2.zero);

            GameObject gridPanel = new GameObject("ItemGrid", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
            gridPanel.transform.SetParent(right.transform, false);
            SetRect(gridPanel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            Image gridImage = gridPanel.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(gridImage, new Color(0.018f, 0.023f, 0.032f, 0.78f));
            GridLayoutGroup gridLayout = gridPanel.GetComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(12, 12, 12, 12);
            gridLayout.cellSize = new Vector2(72f, 72f);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridRoot = gridPanel.transform;
            RefreshUI();
        }

        public void RefreshUI()
        {
            inventory = NewInventorySystem.Instance;
            if (inventory == null || statText == null || gridRoot == null || equipRoot == null)
            {
                return;
            }

            ItemData weapon = inventory.GetEquippedWeapon();
            ItemData armor = inventory.GetEquippedArmor();
            PlayerStatBlock stats = inventory.PlayerStats;

            statText.text = string.Format(
                "等级  {0}\n经验  {1}/{2}\n攻击  {3}\n防御  {4}\n生命  {5}/{6}\n\n左键：按武器攻击\n空格：闪避\n1/2/3：使用前三格消耗品",
                stats.level,
                stats.experience,
                stats.level * 60,
                stats.TotalAtk(weapon),
                stats.TotalDef(armor),
                stats.currentHp,
                stats.TotalMaxHp(armor));
            coinText.text = "金币  " + stats.coins;

            RebuildEquipSlots(weapon, armor);
            RebuildGrid();
        }

        private void RebuildEquipSlots(ItemData weapon, ItemData armor)
        {
            ClearChildren(equipRoot);
            CreateEquipSlot("WeaponSlot", "武器", weapon, () => inventory.UnequipWeapon());
            CreateEquipSlot("ArmorSlot", "护甲", armor, () => inventory.UnequipArmor());
        }

        private void RebuildGrid()
        {
            ClearChildren(gridRoot);
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                ItemSlot slot = inventory.Slots[i];
                ItemData item = GameDataModel.GetItem(slot.itemId);
                GameObject cell = new GameObject("Cell_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemCellHandler));
                cell.transform.SetParent(gridRoot, false);
                Image image = cell.GetComponent<Image>();
                RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.08f, 0.095f, 0.92f));
                if (item != null)
                {
                    RuntimeUIVisuals.AddFrame(cell.transform, "RarityFrame", GameDataModel.GetRarityColor(item.rarity), 0.08f);
                    AttachItemIcon(cell.transform, item, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.92f));
                }

                Button button = cell.GetComponent<Button>();
                int captured = i;
                button.onClick.AddListener(() => inventory.EquipFromSlot(captured));

                Text label = CreateText(cell.transform, "Label", 10, FontStyle.Bold, TextAnchor.LowerCenter);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.04f, 0.02f);
                labelRect.anchorMax = new Vector2(0.96f, 0.3f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label.text = item != null ? item.itemName + "\nx" + slot.count : "";

                if (i < 3)
                {
                    Text hotkey = CreateText(cell.transform, "Hotkey", 12, FontStyle.Bold, TextAnchor.UpperLeft);
                    hotkey.color = new Color(1f, 0.86f, 0.42f, 1f);
                    hotkey.rectTransform.anchorMin = new Vector2(0.04f, 0.78f);
                    hotkey.rectTransform.anchorMax = new Vector2(0.34f, 0.98f);
                    hotkey.rectTransform.offsetMin = Vector2.zero;
                    hotkey.rectTransform.offsetMax = Vector2.zero;
                    hotkey.text = (i + 1).ToString();
                }
            }
        }

        private void CreateEquipSlot(string name, string label, ItemData item, UnityEngine.Events.UnityAction unequip)
        {
            GameObject slot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            slot.transform.SetParent(equipRoot, false);
            Image image = slot.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.07f, 0.08f, 0.095f, 0.9f));
            if (item != null)
            {
                RuntimeUIVisuals.AddFrame(slot.transform, "EquipFrame", GameDataModel.GetRarityColor(item.rarity), 0.05f);
                AttachItemIcon(slot.transform, item, new Vector2(0.08f, 0.18f), new Vector2(0.34f, 0.92f));
            }
            LayoutElement layout = slot.AddComponent<LayoutElement>();
            layout.preferredHeight = 92f;
            slot.GetComponent<Button>().onClick.AddListener(unequip);

            Text text = CreateText(slot.transform, "Label", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = new Vector2(0.36f, 0.08f);
            text.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.text = item != null ? label + "\n" + item.itemName : label + "\n空";
        }

        private static void AttachItemIcon(Transform parent, ItemData item, Vector2 anchorMin, Vector2 anchorMax)
        {
            Sprite icon = Art2DUtility.LoadItemSprite(item);
            if (icon == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = anchorMin;
            iconRect.anchorMax = anchorMax;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject column = new GameObject(name, typeof(RectTransform), typeof(Image));
            column.transform.SetParent(parent, false);
            Image image = column.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.018f, 0.023f, 0.032f, 0.82f));
            SetRect(column.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            return column;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = new Color(0.94f, 0.96f, 1f, 1f);
            text.alignment = anchor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
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

        private sealed class ItemCellHandler : MonoBehaviour, IPointerClickHandler
        {
            public void OnPointerClick(PointerEventData eventData)
            {
            }
        }
    }
}
