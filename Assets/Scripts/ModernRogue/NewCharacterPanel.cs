using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class NewCharacterPanel : MonoBehaviour
    {
        private NewInventorySystem inventory;
        private Transform rowsRoot;
        private Text hintText;

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

            Text title = CreateText(root.transform, "Title", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = "角色养成";
            SetRect(title.rectTransform, new Vector2(0.035f, 0.905f), new Vector2(0.42f, 0.98f), Vector2.zero, Vector2.zero);

            Text closeHint = CreateText(root.transform, "CloseHint", 14, FontStyle.Normal, TextAnchor.MiddleRight);
            closeHint.text = "局内装备固定，成长请回基地武器铺";
            closeHint.color = new Color(0.82f, 0.86f, 0.94f, 0.92f);
            SetRect(closeHint.rectTransform, new Vector2(0.38f, 0.905f), new Vector2(0.965f, 0.98f), Vector2.zero, Vector2.zero);

            GameObject panel = new GameObject("StatsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            Image panelImage = panel.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(panelImage, new Color(0.018f, 0.023f, 0.032f, 0.82f));
            RuntimeUIVisuals.AddFrame(panel.transform, "StatsFrame", new Color(0.58f, 0.66f, 0.82f, 0.35f), 0.008f);
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.055f, 0.28f), new Vector2(0.945f, 0.86f), Vector2.zero, Vector2.zero);

            rowsRoot = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            rowsRoot.SetParent(panel.transform, false);
            SetRect(rowsRoot.GetComponent<RectTransform>(), new Vector2(0.055f, 0.08f), new Vector2(0.945f, 0.92f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rowsLayout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 8f;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            Button upgradeButton = CreateButton(root.transform, "UpgradeButton", "花费 20 金币：核心属性永久 +1");
            RectTransform buttonRect = upgradeButton.GetComponent<RectTransform>();
            SetRect(buttonRect, new Vector2(0.17f, 0.13f), new Vector2(0.83f, 0.24f), Vector2.zero, Vector2.zero);
            upgradeButton.onClick.AddListener(() =>
            {
                if (NewInventorySystem.Instance != null && !NewInventorySystem.Instance.UpgradeBaseStat())
                {
                    hintText.text = "金币不足。";
                }
            });

            hintText = CreateText(root.transform, "Hint", 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            hintText.text = "数值随虫核、宝物与永久升级实时刷新。";
            SetRect(hintText.rectTransform, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.11f), Vector2.zero, Vector2.zero);
            RefreshUI();
        }

        public void RefreshUI()
        {
            inventory = NewInventorySystem.Instance;
            if (inventory == null || rowsRoot == null)
            {
                return;
            }

            ClearChildren(rowsRoot);
            ItemData weapon = inventory.GetEquippedWeapon();
            ItemData armor = inventory.GetEquippedArmor();
            PlayerStatBlock stats = inventory.PlayerStats;

            AddStatRow("攻击", stats.baseAtk + stats.permanentAtkBonus, stats.coreAtkBonus + (weapon != null ? weapon.bonusAtk : 0));
            AddStatRow("防御", stats.baseDef + stats.permanentDefBonus, stats.coreDefBonus + (armor != null ? armor.bonusDef : 0));
            AddStatRow("最大生命", stats.baseMaxHp + stats.permanentHpBonus, stats.coreHpBonus + (armor != null ? armor.bonusMaxHp : 0));
            AddStatRow("当前生命", stats.currentHp, 0);
            AddPercentRow("移动速度", inventory.GetMoveSpeedMultiplier() - 1f);
            AddPercentRow("暴击率", stats.coreCritRateBonus + stats.runCritRateBonus);
            AddPercentRow("攻击速度", stats.coreAttackSpeedBonus + stats.runAttackSpeedBonus);
            AddStatRow("金币", stats.coins, 0);
            AddStatRow("等级", stats.level, 0);
            AddStatRow("经验", stats.experience, 0);
        }

        private void AddStatRow(string label, int baseValue, int bonus)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(rowsRoot, false);
            row.GetComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childForceExpandHeight = true;
            rowLayout.childForceExpandWidth = true;

            Text name = CreateText(row.transform, "Name", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
            name.text = label;
            Text value = CreateText(row.transform, "Value", 17, FontStyle.Bold, TextAnchor.MiddleRight);
            value.text = bonus > 0 ? baseValue + " + <color=green>(+" + bonus + ")</color>" : baseValue.ToString();
        }

        private void AddPercentRow(string label, float value)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(rowsRoot, false);
            row.GetComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childForceExpandHeight = true;
            rowLayout.childForceExpandWidth = true;

            Text name = CreateText(row.transform, "Name", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
            name.text = label;
            Text statValue = CreateText(row.transform, "Value", 17, FontStyle.Bold, TextAnchor.MiddleRight);
            statValue.text = value > 0.001f ? "<color=green>+" + Mathf.RoundToInt(value * 100f) + "%</color>" : "0%";
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, new Color(0.08f, 0.1f, 0.13f, 0.94f));
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 46f;
            Text text = CreateText(buttonObject.transform, "Label", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.text = label;
            return buttonObject.GetComponent<Button>();
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
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
