using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public enum HeroClass
    {
        Warrior,
        Archer,
        Mage
    }

    public enum TreasureKind
    {
        ElementShard,
        ShopRefresh,
        UpgradeReroll,
        PerfectRoomGrowth,
        ManaSpring,
        ArmorCharm,
        Revive
    }

    public sealed class TreasureData
    {
        public int id;
        public string treasureName;
        public TreasureKind kind;
        public CoreElement element;
        public string description;
        public string iconKey;
    }

    [DefaultExecutionOrder(-620)]
    [DisallowMultipleComponent]
    public class RunProgressionSystem : MonoBehaviour
    {
        public static RunProgressionSystem Instance { get; private set; }

        private readonly List<TreasureData> treasures = new List<TreasureData>();
        private readonly List<TreasureData> allTreasures = new List<TreasureData>();
        private HeroClass currentClass = HeroClass.Warrior;
        private bool runActive;
        private int metaBossCurrency;
        private readonly int[] metaWeaponLevels = new int[3];
        private readonly int[] metaArmorLevels = new int[3];
        private readonly List<MetaAffixKind>[] metaWeaponAffixes =
        {
            new List<MetaAffixKind>(),
            new List<MetaAffixKind>(),
            new List<MetaAffixKind>()
        };
        private readonly List<MetaAffixKind>[] metaArmorAffixes =
        {
            new List<MetaAffixKind>(),
            new List<MetaAffixKind>(),
            new List<MetaAffixKind>()
        };
        private float mana;
        private float shieldUntil;
        private float armorShieldCurrent;
        private float armorShieldRegenReadyTime;
        private Canvas canvas;
        private GameObject levelPanel;

        public event System.Action Changed;
        public HeroClass CurrentClass => currentClass;
        public bool IsRunActive => runActive;
        public IReadOnlyList<TreasureData> Treasures => treasures;
        public int MetaBossCurrency => metaBossCurrency;
        public int CurrentWeaponMetaLevel => metaWeaponLevels[(int)currentClass];
        public int CurrentArmorMetaLevel => metaArmorLevels[(int)currentClass];
        public float Mana => mana;
        public float MaxMana => currentClass == HeroClass.Mage ? 100f + GetTreasureCount(TreasureKind.ManaSpring) * 15f : 0f;
        public bool IsShielding => currentClass == HeroClass.Warrior && Time.time < shieldUntil;
        public int CoreSlotCapacity => 6 + GetElementShardSetBonusSlots();
        public int ShopRefreshes => GetTreasureCount(TreasureKind.ShopRefresh);
        public int UpgradeRerolls => GetTreasureCount(TreasureKind.UpgradeReroll);
        public float ArmorShieldCurrent => armorShieldCurrent;
        public int ArmorShieldMax => GetArmorShieldMax(currentClass);
        public int ArmorShieldTier => GetArmorShieldTier(currentClass);
        public float ArmorShieldRegenDelay => GetArmorShieldRegenDelay(currentClass);
        public float ArmorShieldRegenPerSecond => GetArmorShieldRegenPerSecond(currentClass);
        public bool HasArmorShield => ArmorShieldMax > 0;
        public bool IsArmorShieldRegenerating => HasArmorShield && armorShieldCurrent < ArmorShieldMax && Time.time >= armorShieldRegenReadyTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            metaBossCurrency = PlayerPrefs.GetInt("ModernRogue_MetaBossCurrency", 0);
            for (int i = 0; i < metaWeaponLevels.Length; i++)
            {
                metaWeaponLevels[i] = PlayerPrefs.GetInt("ModernRogue_MetaWeaponLevel_" + i, 0);
                metaArmorLevels[i] = PlayerPrefs.GetInt("ModernRogue_MetaArmorLevel_" + i, 0);
                LoadAffixList(metaWeaponAffixes[i], "ModernRogue_MetaWeaponAffix_" + i);
                LoadAffixList(metaArmorAffixes[i], "ModernRogue_MetaArmorAffix_" + i);
            }

            BuildTreasurePool();
        }

        private void Update()
        {
            if (!runActive)
            {
                if (Input.GetKeyDown(KeyCode.M))
                {
                    WeaponForgePanel.Open();
                }
            }

            if (runActive && currentClass == HeroClass.Mage)
            {
                mana = Mathf.Min(MaxMana, mana + (18f + GetTreasureCount(TreasureKind.ManaSpring) * 3f) * Time.deltaTime);
                Changed?.Invoke();
            }

            if (runActive)
            {
                TickArmorShieldRegen();
            }
        }

        public void SelectClass(HeroClass heroClass)
        {
            if (heroClass == HeroClass.Mage)
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.ShowToast("当前仅开放战士与弓箭手");
                }

                return;
            }

            currentClass = heroClass;
            Changed?.Invoke();
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowToast("已选择职业：" + GetClassName(heroClass));
            }
        }

        public void BeginRun()
        {
            runActive = true;
            treasures.Clear();
            mana = MaxMana;
            shieldUntil = 0f;
            armorShieldCurrent = GetArmorShieldMax(currentClass);
            armorShieldRegenReadyTime = 0f;
            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.ResetRunGrowth();
                NewInventorySystem.Instance.SetFixedLoadout(GetClassWeaponId(currentClass), GetStarterArmorId(currentClass));
            }

            Changed?.Invoke();
        }

        public void EndRun()
        {
            runActive = false;
            GameSaveService.ClearRunSave();
            GameAudioService.Ensure()?.SetBossBattleActive(false);
            Changed?.Invoke();
        }

        /// <summary>宝物优先于消耗品；成功时移除对应复活宝物。</summary>
        public bool TryConsumeReviveTreasure(out string treasureName)
        {
            treasureName = null;
            if (!runActive)
            {
                return false;
            }

            for (int i = 0; i < treasures.Count; i++)
            {
                TreasureData treasure = treasures[i];
                if (treasure == null || treasure.kind != TreasureKind.Revive)
                {
                    continue;
                }

                treasureName = treasure.treasureName;
                treasures.RemoveAt(i);
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public bool TrySpendMana(float amount)
        {
            if (currentClass != HeroClass.Mage)
            {
                return true;
            }

            if (mana < amount)
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.ShowPickupTip("法力不足");
                }

                return false;
            }

            mana -= amount;
            Changed?.Invoke();
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (currentClass != HeroClass.Mage)
            {
                return;
            }

            mana = Mathf.Min(MaxMana, mana + Mathf.Max(0f, amount));
            Changed?.Invoke();
        }

        public void BeginWarriorShield()
        {
            if (currentClass != HeroClass.Warrior)
            {
                return;
            }

            shieldUntil = Time.time + 1f;
            Changed?.Invoke();
        }

        public void RestoreArmorShieldAfterRevive()
        {
            if (!HasArmorShield)
            {
                return;
            }

            armorShieldCurrent = Mathf.Max(armorShieldCurrent, ArmorShieldMax * 0.5f);
            armorShieldRegenReadyTime = 0f;
            Changed?.Invoke();
        }

        public int ModifyIncomingDamage(int amount)
        {
            amount = ModifyIncomingDamageWithMeta(amount);
            amount = AbsorbDamageWithArmorShield(amount);
            if (IsShielding)
            {
                return Mathf.Max(0, Mathf.RoundToInt(amount * 0.25f));
            }

            return amount;
        }

        public void AddRunExperience(int amount)
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            PlayerStatBlock stats = inventory.PlayerStats;
            stats.experience += Mathf.Max(0, amount);
            while (stats.experience >= GetRequiredExp(stats.level))
            {
                stats.experience -= GetRequiredExp(stats.level);
                stats.level++;
                ShowLevelChoices(stats.level);
            }

            inventory.RefreshDerivedStats();
            Changed?.Invoke();
        }

        public void GrantBossDefeatRewards()
        {
            metaBossCurrency++;
            PlayerPrefs.SetInt("ModernRogue_MetaBossCurrency", metaBossCurrency);
            PlayerPrefs.Save();
            Changed?.Invoke();
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowPickupTip("Boss 精华 +1（可带回基地武器铺）");
            }
        }

        public int GetWeaponMetaBonus()
        {
            return metaWeaponLevels[(int)currentClass];
        }

        public int GetArmorMetaDefBonus()
        {
            return metaArmorLevels[(int)currentClass];
        }

        public int GetArmorMetaHpBonus()
        {
            return metaArmorLevels[(int)currentClass] * 3;
        }

        public float GetMetaCritRateBonus()
        {
            return SumAffix(metaWeaponAffixes[(int)currentClass], MetaAffixKind.CritRate, 0.05f);
        }

        public float GetMetaLifeStealRate()
        {
            return SumAffix(metaWeaponAffixes[(int)currentClass], MetaAffixKind.LifeSteal, 0.03f);
        }

        public float GetMetaAttackSpeedBonus()
        {
            return SumAffix(metaWeaponAffixes[(int)currentClass], MetaAffixKind.AttackSpeed, 0.08f);
        }

        public float GetMetaDamageReductionRate()
        {
            return 0f;
        }

        public float GetMetaMaxHpPercentBonus()
        {
            return 0f;
        }

        public static int GetArmorShieldTier(HeroClass heroClass)
        {
            if (RunProgressionSystem.Instance == null)
            {
                return 0;
            }

            return RunProgressionSystem.Instance.metaArmorLevels[(int)heroClass] / 5;
        }

        public static int GetArmorShieldMax(HeroClass heroClass)
        {
            int tier = GetArmorShieldTier(heroClass);
            if (tier <= 0)
            {
                return 0;
            }

            return 20 + tier * 14;
        }

        public static float GetArmorShieldRegenDelay(HeroClass heroClass)
        {
            int tier = GetArmorShieldTier(heroClass);
            if (tier <= 0)
            {
                return 999f;
            }

            return Mathf.Max(2.4f, 6.2f - (tier - 1) * 0.8f);
        }

        public static float GetArmorShieldRegenPerSecond(HeroClass heroClass)
        {
            int tier = GetArmorShieldTier(heroClass);
            if (tier <= 0)
            {
                return 0f;
            }

            return 7f + tier * 3.5f;
        }

        public string GetArmorShieldSummary(HeroClass heroClass)
        {
            int tier = GetArmorShieldTier(heroClass);
            if (tier <= 0)
            {
                int levelsToUnlock = 5 - (metaArmorLevels[(int)heroClass] % 5);
                if (metaArmorLevels[(int)heroClass] == 0)
                {
                    levelsToUnlock = 5;
                }

                return "护甲护盾：未解锁（防具 Lv.5 解锁）";
            }

            return "护甲护盾：上限 " + GetArmorShieldMax(heroClass)
                + "    击破后 " + GetArmorShieldRegenDelay(heroClass).ToString("0.0") + " 秒开始回复"
                + "    回复 " + GetArmorShieldRegenPerSecond(heroClass).ToString("0.0") + " / 秒";
        }

        public float GetArmorShieldRegenRemaining()
        {
            if (!HasArmorShield || armorShieldCurrent >= ArmorShieldMax)
            {
                return 0f;
            }

            return Mathf.Max(0f, armorShieldRegenReadyTime - Time.time);
        }

        public int GetUpgradeCost(int currentLevel)
        {
            return currentLevel + 1;
        }

        public bool UpgradeCurrentClassWeapon()
        {
            return TryUpgradeWeapon(currentClass);
        }

        public bool UpgradeCurrentClassArmor()
        {
            return TryUpgradeArmor(currentClass);
        }

        public bool TryUpgradeWeapon(HeroClass heroClass)
        {
            int index = (int)heroClass;
            int cost = GetUpgradeCost(metaWeaponLevels[index]);
            if (metaBossCurrency < cost)
            {
                SoulKnightDirector.Instance?.ShowToast("Boss 精华不足\n" + GetClassName(heroClass) + "武器升级需要 " + cost + " 精华");
                return false;
            }

            metaBossCurrency -= cost;
            metaWeaponLevels[index]++;
            if (metaWeaponLevels[index] % 5 == 0)
            {
                metaWeaponAffixes[index].Add(RollWeaponAffix());
            }

            SaveMetaProgress();
            NewInventorySystem.Instance?.RefreshDerivedStats();
            Changed?.Invoke();
            SoulKnightDirector.Instance?.ShowToast(GetClassName(heroClass) + " 武器 Lv." + metaWeaponLevels[index]
                + "\n攻击 +" + GetWeaponMetaBonus() + (metaWeaponLevels[index] % 5 == 0 ? "\n获得稀有词条" : ""));
            return true;
        }

        public bool TryUpgradeArmor(HeroClass heroClass)
        {
            int index = (int)heroClass;
            int cost = GetUpgradeCost(metaArmorLevels[index]);
            if (metaBossCurrency < cost)
            {
                SoulKnightDirector.Instance?.ShowToast("Boss 精华不足\n" + GetClassName(heroClass) + "防具升级需要 " + cost + " 精华");
                return false;
            }

            metaBossCurrency -= cost;
            metaArmorLevels[index]++;
            SaveMetaProgress();
            NewInventorySystem.Instance?.RefreshDerivedStats();
            Changed?.Invoke();

            string toast = GetClassName(heroClass) + " 防具 Lv." + metaArmorLevels[index]
                + "\n防御 +" + GetArmorMetaDefBonus() + "  生命 +" + GetArmorMetaHpBonus();
            if (metaArmorLevels[index] % 5 == 0)
            {
                int tier = metaArmorLevels[index] / 5;
                toast += tier == 1 ? "\n解锁护甲护盾" : "\n护甲护盾强化";
                toast += "\n上限 " + GetArmorShieldMax(heroClass)
                    + "  回复延迟 " + GetArmorShieldRegenDelay(heroClass).ToString("0.0") + " 秒"
                    + "  回复 " + GetArmorShieldRegenPerSecond(heroClass).ToString("0.0") + "/秒";
            }

            SoulKnightDirector.Instance?.ShowToast(toast);
            return true;
        }

        public void SetArmorShieldCurrent(float value)
        {
            armorShieldCurrent = Mathf.Clamp(value, 0f, ArmorShieldMax);
            Changed?.Invoke();
        }

        private int AbsorbDamageWithArmorShield(int amount)
        {
            if (amount <= 0 || !HasArmorShield || armorShieldCurrent <= 0.01f)
            {
                return amount;
            }

            float absorb = Mathf.Min(armorShieldCurrent, amount);
            armorShieldCurrent -= absorb;
            if (armorShieldCurrent <= 0.01f)
            {
                armorShieldCurrent = 0f;
                armorShieldRegenReadyTime = Time.time + ArmorShieldRegenDelay;
            }

            Changed?.Invoke();
            return Mathf.Max(0, amount - Mathf.RoundToInt(absorb));
        }

        private void TickArmorShieldRegen()
        {
            if (!HasArmorShield || armorShieldCurrent >= ArmorShieldMax)
            {
                return;
            }

            if (Time.time < armorShieldRegenReadyTime)
            {
                return;
            }

            float previous = armorShieldCurrent;
            armorShieldCurrent = Mathf.Min(ArmorShieldMax, armorShieldCurrent + ArmorShieldRegenPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(previous, armorShieldCurrent))
            {
                Changed?.Invoke();
            }
        }

        public IReadOnlyList<MetaAffixKind> GetWeaponAffixes(HeroClass heroClass)
        {
            return metaWeaponAffixes[(int)heroClass];
        }

        public IReadOnlyList<MetaAffixKind> GetArmorAffixes(HeroClass heroClass)
        {
            return metaArmorAffixes[(int)heroClass];
        }

        public void OnPlayerDealtDamage(int finalDamage)
        {
            float rate = GetMetaLifeStealRate();
            if (rate <= 0f || NewInventorySystem.Instance == null)
            {
                return;
            }

            int heal = Mathf.Max(1, Mathf.RoundToInt(finalDamage * rate));
            NewInventorySystem.Instance.Heal(heal);
        }

        public int ModifyIncomingDamageWithMeta(int amount)
        {
            float reduction = GetMetaDamageReductionRate();
            if (reduction <= 0f)
            {
                return amount;
            }

            return Mathf.Max(0, Mathf.RoundToInt(amount * (1f - reduction)));
        }

        public void RestoreRunState(HeroClass heroClass, int[] treasureIds, float restoredMana)
        {
            runActive = true;
            currentClass = heroClass;
            treasures.Clear();
            if (treasureIds != null)
            {
                for (int i = 0; i < treasureIds.Length; i++)
                {
                    TreasureData treasure = FindTreasureById(treasureIds[i]);
                    if (treasure != null)
                    {
                        treasures.Add(treasure);
                    }
                }
            }

            mana = restoredMana;
            shieldUntil = 0f;
            armorShieldCurrent = GetArmorShieldMax(currentClass);
            armorShieldRegenReadyTime = 0f;
            Changed?.Invoke();
        }

        private TreasureData FindTreasureById(int id)
        {
            for (int i = 0; i < allTreasures.Count; i++)
            {
                if (allTreasures[i] != null && allTreasures[i].id == id)
                {
                    return allTreasures[i];
                }
            }

            return null;
        }

        private static float SumAffix(List<MetaAffixKind> affixes, MetaAffixKind kind, float perStack)
        {
            if (affixes == null)
            {
                return 0f;
            }

            int count = 0;
            for (int i = 0; i < affixes.Count; i++)
            {
                if (affixes[i] == kind)
                {
                    count++;
                }
            }

            return count * perStack;
        }

        private MetaAffixKind RollWeaponAffix()
        {
            MetaAffixKind[] pool =
            {
                MetaAffixKind.LifeSteal,
                MetaAffixKind.CritRate,
                MetaAffixKind.AttackSpeed
            };
            return pool[Random.Range(0, pool.Length)];
        }

        private MetaAffixKind RollArmorAffix()
        {
            MetaAffixKind[] pool =
            {
                MetaAffixKind.LifeSteal,
                MetaAffixKind.CritRate,
                MetaAffixKind.DamageReduction,
                MetaAffixKind.MaxHpPercent
            };
            return pool[Random.Range(0, pool.Length)];
        }

        private void SaveMetaProgress()
        {
            PlayerPrefs.SetInt("ModernRogue_MetaBossCurrency", metaBossCurrency);
            for (int i = 0; i < metaWeaponLevels.Length; i++)
            {
                PlayerPrefs.SetInt("ModernRogue_MetaWeaponLevel_" + i, metaWeaponLevels[i]);
                PlayerPrefs.SetInt("ModernRogue_MetaArmorLevel_" + i, metaArmorLevels[i]);
                SaveAffixList(metaWeaponAffixes[i], "ModernRogue_MetaWeaponAffix_" + i);
                SaveAffixList(metaArmorAffixes[i], "ModernRogue_MetaArmorAffix_" + i);
            }

            PlayerPrefs.Save();
        }

        private static void LoadAffixList(List<MetaAffixKind> target, string key)
        {
            target.Clear();
            string raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int value) && System.Enum.IsDefined(typeof(MetaAffixKind), value))
                {
                    target.Add((MetaAffixKind)value);
                }
            }
        }

        private static void SaveAffixList(List<MetaAffixKind> source, string key)
        {
            if (source == null || source.Count == 0)
            {
                PlayerPrefs.SetString(key, string.Empty);
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < source.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append((int)source[i]);
            }

            PlayerPrefs.SetString(key, builder.ToString());
        }

        public void ForceLevelUp()
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            inventory.PlayerStats.level++;
            ShowLevelChoices(inventory.PlayerStats.level);
            inventory.RefreshDerivedStats();
            Changed?.Invoke();
        }

        public void AddRandomTreasure()
        {
            if (allTreasures.Count == 0)
            {
                BuildTreasurePool();
            }

            TreasureData treasure = allTreasures[Random.Range(0, allTreasures.Count)];
            treasures.Add(treasure);
            ApplyTreasureImmediate(treasure);
            Changed?.Invoke();
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowToast("获得宝物：" + treasure.treasureName + "\n" + treasure.description);
            }
        }

        public void OnPerfectRoomCleared()
        {
            if (GetTreasureCount(TreasureKind.PerfectRoomGrowth) <= 0 || NewInventorySystem.Instance == null)
            {
                return;
            }

            NewInventorySystem.Instance.PlayerStats.permanentHpBonus += 2;
            NewInventorySystem.Instance.RefreshDerivedStats();
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowPickupTip("完美清房：生命上限 +2");
            }
        }

        public int GetClassArmorDropId()
        {
            switch (currentClass)
            {
                case HeroClass.Archer:
                    return 24;
                case HeroClass.Mage:
                    return 25;
                default:
                    return 23;
            }
        }

        public static string GetClassName(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Archer:
                    return "射手";
                case HeroClass.Mage:
                    return "法师";
                default:
                    return "战士";
            }
        }

        private static int GetClassWeaponId(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Archer:
                    return 10;
                case HeroClass.Mage:
                    return 9;
                default:
                    return 8;
            }
        }

        private static int GetStarterArmorId(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Archer:
                    return 21;
                case HeroClass.Mage:
                    return 22;
                default:
                    return 20;
            }
        }

        private static int GetRequiredExp(int level)
        {
            return 28 + level * 14;
        }

        private int GetTreasureCount(TreasureKind kind)
        {
            int count = 0;
            for (int i = 0; i < treasures.Count; i++)
            {
                if (treasures[i] != null && treasures[i].kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private int GetElementShardSetBonusSlots()
        {
            int[] counts = new int[6];
            for (int i = 0; i < treasures.Count; i++)
            {
                TreasureData treasure = treasures[i];
                if (treasure == null || treasure.kind != TreasureKind.ElementShard)
                {
                    continue;
                }

                counts[(int)treasure.element]++;
            }

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] >= 5)
                {
                    return 2;
                }
            }

            return 0;
        }

        private void ApplyTreasureImmediate(TreasureData treasure)
        {
            if (NewInventorySystem.Instance == null || treasure == null)
            {
                return;
            }

            PlayerStatBlock stats = NewInventorySystem.Instance.PlayerStats;
            switch (treasure.kind)
            {
                case TreasureKind.ManaSpring:
                    RestoreMana(30f);
                    break;
                case TreasureKind.ArmorCharm:
                    stats.permanentDefBonus += 1;
                    NewInventorySystem.Instance.RefreshDerivedStats();
                    break;
            }
        }

        private void ShowLevelChoices(int level)
        {
            if (canvas != null && canvas.name != "RunProgressionCanvas")
            {
                canvas = null;
            }

            EnsureCanvas();
            if (levelPanel != null)
            {
                Destroy(levelPanel);
            }

            GameObject backdrop = new GameObject("RunLevelChoiceBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvas.transform, false);
            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            RuntimeUIVisuals.StyleImage(backdrop.GetComponent<Image>(), new Color(0.01f, 0.015f, 0.03f, 0.78f));

            levelPanel = new GameObject("RunLevelChoicePanel", typeof(RectTransform), typeof(Image));
            levelPanel.transform.SetParent(canvas.transform, false);
            levelPanel.AddComponent<ModalPauseToken>();
            RectTransform rect = levelPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 360f);
            rect.anchoredPosition = Vector2.zero;
            RuntimeUIVisuals.StyleImage(levelPanel.GetComponent<Image>(), new Color(0.04f, 0.05f, 0.07f, 0.98f));
            RuntimeUIVisuals.AddFrame(levelPanel.transform, "PanelFrame", new Color(0.82f, 0.62f, 0.22f, 0.85f), 0.012f);

            GameObject badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(levelPanel.transform, false);
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 18f);
            badgeRect.sizeDelta = new Vector2(96f, 96f);
            RuntimeUIVisuals.StyleImage(badge.GetComponent<Image>(), new Color(0.72f, 0.52f, 0.18f, 1f));
            RuntimeUIVisuals.AddFrame(badge.transform, "BadgeFrame", new Color(1f, 0.9f, 0.62f, 0.9f), 0.08f);
            Text badgeText = CreateText(badge.transform, "Value", "Lv." + level, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(badgeText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));

            Text title = CreateText(levelPanel.transform, "Title", "等级提升 · 选择一项强化", 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = new Color(1f, 0.92f, 0.68f, 1f);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.92f));

            Text hint = CreateText(levelPanel.transform, "Hint", "本次升级仅在本局冒险内生效", 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            hint.color = new Color(0.72f, 0.78f, 0.88f, 0.92f);
            SetRect(hint.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.74f));

            RuntimeUIVisuals.CreateBlock(levelPanel.transform, "TitleDivider", new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.645f), new Color(0.72f, 0.52f, 0.18f, 0.55f));

            List<RunUpgradeChoice> choices = BuildChoices(level);
            for (int i = 0; i < choices.Count; i++)
            {
                CreateChoiceButton(choices[i], i);
            }
        }

        private List<RunUpgradeChoice> BuildChoices(int level)
        {
            List<RunUpgradeChoice> pool = new List<RunUpgradeChoice>
            {
                new RunUpgradeChoice("攻击 +2", "基础攻击提高。", () => NewInventorySystem.Instance.PlayerStats.permanentAtkBonus += 2),
                new RunUpgradeChoice("生命上限 +8", "提高容错。", () => NewInventorySystem.Instance.PlayerStats.permanentHpBonus += 8),
                new RunUpgradeChoice("防御 +1", "降低受到的伤害。", () => NewInventorySystem.Instance.PlayerStats.permanentDefBonus += 1),
                new RunUpgradeChoice("移动速度 +4%", "走位更轻快。", () => NewInventorySystem.Instance.PlayerStats.runMoveSpeedBonus += 0.04f)
            };

            if (level % 5 == 0)
            {
                pool.Add(new RunUpgradeChoice("稀有：暴击率 +8%", "第 5、10、15 级会出现的稀有词条。", () => NewInventorySystem.Instance.PlayerStats.runCritRateBonus += 0.08f));
                pool.Add(new RunUpgradeChoice("稀有：攻速 +10%", "提高连续输出。", () => NewInventorySystem.Instance.PlayerStats.runAttackSpeedBonus += 0.1f));
                if (currentClass == HeroClass.Mage)
                {
                    pool.Add(new RunUpgradeChoice("稀有：法力回复", "立刻获得法力喷泉宝物。", AddRandomTreasure));
                }
            }

            List<RunUpgradeChoice> result = new List<RunUpgradeChoice>();
            while (result.Count < 3 && pool.Count > 0)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }

        private void CreateChoiceButton(RunUpgradeChoice choice, int index)
        {
            bool isRare = choice.title.StartsWith("稀有");
            Color accent = ResolveChoiceAccent(choice.title, isRare);

            GameObject obj = new GameObject("Choice_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(levelPanel.transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            float left = 0.04f + index * 0.32f;
            rect.anchorMin = new Vector2(left, 0.1f);
            rect.anchorMax = new Vector2(left + 0.29f, 0.6f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(image, isRare ? new Color(0.18f, 0.14f, 0.08f, 0.98f) : new Color(0.1f, 0.13f, 0.18f, 0.98f));
            RuntimeUIVisuals.AddFrame(obj.transform, "CardFrame", isRare ? new Color(1f, 0.82f, 0.28f, 0.9f) : new Color(0.58f, 0.66f, 0.82f, 0.55f), 0.018f);

            RuntimeUIVisuals.CreateBlock(obj.transform, "AccentBar", new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), accent);

            Button button = obj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                choice.apply();
                NewInventorySystem.Instance.RefreshDerivedStats();
                Transform backdrop = canvas.transform.Find("RunLevelChoiceBackdrop");
                if (backdrop != null)
                {
                    Destroy(backdrop.gameObject);
                }

                Destroy(levelPanel);
                Changed?.Invoke();
            });

            Text titleText = CreateText(obj.transform, "Title", choice.title, isRare ? 19 : 18, FontStyle.Bold, TextAnchor.UpperCenter);
            titleText.color = isRare ? new Color(1f, 0.88f, 0.42f, 1f) : new Color(0.94f, 0.96f, 1f, 1f);
            SetRect(titleText.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.84f));

            Text descText = CreateText(obj.transform, "Desc", choice.description, 14, FontStyle.Normal, TextAnchor.UpperCenter);
            descText.color = new Color(0.74f, 0.8f, 0.9f, 0.95f);
            SetRect(descText.rectTransform, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.42f));

            if (isRare)
            {
                GameObject tagRoot = new GameObject("RareTag", typeof(RectTransform), typeof(Image));
                tagRoot.transform.SetParent(obj.transform, false);
                SetRect(tagRoot.GetComponent<RectTransform>(), new Vector2(0.68f, 0.86f), new Vector2(0.94f, 0.97f));
                RuntimeUIVisuals.StyleImage(tagRoot.GetComponent<Image>(), new Color(1f, 0.82f, 0.28f, 1f));
                Text tag = CreateText(tagRoot.transform, "Label", "稀有", 12, FontStyle.Bold, TextAnchor.MiddleCenter);
                tag.color = new Color(0.12f, 0.08f, 0.02f, 1f);
                SetRect(tag.rectTransform, Vector2.zero, Vector2.one);
            }
        }

        private static Color ResolveChoiceAccent(string title, bool isRare)
        {
            if (isRare)
            {
                return new Color(1f, 0.78f, 0.18f, 1f);
            }

            if (title.Contains("攻击"))
            {
                return new Color(0.92f, 0.34f, 0.28f, 1f);
            }

            if (title.Contains("生命"))
            {
                return new Color(0.28f, 0.82f, 0.42f, 1f);
            }

            if (title.Contains("防御"))
            {
                return new Color(0.28f, 0.58f, 0.96f, 1f);
            }

            if (title.Contains("速度"))
            {
                return new Color(0.28f, 0.86f, 0.92f, 1f);
            }

            return new Color(0.72f, 0.52f, 0.18f, 1f);
        }

        private void EnsureCanvas()
        {
            if (canvas != null && canvas.gameObject != null)
            {
                return;
            }

            GameObject existing = GameObject.Find("RunProgressionCanvas");
            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return;
                }
            }

            GameObject obj = new GameObject("RunProgressionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;
            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildTreasurePool()
        {
            allTreasures.Clear();
            allTreasures.Add(new TreasureData { id = 1, treasureName = "风之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Wind, iconKey = "Treasures/ElementalShards/Treasure_Shard_Wind", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 2, treasureName = "火之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Fire, iconKey = "Treasures/ElementalShards/Treasure_Shard_Fire", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 3, treasureName = "雷之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Thunder, iconKey = "Treasures/ElementalShards/Treasure_Shard_Thunder", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 4, treasureName = "水之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Water, iconKey = "Treasures/ElementalShards/Treasure_Shard_Water", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 5, treasureName = "土之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Earth, iconKey = "Treasures/Defense/Treasure_StoneHeart", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 11, treasureName = "金之碎片", kind = TreasureKind.ElementShard, element = CoreElement.Metal, iconKey = "Treasures/ElementalShards/Treasure_Shard_Metal", description = "五枚同属性碎片会额外解锁 2 个虫核镶嵌位。" });
            allTreasures.Add(new TreasureData { id = 6, treasureName = "讨价还价券", kind = TreasureKind.ShopRefresh, iconKey = "Treasures/Economy/Treasure_ShopRefreshToken", description = "本局商店刷新次数 +1。" });
            allTreasures.Add(new TreasureData { id = 7, treasureName = "命运骰子", kind = TreasureKind.UpgradeReroll, iconKey = "Treasures/Growth/Treasure_UpgradeRerollToken", description = "局内升级选项刷新次数 +1。" });
            allTreasures.Add(new TreasureData { id = 8, treasureName = "无伤徽记", kind = TreasureKind.PerfectRoomGrowth, iconKey = "Treasures/Growth/Treasure_PerfectRoomMedal", description = "完美清房时生命上限 +2。" });
            allTreasures.Add(new TreasureData { id = 9, treasureName = "法力泉眼", kind = TreasureKind.ManaSpring, iconKey = "Treasures/Growth/Treasure_ManaSeed", description = "法师法力上限和回复提升。" });
            allTreasures.Add(new TreasureData { id = 10, treasureName = "护甲扣环", kind = TreasureKind.ArmorCharm, iconKey = "Treasures/Defense/Treasure_GuardianShell", description = "获得时防御 +1。" });
            allTreasures.Add(new TreasureData { id = 12, treasureName = "凤凰之羽", kind = TreasureKind.Revive, iconKey = "Treasures/Growth/Treasure_ManaSeed", description = "生命归零时自动复活一次（恢复 50% 生命），随后失去该宝物。" });
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class RunUpgradeChoice
        {
            public readonly string title;
            public readonly string description;
            public readonly System.Action apply;

            public RunUpgradeChoice(string title, string description, System.Action apply)
            {
                this.title = title;
                this.description = description;
                this.apply = apply;
            }
        }
    }
}
