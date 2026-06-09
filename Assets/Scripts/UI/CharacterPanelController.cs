using UnityEngine;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class CharacterPanelController : MonoBehaviour
    {
        private const string PanelObjectName = "CharacterPanelRuntime";

        public static CharacterPanelController Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private Vector2 panelSize = new Vector2(420f, 240f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color textColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private GameObject panelRoot;
        private Text statsText;
        private Text detailText;
        private Canvas targetCanvas;
        private PlayerStats playerStats;
        private global::Player legacyPlayer;
        private bool isVisible;

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

        private void Start()
        {
            CacheReferences();
            RefreshText();
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
                CacheReferences();
                RefreshText();
            }
        }

        public void TogglePanel()
        {
            if (panelRoot == null)
            {
                EnsurePanel();
            }

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
            CacheReferences();
            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
                panelRoot.SetActive(true);
            }

            isVisible = true;
            RefreshText();
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

            targetCanvas = Object.FindObjectOfType<Canvas>(true);
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

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;

            Image background = panelRoot.GetComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = true;

            GameObject titleObject = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(panelRoot.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(18f, 8f);
            titleRect.offsetMax = new Vector2(-18f, -8f);

            Text titleText = titleObject.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = textColor;
            titleText.text = "角色属性";

            GameObject statsObject = new GameObject("StatsText", typeof(RectTransform), typeof(Text));
            statsObject.transform.SetParent(panelRoot.transform, false);
            RectTransform statsRect = statsObject.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0.42f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.offsetMin = new Vector2(18f, 16f);
            statsRect.offsetMax = new Vector2(-18f, -38f);

            statsText = statsObject.GetComponent<Text>();
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 18;
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.color = textColor;
            statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
            statsText.text = string.Empty;

            GameObject detailObject = new GameObject("DetailText", typeof(RectTransform), typeof(Text));
            detailObject.transform.SetParent(panelRoot.transform, false);
            RectTransform detailRect = detailObject.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0.42f);
            detailRect.offsetMin = new Vector2(18f, 14f);
            detailRect.offsetMax = new Vector2(-18f, -14f);

            detailText = detailObject.GetComponent<Text>();
            detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailText.fontSize = 16;
            detailText.alignment = TextAnchor.UpperLeft;
            detailText.color = textColor;
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailText.verticalOverflow = VerticalWrapMode.Overflow;
            detailText.text = string.Empty;
        }

        private void CacheReferences()
        {
            if (playerStats == null)
            {
                playerStats = Object.FindObjectOfType<PlayerStats>(true);
            }

            if (legacyPlayer == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    legacyPlayer = playerObject.GetComponent<global::Player>();
                }
            }
        }

        private void RefreshText()
        {
            if (statsText == null)
            {
                return;
            }

            int currentHp = playerStats != null ? playerStats.CurrentHP : 0;
            int maxHp = playerStats != null ? playerStats.MaxHP : 0;
            int attack = playerStats != null ? playerStats.ATK : 0;
            int defense = playerStats != null ? playerStats.DEF : 0;
            int coins = playerStats != null ? playerStats.CoinAmount : (legacyPlayer != null ? legacyPlayer.CoinAmount : 0);

            statsText.text = string.Format(
                "当前血量：{0}/{1}\n攻击力：{2}\n防御力：{3}\n金币：{4}",
                currentHp,
                maxHp,
                attack,
                defense,
                coins);

            if (detailText != null)
            {
                Weapon currentWeapon = ResolveEquippedWeapon();
                if (currentWeapon != null)
                {
                    string attackMode = currentWeapon.IsRangedStyle() ? "远程" : "近战";
                    detailText.text = string.Format(
                        "当前武器：{0}\n武器类型：{1}\n武器攻击：+{2}\n攻击模式：{3}\n\n左键攻击\nDagger 走近战扇形\nWand 走法术飞弹",
                        currentWeapon.Name,
                        GetWeaponTypeText(currentWeapon.WpType),
                        currentWeapon.Damage,
                        attackMode);
                }
                else
                {
                    detailText.text = "当前武器：未装备\n攻击模式：默认近战\n\n把 Dagger 放到主手，Wand 放到副手即可切换模式。";
                }
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

        private Weapon ResolveEquippedWeapon()
        {
            CharacterPanel panel = CharacterPanel.Instance;
            if (panel == null)
            {
                return null;
            }

            EquipmentSlot[] equipmentSlots = panel.GetComponentsInChildren<EquipmentSlot>(true);
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                EquipmentSlot slot = equipmentSlots[i];
                if (slot == null || slot.transform.childCount <= 0)
                {
                    continue;
                }

                ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
                if (itemUI != null && itemUI.Item is Weapon weapon)
                {
                    return weapon;
                }
            }

            return null;
        }

        private static string GetWeaponTypeText(Weapon.WeaponType weaponType)
        {
            switch (weaponType)
            {
                case Weapon.WeaponType.Dagger:
                    return "短剑";
                case Weapon.WeaponType.Wand:
                    return "法杖";
                case Weapon.WeaponType.OffHand:
                    return "副手";
                case Weapon.WeaponType.MainHand:
                    return "主手";
                default:
                    return "无";
            }
        }
    }
}
