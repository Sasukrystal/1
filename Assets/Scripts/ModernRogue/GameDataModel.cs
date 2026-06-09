using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    public enum ItemType
    {
        Material,
        Consumable,
        Weapon,
        Armor
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum WeaponStyle
    {
        Melee,
        Ranged
    }

    public enum RoomRewardType
    {
        Gold,
        Vitality,
        Core,
        Treasure,
        Shop,
        Equipment
    }

    public enum CoreElement
    {
        Wind,
        Fire,
        Thunder,
        Earth,
        Water,
        Metal
    }

    public enum CoreQuality
    {
        Common,
        Rare,
        Legendary
    }

    public enum CoreStatKind
    {
        Attack,
        MaxHp,
        Defense,
        MoveSpeed,
        CritRate,
        AttackSpeed
    }

    [System.Serializable]
    public class CoreStatRoll
    {
        public CoreStatKind kind;
        public float value;

        public string Format()
        {
            switch (kind)
            {
                case CoreStatKind.Attack:
                    return "攻击 +" + Mathf.RoundToInt(value);
                case CoreStatKind.MaxHp:
                    return "生命上限 +" + Mathf.RoundToInt(value);
                case CoreStatKind.Defense:
                    return "防御 +" + Mathf.RoundToInt(value);
                case CoreStatKind.MoveSpeed:
                    return "移动速度 +" + Mathf.RoundToInt(value * 100f) + "%";
                case CoreStatKind.CritRate:
                    return "暴击率 +" + Mathf.RoundToInt(value * 100f) + "%";
                case CoreStatKind.AttackSpeed:
                    return "攻击速度 +" + Mathf.RoundToInt(value * 100f) + "%";
                default:
                    return "";
            }
        }
    }

    [System.Serializable]
    public class CoreInstance
    {
        public int instanceId;
        public int templateId;
        public CoreQuality quality;
        public List<CoreStatRoll> stats = new List<CoreStatRoll>();
        public string specialAffix;

        public string DisplayName
        {
            get
            {
                CoreData core = GameDataModel.GetCore(templateId);
                return (core != null ? core.coreName : "未知虫核") + " [" + GameDataModel.GetCoreQualityName(quality) + "]";
            }
        }

        public string GetDescription()
        {
            CoreData core = GameDataModel.GetCore(templateId);
            string text = DisplayName + "\n" + (core != null ? core.description : "") + "\n";
            for (int i = 0; i < stats.Count; i++)
            {
                text += "\n" + stats[i].Format();
            }

            if (!string.IsNullOrEmpty(specialAffix))
            {
                text += "\n特殊词条：" + specialAffix;
            }

            return text;
        }
    }

    [System.Serializable]
    public class CoreData
    {
        public int id;
        public string coreName;
        public CoreElement element;
        public string description;
        public float moveSpeedBonus;
        public int bonusAtk;
        public int bonusMaxHp;
    }

    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public ItemType itemType;
        public Rarity rarity;
        public int maxStack;
        public string description;
        public int bonusAtk;
        public int bonusDef;
        public int bonusMaxHp;
        public WeaponStyle weaponStyle;
        public bool classRestricted;
        public HeroClass requiredClass;
        public bool revivesOnDeath;
        public string iconKey;
    }

    [System.Serializable]
    public class ItemSlot
    {
        public int itemId;
        public int count;

        public bool IsEmpty => itemId <= 0 || count <= 0;

        public void Clear()
        {
            itemId = 0;
            count = 0;
        }
    }

    [System.Serializable]
    public class PlayerStatBlock
    {
        public int baseAtk = 10;
        public int baseDef = 5;
        public int baseMaxHp = 100;
        public int currentHp = 100;
        public int coins = 120;
        public int experience;
        public int level = 1;

        public int permanentAtkBonus;
        public int permanentDefBonus;
        public int permanentHpBonus;
        public int coreAtkBonus;
        public int coreDefBonus;
        public int coreHpBonus;
        public float coreMoveSpeedBonus;
        public float coreCritRateBonus;
        public float coreAttackSpeedBonus;
        public float runMoveSpeedBonus;
        public float runCritRateBonus;
        public float runAttackSpeedBonus;
        public int metaDefBonus;
        public int metaHpBonus;
        public float metaMaxHpPercentBonus;
        public float metaCritRateBonus;
        public float metaAttackSpeedBonus;

        public int TotalAtk(ItemData weapon)
        {
            return baseAtk + permanentAtkBonus + coreAtkBonus + (weapon != null ? weapon.bonusAtk : 0);
        }

        public int TotalDef(ItemData armor)
        {
            return baseDef + permanentDefBonus + coreDefBonus + metaDefBonus + (armor != null ? armor.bonusDef : 0);
        }

        public int TotalMaxHp(ItemData armor)
        {
            int raw = baseMaxHp + permanentHpBonus + coreHpBonus + metaHpBonus + (armor != null ? armor.bonusMaxHp : 0);
            return Mathf.Max(1, Mathf.RoundToInt(raw * (1f + metaMaxHpPercentBonus)));
        }
    }

    public static class GameDataModel
    {
        private static readonly Dictionary<int, ItemData> itemsById = new Dictionary<int, ItemData>();
        private static readonly Dictionary<int, CoreData> coresById = new Dictionary<int, CoreData>();

        public static IReadOnlyDictionary<int, ItemData> ItemsById => itemsById;
        public static IReadOnlyDictionary<int, CoreData> CoresById => coresById;

        public static void EnsureLoaded()
        {
            if (itemsById.Count > 0)
            {
                return;
            }

            Register(new ItemData
            {
                id = 1,
                itemName = "铁片",
                itemType = ItemType.Material,
                rarity = Rarity.Common,
                maxStack = 99,
                description = "可靠的基础锻造材料。"
            });

            Register(new ItemData
            {
                id = 2,
                itemName = "猩红药剂",
                itemType = ItemType.Consumable,
                rarity = Rarity.Rare,
                maxStack = 8,
                description = "恢复少量生命值。",
                bonusMaxHp = 12,
                iconKey = "Sprites/Items/hp"
            });

            Register(new ItemData
            {
                id = 26,
                itemName = "重生护符",
                itemType = ItemType.Consumable,
                rarity = Rarity.Epic,
                maxStack = 3,
                description = "携带时，生命归零会自动消耗并复活（恢复 45% 生命）。",
                revivesOnDeath = true
            });

            Register(new ItemData
            {
                id = 3,
                itemName = "决斗短匕",
                itemType = ItemType.Weapon,
                rarity = Rarity.Rare,
                maxStack = 1,
                description = "适合近距离清怪的快速短刃。",
                bonusAtk = 8,
                weaponStyle = WeaponStyle.Melee
            });

            Register(new ItemData
            {
                id = 4,
                itemName = "灰烬法杖",
                itemType = ItemType.Weapon,
                rarity = Rarity.Epic,
                maxStack = 1,
                description = "用于远程攻击的法术媒介。",
                bonusAtk = 14,
                weaponStyle = WeaponStyle.Ranged
            });

            Register(new ItemData
            {
                id = 5,
                itemName = "守护板甲",
                itemType = ItemType.Armor,
                rarity = Rarity.Epic,
                maxStack = 1,
                description = "显著提升生存能力的厚重护甲。",
                bonusDef = 8,
                bonusMaxHp = 35
            });

            Register(new ItemData
            {
                id = 6,
                itemName = "日铸长刃",
                itemType = ItemType.Weapon,
                rarity = Rarity.Legendary,
                maxStack = 1,
                description = "拥有 Boss 级强度的传奇武器。",
                bonusAtk = 24,
                weaponStyle = WeaponStyle.Melee
            });

            Register(new ItemData
            {
                id = 7,
                itemName = "圣盾披风",
                itemType = ItemType.Armor,
                rarity = Rarity.Legendary,
                maxStack = 1,
                description = "用于高强度战斗的传奇防具。",
                bonusDef = 16,
                bonusMaxHp = 80
            });

            Register(new ItemData
            {
                id = 8,
                itemName = "破旧的短剑",
                itemType = ItemType.Weapon,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "大厅初始宝箱中的基础近战武器。",
                bonusAtk = 4,
                weaponStyle = WeaponStyle.Melee,
                classRestricted = true,
                requiredClass = HeroClass.Warrior
            });

            Register(new ItemData
            {
                id = 9,
                itemName = "见习法杖",
                itemType = ItemType.Weapon,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "大厅初始宝箱中的基础远程武器。",
                bonusAtk = 5,
                weaponStyle = WeaponStyle.Ranged,
                classRestricted = true,
                requiredClass = HeroClass.Mage
            });

            Register(new ItemData
            {
                id = 10,
                itemName = "猎手短弓",
                itemType = ItemType.Weapon,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "射手职业固定武器。长按左键蓄力，满蓄力箭矢可穿透。",
                bonusAtk = 5,
                weaponStyle = WeaponStyle.Ranged,
                classRestricted = true,
                requiredClass = HeroClass.Archer
            });

            Register(new ItemData
            {
                id = 20,
                itemName = "战士旧甲",
                itemType = ItemType.Armor,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "战士初始防具。提高近战容错。",
                bonusDef = 2,
                bonusMaxHp = 18,
                classRestricted = true,
                requiredClass = HeroClass.Warrior
            });

            Register(new ItemData
            {
                id = 21,
                itemName = "射手轻衣",
                itemType = ItemType.Armor,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "射手初始防具。轻便，适合长距离闪避。",
                bonusDef = 1,
                bonusMaxHp = 10,
                classRestricted = true,
                requiredClass = HeroClass.Archer
            });

            Register(new ItemData
            {
                id = 22,
                itemName = "法师长袍",
                itemType = ItemType.Armor,
                rarity = Rarity.Common,
                maxStack = 1,
                description = "法师初始防具。为法力循环预留接口。",
                bonusDef = 1,
                bonusMaxHp = 8,
                classRestricted = true,
                requiredClass = HeroClass.Mage
            });

            Register(new ItemData
            {
                id = 23,
                itemName = "守卫重甲",
                itemType = ItemType.Armor,
                rarity = Rarity.Epic,
                maxStack = 1,
                description = "战士高级防具。提供更高生命和防御。",
                bonusDef = 10,
                bonusMaxHp = 55,
                classRestricted = true,
                requiredClass = HeroClass.Warrior
            });

            Register(new ItemData
            {
                id = 24,
                itemName = "游隼斗篷",
                itemType = ItemType.Armor,
                rarity = Rarity.Epic,
                maxStack = 1,
                description = "射手高级防具。提高机动性与闪避手感。",
                bonusDef = 5,
                bonusMaxHp = 28,
                classRestricted = true,
                requiredClass = HeroClass.Archer
            });

            Register(new ItemData
            {
                id = 25,
                itemName = "星辉法袍",
                itemType = ItemType.Armor,
                rarity = Rarity.Epic,
                maxStack = 1,
                description = "法师高级防具。强化法力回复流派。",
                bonusDef = 4,
                bonusMaxHp = 24,
                classRestricted = true,
                requiredClass = HeroClass.Mage
            });

            RegisterCore(new CoreData
            {
                id = 101,
                coreName = "风核",
                element = CoreElement.Wind,
                description = "2 件：移动速度与速度转攻击；3 件：闪避后下次攻击必定暴击；4 件：额外闪避充能。",
                moveSpeedBonus = 0.06f
            });

            RegisterCore(new CoreData
            {
                id = 102,
                coreName = "火核",
                element = CoreElement.Fire,
                description = "2 件：攻击概率生成环绕火球；3 件：火球耗尽后追踪敌人；4 件：火球附加灼烧。",
                bonusAtk = 1
            });

            RegisterCore(new CoreData
            {
                id = 103,
                coreName = "水核",
                element = CoreElement.Water,
                description = "2 件：移动留下水渍并浸润敌人；3 件：水渍上缓慢回血；4 件：对浸润敌人伤害提高。",
                bonusMaxHp = 4
            });

            RegisterCore(new CoreData
            {
                id = 104,
                coreName = "雷核",
                element = CoreElement.Thunder,
                description = "2 件：攻击概率施加感电并触发闪电链；3 件：链电概率和攻速提高；4 件：储存链电伤害后释放。",
                bonusAtk = 1
            });

            RegisterCore(new CoreData
            {
                id = 105,
                coreName = "金核",
                element = CoreElement.Metal,
                description = "2 件：清房和击杀额外金币；3 件：商店折扣并处决残血；4 件：攻击概率触发落金。",
                bonusAtk = 1,
                bonusMaxHp = 2
            });
        }

        public static ItemData GetItem(int id)
        {
            EnsureLoaded();
            return itemsById.TryGetValue(id, out ItemData item) ? item : null;
        }

        public static string ResolveItemIconKey(ItemData item)
        {
            if (item == null)
            {
                return "Sprites/Items/bag";
            }

            if (!string.IsNullOrEmpty(item.iconKey))
            {
                return item.iconKey;
            }

            switch (item.itemType)
            {
                case ItemType.Material:
                    return "Sprites/Items/ingots";
                case ItemType.Consumable:
                    return item.revivesOnDeath ? "Sprites/Items/scroll" : "Sprites/Items/hp";
                case ItemType.Weapon:
                    if (item.requiredClass == HeroClass.Mage)
                    {
                        return "Sprites/Items/the_great_stick";
                    }

                    if (item.requiredClass == HeroClass.Archer)
                    {
                        return "Sprites/Items/axe";
                    }

                    return item.rarity >= Rarity.Legendary ? "Sprites/Items/steel_sword" : "Sprites/Items/sword";
                case ItemType.Armor:
                    if (item.requiredClass == HeroClass.Mage)
                    {
                        return item.rarity >= Rarity.Epic ? "Sprites/Items/necklace" : "Sprites/Items/book";
                    }

                    if (item.requiredClass == HeroClass.Archer)
                    {
                        return "Sprites/Items/cloaks";
                    }

                    if (item.requiredClass == HeroClass.Warrior && item.rarity >= Rarity.Epic)
                    {
                        return "Sprites/Items/shield";
                    }

                    return item.rarity >= Rarity.Legendary ? "Sprites/Items/cloaks" : "Sprites/Items/armor";
                default:
                    return "Sprites/Items/bag";
            }
        }

        public static Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare:
                    return new Color(0.25f, 0.52f, 1f, 1f);
                case Rarity.Epic:
                    return new Color(0.72f, 0.32f, 1f, 1f);
                case Rarity.Legendary:
                    return new Color(1f, 0.58f, 0.16f, 1f);
                default:
                    return Color.white;
            }
        }

        public static bool CanAppearInRunMerchantLoot(ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            return item.itemType == ItemType.Consumable || item.itemType == ItemType.Material;
        }

        public static bool IsReviveConsumable(ItemData item)
        {
            return item != null && item.itemType == ItemType.Consumable && item.revivesOnDeath;
        }

        public static bool IsAdvancedEquipment(ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.itemType != ItemType.Weapon && item.itemType != ItemType.Armor)
            {
                return false;
            }

            return item.rarity >= Rarity.Epic;
        }

        public static string GetCoreQualityName(CoreQuality quality)
        {
            switch (quality)
            {
                case CoreQuality.Rare:
                    return "蓝";
                case CoreQuality.Legendary:
                    return "金";
                default:
                    return "白";
            }
        }

        public static Color GetCoreQualityColor(CoreQuality quality)
        {
            switch (quality)
            {
                case CoreQuality.Rare:
                    return new Color(0.25f, 0.55f, 1f, 1f);
                case CoreQuality.Legendary:
                    return new Color(1f, 0.7f, 0.16f, 1f);
                default:
                    return new Color(0.82f, 0.86f, 0.9f, 1f);
            }
        }

        public static CoreInstance CreateCoreInstance(int instanceId, int templateId, CoreQuality quality)
        {
            CoreInstance instance = new CoreInstance
            {
                instanceId = instanceId,
                templateId = templateId,
                quality = quality
            };

            int statCount = quality == CoreQuality.Legendary ? 4 : quality == CoreQuality.Rare ? 3 : 2;
            AddCoreStat(instance, CoreStatKind.Attack);
            AddCoreStat(instance, CoreStatKind.MaxHp);
            if (statCount >= 3)
            {
                AddCoreStat(instance, Random.value > 0.5f ? CoreStatKind.CritRate : CoreStatKind.MoveSpeed);
            }

            if (statCount >= 4)
            {
                AddCoreStat(instance, Random.value > 0.5f ? CoreStatKind.AttackSpeed : CoreStatKind.Defense);
                instance.specialAffix = ResolveSpecialAffix(templateId);
            }

            return instance;
        }

        private static void AddCoreStat(CoreInstance instance, CoreStatKind kind)
        {
            float value;
            switch (kind)
            {
                case CoreStatKind.Attack:
                    value = Random.Range(1, instance.quality == CoreQuality.Legendary ? 6 : 4);
                    break;
                case CoreStatKind.MaxHp:
                    value = Random.Range(4, instance.quality == CoreQuality.Legendary ? 18 : 11);
                    break;
                case CoreStatKind.Defense:
                    value = Random.Range(1, 4);
                    break;
                case CoreStatKind.MoveSpeed:
                    value = Random.Range(0.03f, 0.08f);
                    break;
                case CoreStatKind.CritRate:
                    value = Random.Range(0.03f, 0.1f);
                    break;
                default:
                    value = Random.Range(0.04f, 0.12f);
                    break;
            }

            instance.stats.Add(new CoreStatRoll { kind = kind, value = value });
        }

        private static string ResolveSpecialAffix(int templateId)
        {
            CoreData core = GetCore(templateId);
            if (core == null)
            {
                return "未知共鸣";
            }

            switch (core.element)
            {
                case CoreElement.Wind:
                    return "疾风回响：闪避后的下一次攻击附带额外击退";
                case CoreElement.Fire:
                    return "余烬爆裂：击杀敌人时小范围溅射";
                case CoreElement.Water:
                    return "潮汐护佑：进入新房间时恢复少量生命";
                case CoreElement.Thunder:
                    return "雷鸣蓄能：链电会为下一次爆发储能";
                case CoreElement.Metal:
                    return "落金裁决：金币越多，落金伤害越高";
                default:
                    return "元素共鸣：提升对应元素套装计数";
            }
        }

        private static void Register(ItemData item)
        {
            itemsById[item.id] = item;
        }

        public static CoreData GetCore(int id)
        {
            EnsureLoaded();
            return coresById.TryGetValue(id, out CoreData core) ? core : null;
        }

        private static void RegisterCore(CoreData core)
        {
            coresById[core.id] = core;
        }
    }
}
