using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModernRogue
{
    public sealed class WeaponForgePanel : MonoBehaviour
    {
        private GameObject root;
        private Text currencyText;
        private Text classText;
        private Text weaponText;
        private Text armorText;
        private Text footerText;

        public static void Open()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                return;
            }

            GameObject old = GameObject.Find("WeaponForgePanel");
            if (old != null)
            {
                Destroy(old);
            }

            GameObject panel = new GameObject("WeaponForgePanel", typeof(RectTransform), typeof(Image), typeof(WeaponForgePanel), typeof(ModalPauseToken));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 580f);
            rect.anchoredPosition = Vector2.zero;
            RuntimeUIVisuals.StyleImage(panel.GetComponent<Image>(), new Color(0.06f, 0.07f, 0.1f, 0.98f));
            RuntimeUIVisuals.AddFrame(panel.transform, "ForgeFrame", new Color(0.72f, 0.52f, 0.18f, 0.92f), 0.012f);
            panel.GetComponent<WeaponForgePanel>().root = panel;
            ModalPanelNavigation navigation = panel.AddComponent<ModalPanelNavigation>();
            navigation.Initialize(panel);
            panel.GetComponent<WeaponForgePanel>().Build(navigation);
        }

        private void Build(ModalPanelNavigation navigation)
        {
            RuntimeUIVisuals.CreateBlock(transform, "HeaderBar", new Vector2(0.02f, 0.9f), new Vector2(0.98f, 0.985f), new Color(0.1f, 0.07f, 0.04f, 0.98f));

            Text title = CreateText(transform, "Title", "武器铺 · 局外成长", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.905f), new Vector2(0.48f, 0.98f));
            title.color = new Color(1f, 0.9f, 0.62f, 1f);

            currencyText = CreateText(transform, "Currency", "", 17, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(currencyText.rectTransform, new Vector2(0.48f, 0.905f), new Vector2(0.82f, 0.98f));

            Button close = CreateButton(transform, "Close", "返回上一级");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.84f, 0.915f), new Vector2(0.96f, 0.975f));
            close.onClick.AddListener(navigation.Close);

            classText = CreateText(transform, "Class", "", 16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(classText.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.94f, 0.885f));

            CreateUpgradeCard(
                "WeaponCard",
                new Vector2(0.04f, 0.18f),
                new Vector2(0.48f, 0.8f),
                new Color(0.12f, 0.16f, 0.24f, 0.94f),
                out weaponText,
                "升级武器",
                () =>
                {
                    RunProgressionSystem.Instance?.UpgradeCurrentClassWeapon();
                    RefreshInfo();
                });

            CreateUpgradeCard(
                "ArmorCard",
                new Vector2(0.52f, 0.18f),
                new Vector2(0.96f, 0.8f),
                new Color(0.14f, 0.18f, 0.16f, 0.94f),
                out armorText,
                "升级防具",
                () =>
                {
                    RunProgressionSystem.Instance?.UpgradeCurrentClassArmor();
                    RefreshInfo();
                });

            footerText = CreateText(transform, "Footer", "", 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(footerText.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.14f));
            footerText.color = new Color(0.78f, 0.82f, 0.9f, 0.92f);

            RefreshInfo();
        }

        private void CreateUpgradeCard(
            string name,
            Vector2 min,
            Vector2 max,
            Color backColor,
            out Text bodyText,
            string buttonLabel,
            UnityEngine.Events.UnityAction action)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            SetRect(card.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleImage(card.GetComponent<Image>(), backColor);
            RuntimeUIVisuals.AddFrame(card.transform, name + "Frame", new Color(0.55f, 0.62f, 0.78f, 0.55f), 0.018f);

            bodyText = CreateText(card.transform, "Body", "", 16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(bodyText.rectTransform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.94f));

            Button button = CreateButton(card.transform, "Upgrade", buttonLabel);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.15f));
            button.onClick.AddListener(action);
        }

        private void RefreshInfo()
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            if (run == null)
            {
                return;
            }

            HeroClass heroClass = run.CurrentClass;
            if (currencyText != null)
            {
                currencyText.text = "Boss 精华  " + run.MetaBossCurrency;
            }

            if (classText != null)
            {
                classText.text = "当前职业：" + RunProgressionSystem.GetClassName(heroClass) + "    ·    精华仅由 Boss 战带回基地";
            }

            if (weaponText != null)
            {
                StringBuilder weapon = new StringBuilder();
                weapon.AppendLine("【武器 Lv." + run.CurrentWeaponMetaLevel + "】");
                weapon.AppendLine("下次升级消耗 " + run.GetUpgradeCost(run.CurrentWeaponMetaLevel) + " 精华");
                weapon.AppendLine("攻击 +" + run.GetWeaponMetaBonus());
                weapon.AppendLine("每 5 级获得 1 条稀有词条");
                AppendAffixLines(weapon, run.GetWeaponAffixes(heroClass));
                weaponText.text = weapon.ToString();
            }

            if (armorText != null)
            {
                StringBuilder armor = new StringBuilder();
                armor.AppendLine("【防具 Lv." + run.CurrentArmorMetaLevel + "】");
                armor.AppendLine("下次升级消耗 " + run.GetUpgradeCost(run.CurrentArmorMetaLevel) + " 精华");
                armor.AppendLine("防御 +" + run.GetArmorMetaDefBonus() + "    生命 +" + run.GetArmorMetaHpBonus());
                armor.AppendLine("每 5 级强化护甲护盾（Lv.5 解锁）");
                armor.AppendLine();
                armor.AppendLine(run.GetArmorShieldSummary(heroClass));
                if (run.CurrentArmorMetaLevel >= 5)
                {
                    armor.AppendLine("当前护盾段数：" + run.ArmorShieldTier);
                }

                armorText.text = armor.ToString();
            }

            if (footerText != null)
            {
                footerText.text = "武器每 5 级获得稀有词条；防具每 5 级强化护甲护盾（上限、回复延迟、回复速度）。";
            }
        }

        private static void AppendAffixLines(StringBuilder builder, System.Collections.Generic.IReadOnlyList<MetaAffixKind> affixes)
        {
            builder.AppendLine();
            if (affixes == null || affixes.Count == 0)
            {
                builder.AppendLine("稀有词条：无");
                return;
            }

            builder.AppendLine("稀有词条：");
            for (int i = 0; i < affixes.Count; i++)
            {
                builder.AppendLine("· " + MetaAffixUtility.GetName(affixes[i]));
                builder.AppendLine("  " + MetaAffixUtility.GetDescription(affixes[i]));
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

            GameObject obj = new GameObject("WeaponForgeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 545;
            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), new Color(0.34f, 0.24f, 0.12f, 0.98f));
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
    }
}
