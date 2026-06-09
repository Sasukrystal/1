using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(-520)]
    [DisallowMultipleComponent]
    public class ElementalCoreCombatSystem : MonoBehaviour
    {
        public static ElementalCoreCombatSystem Instance { get; private set; }

        private Transform player;
        private float nextPuddleTime;
        private float nextWaterHealTime;
        private bool windCritReady;
        private int windDodgeCharges;
        private float nextWindChargeTime;
        private float temporaryAttackSpeedUntil;
        private float lightningWindowStart;
        private int storedLightningDamage;

        public float TemporaryAttackSpeedBonus => Time.time < temporaryAttackSpeedUntil ? 0.15f : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            lightningWindowStart = Time.time;
        }

        private void Update()
        {
            CachePlayer();
            TickWaterTrail();
            TickWindCharge();
            if (storedLightningDamage <= 0 && Time.time - lightningWindowStart >= 3f)
            {
                lightningWindowStart = Time.time;
            }
        }

        public int Count(CoreElement element)
        {
            return NewInventorySystem.Instance != null ? NewInventorySystem.Instance.CountEquippedCores(element) : 0;
        }

        public int GetWetDefenseReduction(GameObject enemy)
        {
            return enemy != null && enemy.GetComponent<WetDebuff2D>() != null ? 2 : 0;
        }

        public float GetEnemyMoveMultiplier(GameObject enemy)
        {
            return enemy != null && enemy.GetComponent<WetDebuff2D>() != null ? 0.78f : 1f;
        }

        public void OnPlayerDodge()
        {
            if (Count(CoreElement.Wind) >= 3)
            {
                windCritReady = true;
            }
        }

        public bool TryConsumeWindDodgeCharge()
        {
            if (Count(CoreElement.Wind) < 4)
            {
                return false;
            }

            if (windDodgeCharges <= 0)
            {
                return false;
            }

            windDodgeCharges--;
            nextWindChargeTime = Time.time + 3.2f;
            OnPlayerDodge();
            return true;
        }

        public void OnRoomCleared(bool perfect)
        {
            int metal = Count(CoreElement.Metal);
            if (metal >= 2 && SoulKnightDirector.Instance != null)
            {
                int amount = perfect ? 15 : 10;
                SoulKnightDirector.Instance.AddRunGold(amount);
                SoulKnightDirector.Instance.ShowPickupTip("金核通关奖励：金币 +" + amount);
            }

            if (Count(CoreElement.Wind) >= 4)
            {
                windDodgeCharges = 1;
            }
        }

        public int BonusCoinsOnEnemyDeath()
        {
            int metal = Count(CoreElement.Metal);
            if (metal >= 3)
            {
                return 2;
            }

            return metal >= 2 ? 1 : 0;
        }

        public int ModifyOutgoingDamage(Bagsys.RogueLike.EnemyStats enemy, int rawDamage)
        {
            int damage = Mathf.Max(1, rawDamage);

            if (Count(CoreElement.Water) >= 4 && enemy != null && enemy.GetComponent<WetDebuff2D>() != null)
            {
                damage = Mathf.CeilToInt(damage * 1.08f);
            }

            int wind = Count(CoreElement.Wind);
            if (wind >= 2 && NewInventorySystem.Instance != null)
            {
                float speedBonus = Mathf.Max(0f, NewInventorySystem.Instance.GetMoveSpeedMultiplier() - 1f);
                damage = Mathf.CeilToInt(damage * (1f + speedBonus * 0.8f));
            }

            if (windCritReady)
            {
                windCritReady = false;
                damage *= 2;
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.ShowPickupTip("风核暴击！");
                }
            }

            int thunder = Count(CoreElement.Thunder);
            if (thunder >= 4 && enemy != null && enemy.GetComponent<ElectrifiedDebuff2D>() != null)
            {
                ElectrifiedDebuff2D debuff = enemy.GetComponent<ElectrifiedDebuff2D>();
                if (debuff != null)
                {
                    damage = Mathf.CeilToInt(damage * (1f + debuff.DeepeningMultiplier));
                    debuff.AddDeepeningStack();
                }

                if (storedLightningDamage > 0 && Time.time - lightningWindowStart >= 3f)
                {
                    enemy.TakeTrueDamage(storedLightningDamage, player != null ? player.position : enemy.transform.position);
                    storedLightningDamage = 0;
                    lightningWindowStart = Time.time;
                }
            }

            return damage;
        }

        public void AfterPlayerDamagedEnemy(Bagsys.RogueLike.EnemyStats enemy, int finalDamage)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            TrySpawnFireOrb(finalDamage);
            TryApplyThunder(enemy);
            TryMetalExecuteAndFall(enemy, finalDamage);
        }

        public void HealPlayerFromWater()
        {
            if (Time.time < nextWaterHealTime || Count(CoreElement.Water) < 3 || NewInventorySystem.Instance == null)
            {
                return;
            }

            nextWaterHealTime = Time.time + 0.8f;
            NewInventorySystem.Instance.Heal(1);
        }

        public int ResolveShopPrice(int basePrice)
        {
            return Count(CoreElement.Metal) >= 3 ? Mathf.CeilToInt(basePrice * 0.9f) : basePrice;
        }

        private void TrySpawnFireOrb(int finalDamage)
        {
            int fire = Count(CoreElement.Fire);
            if (fire < 2 || player == null)
            {
                return;
            }

            float chance = 0.45f + (fire >= 3 ? 0.15f : 0f) + (fire >= 4 ? 0.2f : 0f);
            if (Random.value > chance)
            {
                return;
            }

            int damage = Mathf.Max(2, Mathf.RoundToInt(finalDamage * (fire >= 4 ? 0.42f : 0.32f)));
            FireOrb2D.Create(player, damage, fire >= 3, fire >= 4);
        }

        private void TryApplyThunder(Bagsys.RogueLike.EnemyStats enemy)
        {
            int thunder = Count(CoreElement.Thunder);
            if (thunder < 2)
            {
                return;
            }

            float chance = thunder >= 3 ? 0.8f : 0.5f;
            if (Random.value <= chance)
            {
                enemy.gameObject.AddOrRefreshDebuff<ElectrifiedDebuff2D>(4f);
            }

            ElectrifiedDebuff2D[] electrified = FindObjectsOfType<ElectrifiedDebuff2D>();
            bool soloThunderFour = thunder >= 4 && CountAliveEnemies() <= 1 && enemy.GetComponent<ElectrifiedDebuff2D>() != null;
            if ((electrified.Length < 2 && !soloThunderFour) || enemy.GetComponent<ElectrifiedDebuff2D>() == null)
            {
                return;
            }

            int chainDamage = 6 + thunder * 2;
            bool chained = false;
            for (int i = 0; i < electrified.Length; i++)
            {
                if (electrified[i] != null && electrified[i].TryGetComponent(out Bagsys.RogueLike.EnemyStats target) && target != enemy)
                {
                    target.TakeTrueDamage(chainDamage, enemy.transform.position);
                    storedLightningDamage += Mathf.RoundToInt(chainDamage * 0.6f);
                    chained = true;
                }
            }

            if (soloThunderFour && !chained)
            {
                storedLightningDamage += Mathf.RoundToInt(chainDamage * 0.6f);
                chained = true;
            }

            if (thunder >= 3 && chained)
            {
                temporaryAttackSpeedUntil = Time.time + 2f;
            }
        }

        private void TryMetalExecuteAndFall(Bagsys.RogueLike.EnemyStats enemy, int finalDamage)
        {
            int metal = Count(CoreElement.Metal);
            if (metal >= 3 && enemy.CurrentHP > 0 && enemy.CurrentHP <= enemy.MaxHP * 0.04f)
            {
                enemy.TakeTrueDamage(enemy.CurrentHP, player != null ? player.position : enemy.transform.position);
                return;
            }

            if (metal >= 4 && Random.value <= 0.25f)
            {
                int gold = SoulKnightDirector.Instance != null ? SoulKnightDirector.Instance.RunGold : 0;
                int fallDamage = Mathf.Clamp(5 + gold / 35, 5, 36);
                FallingGoldStrike2D.Create(enemy, fallDamage);
            }
        }

        private void TickWindCharge()
        {
            if (Count(CoreElement.Wind) < 4)
            {
                windDodgeCharges = 0;
                return;
            }

            if (windDodgeCharges <= 0 && Time.time >= nextWindChargeTime)
            {
                windDodgeCharges = 1;
                nextWindChargeTime = Time.time + 3.2f;
            }
        }

        private void TickWaterTrail()
        {
            if (Count(CoreElement.Water) < 2)
            {
                return;
            }

            CachePlayer();
            if (player == null || Time.time < nextPuddleTime)
            {
                return;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null || body.velocity.sqrMagnitude < 0.04f)
            {
                return;
            }

            nextPuddleTime = Time.time + 0.28f;
            PlayerVisualHub2D hub = PlayerVisualHub2D.Get(player);
            Vector2 puddlePosition = hub != null ? hub.ResolveFeetWorld() : (Vector2)player.position;
            WaterPuddle2D.Create(puddlePosition);
        }

        private void CachePlayer()
        {
            if (player != null)
            {
                return;
            }

            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
            {
                player = obj.transform;
            }
        }

        private static int CountAliveEnemies()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                {
                    continue;
                }

                Bagsys.RogueLike.EnemyStats enemy = enemies[i].GetComponent<Bagsys.RogueLike.EnemyStats>();
                if (enemy != null && !enemy.IsDead)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public abstract class TimedDebuff2D : MonoBehaviour
    {
        private float endTime;

        public void Refresh(float duration)
        {
            endTime = Time.time + duration;
        }

        protected virtual void Update()
        {
            if (Time.time >= endTime)
            {
                Destroy(this);
            }
        }
    }

    public class WetDebuff2D : TimedDebuff2D
    {
    }

    public class ElectrifiedDebuff2D : TimedDebuff2D
    {
        private int deepeningStacks;

        public float DeepeningMultiplier => Mathf.Min(0.08f, deepeningStacks * 0.008f);

        public void AddDeepeningStack()
        {
            deepeningStacks = Mathf.Min(10, deepeningStacks + 1);
        }
    }

    public class BurningDebuff2D : TimedDebuff2D
    {
        private float nextTick;

        protected override void Update()
        {
            base.Update();
            if (Time.time < nextTick)
            {
                return;
            }

            nextTick = Time.time + 1f;
            Bagsys.RogueLike.EnemyStats enemy = GetComponent<Bagsys.RogueLike.EnemyStats>();
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            float ratio = enemy.MaxHP >= 220 ? 0.01f : 0.02f;
            enemy.TakeTrueDamage(Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHP * ratio)), transform.position);
        }
    }

    public static class DebuffExtensions
    {
        public static T AddOrRefreshDebuff<T>(this GameObject obj, float duration) where T : TimedDebuff2D
        {
            T debuff = obj.GetComponent<T>();
            if (debuff == null)
            {
                debuff = obj.AddComponent<T>();
            }

            debuff.Refresh(duration);
            return debuff;
        }
    }

    public class WaterPuddle2D : MonoBehaviour
    {
        public static void Create(Vector3 position)
        {
            GameObject obj = new GameObject("WaterPuddle", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(WaterPuddle2D));
            obj.transform.position = new Vector3(position.x, position.y, 0f);
            obj.transform.localScale = new Vector3(1.6f, 0.72f, 1f);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.LoadSprite("Environment/GroundEffects/WaterPuddle");
            if (Art2DUtility.IsFallbackSprite(renderer.sprite))
            {
                Object.Destroy(obj);
                return;
            }
            renderer.color = Color.white;
            renderer.sortingOrder = -1;
            CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.55f;
            Destroy(obj, 6f);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.CompareTag("Enemy"))
            {
                other.gameObject.AddOrRefreshDebuff<WetDebuff2D>(1.2f);
            }
            else if (other.CompareTag("Player") && ElementalCoreCombatSystem.Instance != null)
            {
                ElementalCoreCombatSystem.Instance.HealPlayerFromWater();
            }
        }
    }

    public class FireOrb2D : MonoBehaviour
    {
        private Transform owner;
        private PlayerVisualHub2D ownerHub;
        private int damage;
        private bool launchAfterHits;
        private bool burn;
        private int hitCount;
        private float angle;
        private bool launched;
        private Transform target;

        public static void Create(Transform owner, int damage, bool launchAfterHits, bool burn)
        {
            GameObject obj = new GameObject("FireOrb", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(FireOrb2D));
            Vector2 center = ResolveOwnerOrbitCenter(owner);
            obj.transform.position = center + Vector2.right * 1.2f;
            obj.transform.localScale = Vector3.one * 0.42f;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = new Color(1f, 0.36f, 0.08f, 1f);
            renderer.sortingOrder = 16;
            CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            FireOrb2D orb = obj.GetComponent<FireOrb2D>();
            orb.owner = owner;
            orb.ownerHub = PlayerVisualHub2D.Get(owner);
            orb.damage = damage;
            orb.launchAfterHits = launchAfterHits;
            orb.burn = burn;
            Destroy(obj, 8f);
        }

        private static Vector2 ResolveOwnerOrbitCenter(Transform owner)
        {
            if (owner == null)
            {
                return Vector2.zero;
            }

            PlayerVisualHub2D hub = PlayerVisualHub2D.Get(owner);
            if (hub != null)
            {
                return hub.ResolveOrbitCenter();
            }

            return PlayerAim2D.ResolveVisualAimOrigin(owner);
        }

        private void Update()
        {
            if (!launched && owner != null)
            {
                if (ownerHub == null)
                {
                    ownerHub = PlayerVisualHub2D.Get(owner);
                }

                angle += Time.deltaTime * 230f;
                Vector2 center = ResolveOwnerOrbitCenter(owner);
                Vector2 offset = Quaternion.Euler(0f, 0f, angle) * Vector2.right * 1.15f;
                transform.position = center + offset;
                return;
            }

            if (target == null)
            {
                target = FindNearestEnemy();
            }

            if (target != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * 9f);
            }
            else
            {
                transform.position += transform.right * (Time.deltaTime * 7f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Enemy"))
            {
                return;
            }

            Bagsys.RogueLike.EnemyStats enemy = other.GetComponentInParent<Bagsys.RogueLike.EnemyStats>();
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            enemy.TakeTrueDamage(damage, transform.position);
            if (burn)
            {
                enemy.gameObject.AddOrRefreshDebuff<BurningDebuff2D>(3f);
            }

            hitCount++;
            if (launched || hitCount >= 4)
            {
                if (launchAfterHits && !launched)
                {
                    launched = true;
                    target = FindNearestEnemy();
                    return;
                }

                Destroy(gameObject);
            }
        }

        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform best = null;
            float bestDistance = 999f;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i] == gameObject)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, enemies[i].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemies[i].transform;
                }
            }

            return best;
        }
    }

    public class FallingGoldStrike2D : MonoBehaviour
    {
        private Bagsys.RogueLike.EnemyStats target;
        private int damage;
        private float resolveTime;
        private SpriteRenderer spriteRenderer;
        private Color color;

        public static void Create(Bagsys.RogueLike.EnemyStats target, int damage)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            GameObject obj = new GameObject("FallingGoldStrike", typeof(SpriteRenderer), typeof(FallingGoldStrike2D));
            obj.transform.position = target.transform.position + Vector3.up * 1.2f;
            obj.transform.localScale = new Vector3(0.9f, 1.8f, 1f);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.LoadSprite("Environment/GroundEffects/GoldDropSpark");
            if (Art2DUtility.IsFallbackSprite(renderer.sprite))
            {
                Object.Destroy(obj);
                return;
            }
            renderer.color = Color.white;
            renderer.sortingOrder = 17;

            FallingGoldStrike2D strike = obj.GetComponent<FallingGoldStrike2D>();
            strike.target = target;
            strike.damage = Mathf.Max(1, damage);
            strike.resolveTime = Time.time + 0.28f;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            color = spriteRenderer != null ? spriteRenderer.color : Color.yellow;
        }

        private void Update()
        {
            transform.position += Vector3.down * (Time.deltaTime * 2.2f);
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(0.62f, 0.95f, Mathf.PingPong(Time.time * 9f, 1f));
                spriteRenderer.color = color;
            }

            if (Time.time < resolveTime)
            {
                return;
            }

            if (target != null && !target.IsDead)
            {
                target.TakeTrueDamage(damage, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
