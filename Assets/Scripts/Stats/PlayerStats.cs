using UnityEngine;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    public class PlayerStats : CharacterStats
    {
        private const string CoinPrefsKey = "CoinAmount";

        [Header("Coins")]
        [SerializeField] private int coinAmount = 100;

        private Text coinText;
        private global::Player legacyPlayer;
        private int baseMaxHP;
        private int baseATK;
        private int baseDEF;
        private bool baseStatsCached;

        public int CoinAmount
        {
            get => coinAmount;
            set
            {
                coinAmount = Mathf.Max(0, value);
                SyncCoinState();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            CacheBaseStats();
            coinAmount = PlayerPrefs.GetInt(CoinPrefsKey, coinAmount);
            CacheCoinUi();
            SyncCoinState();
        }

        private void Start()
        {
            CacheCoinUi();
            SyncCoinState();
        }

        private void OnEnable()
        {
            CacheCoinUi();
            SyncCoinState();
        }

        private void OnDisable()
        {
            SaveCoinAmount();
        }

        private void OnApplicationQuit()
        {
            SaveCoinAmount();
        }

        public bool ConsumeCoin(int amount)
        {
            if (amount <= 0 || coinAmount < amount)
            {
                return false;
            }

            coinAmount -= amount;
            SyncCoinState();
            return true;
        }

        public void EarnCoin(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coinAmount += amount;
            SyncCoinState();
        }

        public void ApplyEquipmentBonuses(int strength, int intellect, int agility, int stamina, int weaponDamage)
        {
            CacheBaseStats();

            int previousMaxHP = maxHP;
            maxHP = Mathf.Max(1, baseMaxHP + Mathf.Max(0, stamina) * 5);
            atk = Mathf.Max(1, baseATK + Mathf.Max(0, weaponDamage) + Mathf.Max(0, strength) / 2 + Mathf.Max(0, intellect) / 3);
            def = Mathf.Max(0, baseDEF + Mathf.Max(0, agility) / 3 + Mathf.Max(0, stamina) / 4);

            if (maxHP > previousMaxHP && currentHP == previousMaxHP)
            {
                currentHP = maxHP;
            }

            currentHP = Mathf.Clamp(currentHP, isDead ? 0 : 1, maxHP);
        }

        protected override void Die()
        {
            base.Die();
            Debug.Log($"{name} triggered rogue-lite reset on death.");
        }

        private void CacheCoinUi()
        {
            if (coinText == null)
            {
                GameObject coinObject = GameObject.Find("Coin");
                if (coinObject != null)
                {
                    coinText = coinObject.GetComponentInChildren<Text>();
                }
            }

            if (legacyPlayer == null)
            {
                legacyPlayer = FindObjectOfType<global::Player>();
            }
        }

        private void CacheBaseStats()
        {
            if (baseStatsCached)
            {
                return;
            }

            baseMaxHP = maxHP;
            baseATK = atk;
            baseDEF = def;
            baseStatsCached = true;
        }

        private void SyncCoinState()
        {
            if (coinText != null)
            {
                coinText.text = coinAmount.ToString();
            }

            if (legacyPlayer != null)
            {
                legacyPlayer.CoinAmount = coinAmount;
            }

            PlayerPrefs.SetInt(CoinPrefsKey, coinAmount);
        }

        private void SaveCoinAmount()
        {
            PlayerPrefs.SetInt(CoinPrefsKey, coinAmount);
            PlayerPrefs.Save();
        }
    }
}
