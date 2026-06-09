using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class NewInventorySystem : MonoBehaviour
    {
        public static NewInventorySystem Instance { get; private set; }

        [SerializeField] private int capacity = 24;
        [SerializeField] private PlayerStatBlock playerStats = new PlayerStatBlock();

        private readonly List<ItemSlot> slots = new List<ItemSlot>();
        private readonly List<CoreInstance> equippedCores = new List<CoreInstance>();
        private readonly List<CoreInstance> coreBag = new List<CoreInstance>();
        private ItemSlot currentWeaponSlot = new ItemSlot();
        private ItemSlot currentArmorSlot = new ItemSlot();
        private Bagsys.RogueLike.PlayerStats legacyPlayerStats;
        private int nextCoreInstanceId = 1;

        public event Action Changed;

        public IReadOnlyList<ItemSlot> Slots => slots;
        public ItemSlot CurrentWeaponSlot => currentWeaponSlot;
        public ItemSlot CurrentArmorSlot => currentArmorSlot;
        public PlayerStatBlock PlayerStats => playerStats;
        public IReadOnlyList<CoreInstance> EquippedCores => equippedCores;
        public IReadOnlyList<CoreInstance> CoreBag => coreBag;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameDataModel.EnsureLoaded();
            EnsureSlots();
            SeedStarterItems();
            RefreshPlayerStats();
        }

        public bool AddItem(int id, int count)
        {
            ItemData item = GameDataModel.GetItem(id);
            if (item == null || count <= 0)
            {
                return false;
            }

            if (RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsRunActive)
            {
                if (GameDataModel.IsAdvancedEquipment(item))
                {
                    return false;
                }

                if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
                {
                    return false;
                }
            }

            int remaining = count;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                ItemSlot slot = slots[i];
                if (slot.itemId != id || slot.count >= item.maxStack)
                {
                    continue;
                }

                int move = Mathf.Min(remaining, item.maxStack - slot.count);
                slot.count += move;
                remaining -= move;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                ItemSlot slot = slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                int move = Mathf.Min(remaining, item.maxStack);
                slot.itemId = id;
                slot.count = move;
                remaining -= move;
            }

            NotifyChanged();
            return remaining == 0;
        }

        public void RemoveItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            slots[slotIndex].Clear();
            NotifyChanged();
        }

        public void EquipFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            ItemSlot source = slots[slotIndex];
            ItemData item = GameDataModel.GetItem(source.itemId);
            if (item == null || source.IsEmpty)
            {
                return;
            }

            if (item.itemType == ItemType.Weapon)
            {
                if (RunProgressionSystem.Instance != null)
                {
                    return;
                }

                SwapEquipSlot(source, currentWeaponSlot);
            }
            else if (item.itemType == ItemType.Armor)
            {
                if (RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsRunActive)
                {
                    return;
                }

                if (item.classRestricted && RunProgressionSystem.Instance != null && item.requiredClass != RunProgressionSystem.Instance.CurrentClass)
                {
                    return;
                }

                SwapEquipSlot(source, currentArmorSlot);
            }
            else if (item.itemType == ItemType.Consumable)
            {
                Heal(item.bonusMaxHp > 0 ? item.bonusMaxHp : 15);
                source.count--;
                if (source.count <= 0)
                {
                    source.Clear();
                }
            }

            RefreshPlayerStats();
            NotifyChanged();
        }

        public bool TryUseQuickSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return false;
            }

            ItemSlot source = slots[slotIndex];
            ItemData item = GameDataModel.GetItem(source.itemId);
            if (item == null || source.IsEmpty)
            {
                return false;
            }

            if (item.itemType != ItemType.Consumable)
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.ShowPickupTip("快捷栏仅可使用消耗品");
                }

                return false;
            }

            EquipFromSlot(slotIndex);
            return true;
        }

        public void UnequipWeapon()
        {
            UnequipToInventory(currentWeaponSlot);
        }

        public void UnequipArmor()
        {
            if (RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsRunActive)
            {
                return;
            }

            UnequipToInventory(currentArmorSlot);
        }

        public void RestoreRunState(RunSaveData data)
        {
            if (data == null)
            {
                return;
            }

            ResetRunGrowth();
            playerStats.coins = data.coins;
            playerStats.level = data.level;
            playerStats.experience = data.experience;
            playerStats.currentHp = data.currentHp;
            playerStats.permanentAtkBonus = data.permanentAtkBonus;
            playerStats.permanentDefBonus = data.permanentDefBonus;
            playerStats.permanentHpBonus = data.permanentHpBonus;
            playerStats.runMoveSpeedBonus = data.runMoveSpeedBonus;
            playerStats.runCritRateBonus = data.runCritRateBonus;
            playerStats.runAttackSpeedBonus = data.runAttackSpeedBonus;
            currentWeaponSlot.itemId = data.weaponId;
            currentWeaponSlot.count = data.weaponId > 0 ? 1 : 0;
            currentArmorSlot.itemId = data.armorId;
            currentArmorSlot.count = data.armorId > 0 ? 1 : 0;

            if (data.slotItemIds != null && data.slotCounts != null)
            {
                for (int i = 0; i < slots.Count && i < data.slotItemIds.Length && i < data.slotCounts.Length; i++)
                {
                    slots[i].itemId = data.slotItemIds[i];
                    slots[i].count = data.slotCounts[i];
                }
            }

            equippedCores.Clear();
            coreBag.Clear();
            if (data.equippedCores != null)
            {
                for (int i = 0; i < data.equippedCores.Length; i++)
                {
                    equippedCores.Add(data.equippedCores[i]);
                }
            }

            if (data.coreBag != null)
            {
                for (int i = 0; i < data.coreBag.Length; i++)
                {
                    coreBag.Add(data.coreBag[i]);
                }
            }

            RefreshDerivedStats();
        }

        public bool UpgradeBaseStat()
        {
            const int cost = 20;
            if (playerStats.coins < cost)
            {
                return false;
            }

            playerStats.coins -= cost;
            playerStats.permanentAtkBonus += 1;
            playerStats.permanentDefBonus += 1;
            playerStats.permanentHpBonus += 3;
            RefreshPlayerStats();
            NotifyChanged();
            return true;
        }

        public void EarnCoins(int amount)
        {
            playerStats.coins += Mathf.Max(0, amount);
            NotifyChanged();
        }

        public void AddExperience(int amount)
        {
            int gain = Mathf.Max(0, amount);
            if (gain <= 0)
            {
                return;
            }

            if (RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsRunActive)
            {
                RunProgressionSystem.Instance.AddRunExperience(gain);
                NotifyChanged();
                return;
            }

            playerStats.experience += gain;
            while (playerStats.experience >= playerStats.level * 60)
            {
                playerStats.experience -= playerStats.level * 60;
                playerStats.level++;
                playerStats.permanentHpBonus += 2;
            }

            RefreshPlayerStats();
            NotifyChanged();
        }

        public bool EquipCore(int coreId)
        {
            return AddCore(coreId, CoreQuality.Rare);
        }

        public bool AddCore(int templateId, CoreQuality quality)
        {
            if (GameDataModel.GetCore(templateId) == null)
            {
                return false;
            }

            CoreInstance instance = GameDataModel.CreateCoreInstance(nextCoreInstanceId++, templateId, quality);
            int capacity = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CoreSlotCapacity : 6;
            if (equippedCores.Count < capacity)
            {
                equippedCores.Add(instance);
            }
            else
            {
                coreBag.Add(instance);
            }

            RefreshPlayerStats();
            NotifyChanged();
            return true;
        }

        public bool EquipCoreInstance(int instanceId)
        {
            int capacity = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CoreSlotCapacity : 6;
            if (equippedCores.Count >= capacity)
            {
                return false;
            }

            CoreInstance core = TakeCoreFromList(coreBag, instanceId);
            if (core == null)
            {
                return false;
            }

            equippedCores.Add(core);
            RefreshPlayerStats();
            NotifyChanged();
            return true;
        }

        public bool UnequipCoreInstance(int instanceId)
        {
            CoreInstance core = TakeCoreFromList(equippedCores, instanceId);
            if (core == null)
            {
                return false;
            }

            coreBag.Add(core);
            RefreshPlayerStats();
            NotifyChanged();
            return true;
        }

        public bool RemoveCoreInstance(int instanceId)
        {
            CoreInstance core = TakeCoreFromList(coreBag, instanceId);
            bool equipped = false;
            if (core == null)
            {
                core = TakeCoreFromList(equippedCores, instanceId);
                equipped = core != null;
            }

            if (core == null)
            {
                return false;
            }

            if (equipped)
            {
                RefreshPlayerStats();
            }

            NotifyChanged();
            return true;
        }

        public int CountEquippedCores(CoreElement element)
        {
            int count = 0;
            for (int i = 0; i < equippedCores.Count; i++)
            {
                CoreData core = GameDataModel.GetCore(equippedCores[i].templateId);
                if (core != null && core.element == element)
                {
                    count++;
                }
            }

            return count;
        }

        public float GetMoveSpeedMultiplier()
        {
            int windCount = CountEquippedCores(CoreElement.Wind);
            float setBonus = 0f;
            if (windCount >= 2)
            {
                setBonus += 0.1f;
            }

            if (windCount >= 3)
            {
                setBonus += 0.1f;
            }

            if (windCount >= 4)
            {
                setBonus += 0.1f;
            }

            return 1f + setBonus + playerStats.coreMoveSpeedBonus + playerStats.runMoveSpeedBonus;
        }

        public bool HasWindDashShockwave()
        {
            return CountEquippedCores(CoreElement.Wind) >= 3;
        }

        private static CoreInstance TakeCoreFromList(List<CoreInstance> list, int instanceId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].instanceId == instanceId)
                {
                    CoreInstance core = list[i];
                    list.RemoveAt(i);
                    return core;
                }
            }

            return null;
        }

        public bool SpendCoins(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (playerStats.coins < amount)
            {
                return false;
            }

            playerStats.coins -= amount;
            NotifyChanged();
            return true;
        }

        public void AddBaseAttack(int amount)
        {
            playerStats.baseAtk += Mathf.Max(0, amount);
            RefreshPlayerStats();
            NotifyChanged();
        }

        public void TakeDamage(int amount)
        {
            if (PlayerLifeState2D.Instance != null && PlayerLifeState2D.Instance.IsDeadOrDying)
            {
                return;
            }

            if (RunProgressionSystem.Instance == null || !RunProgressionSystem.Instance.IsRunActive)
            {
                return;
            }

            int damage = Mathf.Max(1, amount);
            if (RunProgressionSystem.Instance != null)
            {
                damage = RunProgressionSystem.Instance.ModifyIncomingDamage(damage);
            }

            playerStats.currentHp = Mathf.Max(0, playerStats.currentHp - damage);
            GameAudioService.Ensure()?.PlayHeroHurt();
            PlayerVisualFeedback2D.PlayHit(FindPlayerTransform());

            if (playerStats.currentHp <= 0)
            {
                if (TryReviveAtZeroHp())
                {
                    return;
                }

                playerStats.currentHp = 0;
                RefreshPlayerStats();
                NotifyChanged();
                PlayerLifeState2D.Ensure(FindPlayerTransform())?.BeginDeathSequence();
                return;
            }

            RefreshPlayerStats();
            NotifyChanged();
        }

        private bool TryReviveAtZeroHp()
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            if (run != null && run.TryConsumeReviveTreasure(out string treasureName))
            {
                ApplyRevive(0.5f, treasureName);
                return true;
            }

            if (TryConsumeReviveConsumable(out string itemName))
            {
                ApplyRevive(0.45f, itemName);
                return true;
            }

            return false;
        }

        private bool TryConsumeReviveConsumable(out string itemName)
        {
            itemName = null;
            for (int i = 0; i < slots.Count; i++)
            {
                ItemSlot slot = slots[i];
                ItemData item = GameDataModel.GetItem(slot.itemId);
                if (item == null || slot.IsEmpty || !GameDataModel.IsReviveConsumable(item))
                {
                    continue;
                }

                itemName = item.itemName;
                slot.count--;
                if (slot.count <= 0)
                {
                    slot.Clear();
                }

                return true;
            }

            return false;
        }

        private void ApplyRevive(float hpRatio, string sourceName)
        {
            ItemData armor = GetEquippedArmor();
            int maxHp = playerStats.TotalMaxHp(armor);
            playerStats.currentHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * Mathf.Clamp01(hpRatio)));
            RefreshPlayerStats();
            NotifyChanged();
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.RestoreArmorShieldAfterRevive();
            }

            PlayerVisualFeedback2D.PlayReviveBurst(FindPlayerTransform());
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowPickupTip(sourceName + " 触发复活");
            }
        }

        private static Transform FindPlayerTransform()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            return playerObject != null ? playerObject.transform : null;
        }

        public void Heal(int amount)
        {
            playerStats.currentHp = Mathf.Min(playerStats.TotalMaxHp(GetEquippedArmor()), playerStats.currentHp + Mathf.Max(0, amount));
            RefreshPlayerStats();
            NotifyChanged();
        }

        public ItemData GetEquippedWeapon()
        {
            return GameDataModel.GetItem(currentWeaponSlot.itemId);
        }

        public ItemData GetEquippedArmor()
        {
            return GameDataModel.GetItem(currentArmorSlot.itemId);
        }

        public void SetFixedLoadout(int weaponId, int armorId)
        {
            currentWeaponSlot.itemId = weaponId;
            currentWeaponSlot.count = 1;
            currentArmorSlot.itemId = armorId;
            currentArmorSlot.count = 1;
            RefreshPlayerStats();
            playerStats.currentHp = playerStats.TotalMaxHp(GetEquippedArmor());
            NotifyChanged();
        }

        public void ResetRunGrowth()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
            }

            equippedCores.Clear();
            coreBag.Clear();
            playerStats.level = 1;
            playerStats.experience = 0;
            playerStats.permanentAtkBonus = 0;
            playerStats.permanentDefBonus = 0;
            playerStats.permanentHpBonus = 0;
            playerStats.coreMoveSpeedBonus = 0f;
            playerStats.coreCritRateBonus = 0f;
            playerStats.coreAttackSpeedBonus = 0f;
            playerStats.runMoveSpeedBonus = 0f;
            playerStats.runCritRateBonus = 0f;
            playerStats.runAttackSpeedBonus = 0f;
            AddItem(1, 12);
            AddItem(2, 3);
            RefreshPlayerStats();
            playerStats.currentHp = playerStats.TotalMaxHp(GetEquippedArmor());
            NotifyChanged();
        }

        public void RefreshDerivedStats()
        {
            RefreshPlayerStats();
            NotifyChanged();
        }

        private void EnsureSlots()
        {
            while (slots.Count < capacity)
            {
                slots.Add(new ItemSlot());
            }
        }

        private void SeedStarterItems()
        {
            bool hasAny = false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    hasAny = true;
                    break;
                }
            }

            if (hasAny)
            {
                return;
            }

            AddItem(1, 12);
            AddItem(2, 3);
        }

        private void SwapEquipSlot(ItemSlot source, ItemSlot equipmentSlot)
        {
            int oldId = equipmentSlot.itemId;
            int oldCount = equipmentSlot.count;
            equipmentSlot.itemId = source.itemId;
            equipmentSlot.count = 1;
            source.Clear();

            if (oldId > 0)
            {
                source.itemId = oldId;
                source.count = oldCount > 0 ? oldCount : 1;
            }
        }

        private void UnequipToInventory(ItemSlot equipmentSlot)
        {
            if (equipmentSlot.IsEmpty)
            {
                return;
            }

            if (AddItem(equipmentSlot.itemId, equipmentSlot.count))
            {
                equipmentSlot.Clear();
                RefreshPlayerStats();
                NotifyChanged();
            }
        }

        private void RefreshPlayerStats()
        {
            RefreshCoreStatBonuses();
            RefreshMetaStatBonuses();
            ItemData weapon = GetEquippedWeapon();
            ItemData armor = GetEquippedArmor();
            int maxHp = playerStats.TotalMaxHp(armor);
            playerStats.currentHp = Mathf.Clamp(playerStats.currentHp, 0, maxHp);

            if (legacyPlayerStats == null)
            {
                legacyPlayerStats = FindObjectOfType<Bagsys.RogueLike.PlayerStats>();
            }

            if (legacyPlayerStats != null)
            {
                legacyPlayerStats.ApplyEquipmentBonuses(
                    Mathf.Max(0, playerStats.permanentAtkBonus * 2 + playerStats.coreAtkBonus),
                    Mathf.Max(0, playerStats.coreDefBonus),
                    0,
                    Mathf.Max(0, playerStats.permanentHpBonus / 3 + playerStats.coreHpBonus),
                    weapon != null ? weapon.bonusAtk : 0);
                legacyPlayerStats.CoinAmount = playerStats.coins;
            }
        }

        private void RefreshCoreStatBonuses()
        {
            playerStats.coreAtkBonus = 0;
            playerStats.coreDefBonus = 0;
            playerStats.coreHpBonus = 0;
            playerStats.coreMoveSpeedBonus = 0f;
            playerStats.coreCritRateBonus = 0f;
            playerStats.coreAttackSpeedBonus = 0f;

            for (int i = 0; i < equippedCores.Count; i++)
            {
                CoreInstance core = equippedCores[i];
                if (core == null || core.stats == null)
                {
                    continue;
                }

                for (int j = 0; j < core.stats.Count; j++)
                {
                    CoreStatRoll roll = core.stats[j];
                    if (roll == null)
                    {
                        continue;
                    }

                    switch (roll.kind)
                    {
                        case CoreStatKind.Attack:
                            playerStats.coreAtkBonus += Mathf.RoundToInt(roll.value);
                            break;
                        case CoreStatKind.Defense:
                            playerStats.coreDefBonus += Mathf.RoundToInt(roll.value);
                            break;
                        case CoreStatKind.MaxHp:
                            playerStats.coreHpBonus += Mathf.RoundToInt(roll.value);
                            break;
                        case CoreStatKind.MoveSpeed:
                            playerStats.coreMoveSpeedBonus += roll.value;
                            break;
                        case CoreStatKind.CritRate:
                            playerStats.coreCritRateBonus += roll.value;
                            break;
                        case CoreStatKind.AttackSpeed:
                            playerStats.coreAttackSpeedBonus += roll.value;
                            break;
                    }
                }
            }
        }

        private void RefreshMetaStatBonuses()
        {
            playerStats.metaDefBonus = 0;
            playerStats.metaHpBonus = 0;
            playerStats.metaMaxHpPercentBonus = 0f;
            playerStats.metaCritRateBonus = 0f;
            playerStats.metaAttackSpeedBonus = 0f;
            RunProgressionSystem run = RunProgressionSystem.Instance;
            if (run == null)
            {
                return;
            }

            playerStats.metaDefBonus = run.GetArmorMetaDefBonus();
            playerStats.metaHpBonus = run.GetArmorMetaHpBonus();
            playerStats.metaMaxHpPercentBonus = run.GetMetaMaxHpPercentBonus();
            playerStats.metaCritRateBonus = run.GetMetaCritRateBonus();
            playerStats.metaAttackSpeedBonus = run.GetMetaAttackSpeedBonus();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
