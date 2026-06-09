using UnityEngine;
using System.Collections;

using UnityEngine.UI;

public class CharacterPanel : Inventory
{
    #region 单例模式
    private static CharacterPanel _instance;
    public static CharacterPanel Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    private Text propertyText;
    private Text detailText;

    private Player player;

    public override void Start()
    {
        base.Start();
        propertyText = transform.Find("PropertyPanel/Text").GetComponent<Text>();
        EnsureDetailText();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        if (propertyText != null)
        {
            propertyText.supportRichText = true;
        }
        UpdatePropertyText();
        Hide();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }


    public void PutOn(Item item)
    {
        Item exitItem = null;
        foreach (Slot slot in slotList)
        {
            EquipmentSlot equipmentSlot = (EquipmentSlot)slot;
            if (equipmentSlot.IsRightItem(item))
            {
                if (equipmentSlot.transform.childCount > 0)
                {
                    ItemUI currentItemUI = equipmentSlot.transform.GetChild(0).GetComponent<ItemUI>();
                    exitItem = currentItemUI.Item;
                    currentItemUI.SetItem(item, 1);
                }
                else
                {
                    equipmentSlot.StoreItem(item);
                }
                break;
            }
        }
        if (exitItem != null)
            Knapsack.Instance.StoreItem(exitItem);

        UpdatePropertyText();
    }

    public void PutOff(Item item)
    {
        Knapsack.Instance.StoreItem(item);
        UpdatePropertyText();
    }

    private void UpdatePropertyText()
    {
        //Debug.Log("UpdatePropertyText");
        int bonusStrength = 0, bonusIntellect = 0, bonusAgility = 0, bonusStamina = 0, bonusDamage = 0;
        Weapon equippedWeapon = null;
        System.Text.StringBuilder equipmentSummary = new System.Text.StringBuilder();
        foreach (EquipmentSlot slot in slotList)
        {
            if (slot.transform.childCount > 0)
            {
                Item item = slot.transform.GetChild(0).GetComponent<ItemUI>().Item;
                if (item is Equipment)
                {
                    Equipment e = (Equipment)item;
                    bonusStrength += e.Strength;
                    bonusIntellect += e.Intellect;
                    bonusAgility += e.Agility;
                    bonusStamina += e.Stamina;
                }
                else if (item is Weapon)
                {
                    Weapon weapon = (Weapon)item;
                    bonusDamage += weapon.Damage;
                    if (equippedWeapon == null)
                    {
                        equippedWeapon = weapon;
                    }
                }
            }

            string slotName = slot != null ? slot.name : "Slot";
            if (slot.transform.childCount > 0)
            {
                ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
                if (itemUI != null && itemUI.Item != null)
                {
                    equipmentSummary.AppendLine(string.Format("{0}：<color={1}>{2}</color> x{3}", slotName, GetQualityColor(itemUI.Item.Quality), itemUI.Item.Name, itemUI.Amount));
                }
            }
            else
            {
                equipmentSummary.AppendLine(string.Format("{0}：空", slotName));
            }
        }
        int totalStrength = player.BasicStrength + bonusStrength;
        int totalIntellect = player.BasicIntellect + bonusIntellect;
        int totalAgility = player.BasicAgility + bonusAgility;
        int totalStamina = player.BasicStamina + bonusStamina;
        int totalDamage = player.BasicDamage + bonusDamage;
        Bagsys.RogueLike.PlayerStats playerStats = player != null ? player.GetComponent<Bagsys.RogueLike.PlayerStats>() : null;
        if (playerStats != null)
        {
            playerStats.ApplyEquipmentBonuses(totalStrength, totalIntellect, totalAgility, totalStamina, bonusDamage);
        }

        string currentAttackMode = equippedWeapon != null && equippedWeapon.IsRangedStyle() ? "远程法球" : "近战挥砍";
        string text = string.Format(
            "当前攻击模式：<color=#FFD84D>{0}</color>\n\n力量：{1} <color={2}>({3}{4})</color>\n智力：{5} <color={2}>({3}{6})</color>\n敏捷：{7} <color={2}>({3}{8})</color>\n体力：{9} <color={2}>({3}{10})</color>\n攻击力：{11} <color={2}>({3}{12})</color>",
            currentAttackMode,
            totalStrength,
            GetBonusColor(bonusStrength),
            bonusStrength >= 0 ? "+" : "-",
            Mathf.Abs(bonusStrength),
            totalIntellect,
            Mathf.Abs(bonusIntellect),
            totalAgility,
            Mathf.Abs(bonusAgility),
            totalStamina,
            Mathf.Abs(bonusStamina),
            totalDamage,
            Mathf.Abs(bonusDamage));
        propertyText.supportRichText = true;
        propertyText.text = text;

        if (detailText != null)
        {
            detailText.supportRichText = true;
            if (equippedWeapon != null)
            {
                string attackMode = equippedWeapon.IsRangedStyle() ? "远程" : "近战";
                detailText.text = string.Format(
                    "当前武器：<color={0}>{1}</color>\n武器类型：{2}\n武器加成：<color=#7CFF7C>+{3}</color>\n攻击模式：<color=#7CFF7C>{4}</color>\n\n基础属性 + 装备加成\n力量：{5} <color=#7CFF7C>+{6}</color>\n智力：{7} <color=#7CFF7C>+{8}</color>\n敏捷：{9} <color=#7CFF7C>+{10}</color>\n体力：{11} <color=#7CFF7C>+{12}</color>\n攻击：{13} <color=#7CFF7C>+{14}</color>\n\n装备详情：\n{15}\nE 打开这个面板\n主手/副手切换武器类型",
                    GetQualityColor(equippedWeapon.Quality),
                    equippedWeapon.Name,
                    GetWeaponTypeText(equippedWeapon.WpType),
                    equippedWeapon.Damage,
                    attackMode,
                    player.BasicStrength,
                    bonusStrength,
                    player.BasicIntellect,
                    bonusIntellect,
                    player.BasicAgility,
                    bonusAgility,
                    player.BasicStamina,
                    bonusStamina,
                    player.BasicDamage,
                    bonusDamage,
                    equipmentSummary.ToString().TrimEnd());
            }
            else
            {
                detailText.text = string.Format(
                    "当前武器：未装备\n攻击模式：<color=#7CFF7C>默认近战</color>\n\n基础属性 + 装备加成\n力量：{0} <color=#7CFF7C>+{1}</color>\n智力：{2} <color=#7CFF7C>+{3}</color>\n敏捷：{4} <color=#7CFF7C>+{5}</color>\n体力：{6} <color=#7CFF7C>+{7}</color>\n攻击：{8} <color=#7CFF7C>+{9}</color>\n\n装备详情：\n{10}\n\n把 Dagger 放到主手，Wand 放到副手即可切换攻击方式。",
                    player.BasicStrength,
                    bonusStrength,
                    player.BasicIntellect,
                    bonusIntellect,
                    player.BasicAgility,
                    bonusAgility,
                    player.BasicStamina,
                    bonusStamina,
                    player.BasicDamage,
                    bonusDamage,
                    equipmentSummary.ToString().TrimEnd());
            }
        }
    }

    private void EnsureDetailText()
    {
        if (detailText != null)
        {
            return;
        }

        Transform panel = transform.Find("PropertyPanel");
        if (panel == null)
        {
            return;
        }

        Transform existing = panel.Find("DetailText");
        if (existing != null)
        {
            detailText = existing.GetComponent<Text>();
            if (detailText != null)
            {
                return;
            }
        }

        GameObject detailObject = new GameObject("DetailText", typeof(RectTransform), typeof(Text));
        detailObject.transform.SetParent(panel, false);

        RectTransform rectTransform = detailObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(12f, 12f);
        rectTransform.offsetMax = new Vector2(-12f, -12f);

        detailText = detailObject.GetComponent<Text>();
        detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailText.fontSize = 16;
        detailText.color = Color.white;
        detailText.alignment = TextAnchor.UpperLeft;
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;
        detailText.supportRichText = true;
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

    private static string GetBonusColor(int bonus)
    {
        if (bonus > 0)
        {
            return "#7CFF7C";
        }

        if (bonus < 0)
        {
            return "#FF6B6B";
        }

        return "#CFCFCF";
    }

}
