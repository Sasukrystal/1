using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    [System.Serializable]
    public class RunSaveData
    {
        public bool hasRun;
        public int stage;
        public float playerX;
        public float playerY;
        public int heroClass;
        public int coins;
        public int level;
        public int experience;
        public int currentHp;
        public int permanentAtkBonus;
        public int permanentDefBonus;
        public int permanentHpBonus;
        public float runMoveSpeedBonus;
        public float runCritRateBonus;
        public float runAttackSpeedBonus;
        public int weaponId;
        public int armorId;
        public int runGold;
        public int killCount;
        public float mana;
        public float armorShieldCurrent;
        public int[] treasureIds;
        public int[] slotItemIds;
        public int[] slotCounts;
        public CoreInstance[] equippedCores;
        public CoreInstance[] coreBag;
    }

    public static class GameSaveService
    {
        private const string RunSaveKey = "ModernRogue_RunSave";

        public static bool HasRunSave()
        {
            RunSaveData data = Load();
            return data != null && data.hasRun && data.stage > 0;
        }

        public static void ClearRunSave()
        {
            PlayerPrefs.DeleteKey(RunSaveKey);
            PlayerPrefs.Save();
        }

        public static void SaveCurrentRun(SoulKnightDirector director, bool flushToDisk = true)
        {
            if (director == null || director.CurrentStage <= 0)
            {
                return;
            }

            RunProgressionSystem run = RunProgressionSystem.Instance;
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (run == null || inventory == null || !run.IsRunActive)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;
            PlayerStatBlock stats = inventory.PlayerStats;

            List<int> treasureIds = new List<int>();
            for (int i = 0; i < run.Treasures.Count; i++)
            {
                if (run.Treasures[i] != null)
                {
                    treasureIds.Add(run.Treasures[i].id);
                }
            }

            List<int> itemIds = new List<int>();
            List<int> itemCounts = new List<int>();
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                ItemSlot slot = inventory.Slots[i];
                itemIds.Add(slot.itemId);
                itemCounts.Add(slot.count);
            }

            RunSaveData data = new RunSaveData
            {
                hasRun = true,
                stage = director.CurrentStage,
                playerX = pos.x,
                playerY = pos.y,
                heroClass = (int)run.CurrentClass,
                coins = stats.coins,
                level = stats.level,
                experience = stats.experience,
                currentHp = stats.currentHp,
                permanentAtkBonus = stats.permanentAtkBonus,
                permanentDefBonus = stats.permanentDefBonus,
                permanentHpBonus = stats.permanentHpBonus,
                runMoveSpeedBonus = stats.runMoveSpeedBonus,
                runCritRateBonus = stats.runCritRateBonus,
                runAttackSpeedBonus = stats.runAttackSpeedBonus,
                weaponId = inventory.CurrentWeaponSlot.itemId,
                armorId = inventory.CurrentArmorSlot.itemId,
                runGold = director.RunGold,
                killCount = director.KillCount,
                mana = run.Mana,
                armorShieldCurrent = run.ArmorShieldCurrent,
                treasureIds = treasureIds.ToArray(),
                slotItemIds = itemIds.ToArray(),
                slotCounts = itemCounts.ToArray(),
                equippedCores = CloneCoreList(inventory.EquippedCores),
                coreBag = CloneCoreList(inventory.CoreBag)
            };

            PlayerPrefs.SetString(RunSaveKey, JsonUtility.ToJson(data));
            if (flushToDisk)
            {
                PlayerPrefs.Save();
            }
        }

        public static bool TryRestoreRun(SoulKnightDirector director)
        {
            RunSaveData data = Load();
            if (director == null || data == null || !data.hasRun || data.stage <= 0)
            {
                return false;
            }

            RunProgressionSystem run = RunProgressionSystem.Instance;
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (run == null || inventory == null)
            {
                return false;
            }

            run.RestoreRunState((HeroClass)Mathf.Clamp(data.heroClass, 0, 2), data.treasureIds, data.mana);
            run.SetArmorShieldCurrent(data.armorShieldCurrent);
            inventory.RestoreRunState(data);
            director.RestoreRunState(data.stage, data.runGold, data.killCount, new Vector2(data.playerX, data.playerY));
            return true;
        }

        private static RunSaveData Load()
        {
            if (!PlayerPrefs.HasKey(RunSaveKey))
            {
                return null;
            }

            string json = PlayerPrefs.GetString(RunSaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonUtility.FromJson<RunSaveData>(json);
        }

        private static CoreInstance[] CloneCoreList(IReadOnlyList<CoreInstance> cores)
        {
            if (cores == null || cores.Count == 0)
            {
                return new CoreInstance[0];
            }

            CoreInstance[] copy = new CoreInstance[cores.Count];
            for (int i = 0; i < cores.Count; i++)
            {
                copy[i] = cores[i];
            }

            return copy;
        }
    }
}
