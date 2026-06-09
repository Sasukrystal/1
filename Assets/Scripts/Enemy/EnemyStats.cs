using UnityEngine;
using System.Collections;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class EnemyStats : CharacterStats
    {
        [Header("Hit Reaction")]
        [SerializeField] private float knockbackForce = 2.15f;
        [SerializeField] private float hitFlashDuration = 0.1f;

        private Rigidbody enemyRigidbody;
        private Rigidbody2D enemyRigidbody2D;
        private Renderer[] cachedRenderers;
        private Color[] originalColors;
        private Coroutine flashRoutine;

        protected override void Awake()
        {
            base.Awake();
            enemyRigidbody = GetComponent<Rigidbody>();
            enemyRigidbody2D = GetComponent<Rigidbody2D>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer != null && renderer.material != null)
                {
                    originalColors[i] = renderer.material.color;
                }
                else
                {
                    originalColors[i] = Color.red;
                }
            }
        }

        public void ConfigureBaseStats(int hp, int attack, int defense)
        {
            maxHP = Mathf.Max(1, hp);
            currentHP = maxHP;
            atk = Mathf.Max(1, attack);
            def = Mathf.Max(0, defense);
            isDead = false;
        }

        public override void TakeDamage(int damage)
        {
            TakeDamage(damage, transform.position - transform.forward);
        }

        public void TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (isDead)
            {
                return;
            }

            int incomingDamage = Mathf.Max(1, damage);
            global::ModernRogue.ElementalCoreCombatSystem elemental = global::ModernRogue.ElementalCoreCombatSystem.Instance;
            if (elemental != null)
            {
                incomingDamage = elemental.ModifyOutgoingDamage(this, incomingDamage);
            }

            int defense = DEF - (elemental != null ? elemental.GetWetDefenseReduction(gameObject) : 0);
            int finalDamage = Mathf.Max(1, incomingDamage - Mathf.Max(0, defense));
            ApplyDirectDamage(finalDamage, sourcePosition, true);
            if (elemental != null)
            {
                elemental.AfterPlayerDamagedEnemy(this, finalDamage);
            }

            global::ModernRogue.RunProgressionSystem progression = global::ModernRogue.RunProgressionSystem.Instance;
            if (progression != null)
            {
                progression.OnPlayerDealtDamage(finalDamage);
            }
        }

        public void TakeTrueDamage(int damage, Vector3 sourcePosition)
        {
            if (isDead)
            {
                return;
            }

            ApplyDirectDamage(Mathf.Max(1, damage), sourcePosition, false);
        }

        private void ApplyDirectDamage(int finalDamage, Vector3 sourcePosition, bool elementalFeedback)
        {
            currentHP = Mathf.Max(0, currentHP - Mathf.Max(1, finalDamage));
            global::ModernRogue.DamagePopup2D.Spawn(transform.position, finalDamage);
            if (GetComponent<global::ModernRogue.BossTopHealthBar2D>() != null)
            {
                global::ModernRogue.BossTopHealthBar2D.NotifyBossHealthChanged();
            }

            global::ModernRogue.SKHealthBar2D healthBar = GetComponent<global::ModernRogue.SKHealthBar2D>();
            if (healthBar != null)
            {
                healthBar.ShowForSeconds(3f);
            }

            if (currentHP <= 0)
            {
                Die();
                return;
            }

            global::ModernRogue.RogueActorVisual2D visual = GetComponent<global::ModernRogue.RogueActorVisual2D>();
            if (visual != null)
            {
                visual.PlayHit();
            }

            PlayHitFeedback(sourcePosition);
            if (elementalFeedback)
            {
                Debug.Log($"敌人受到伤害，当前血量: {CurrentHP}/{MaxHP}");
            }
        }

        protected override void Die()
        {
            if (isDead)
            {
                return;
            }

            base.Die();
            if (global::ModernRogue.SoulKnightDirector.Instance != null)
            {
                global::ModernRogue.SoulKnightDirector.Instance.RegisterKill();
                if (global::ModernRogue.NewInventorySystem.Instance != null)
                {
                    int coins = Random.Range(4, 10);
                    if (global::ModernRogue.ElementalCoreCombatSystem.Instance != null)
                    {
                        coins += global::ModernRogue.ElementalCoreCombatSystem.Instance.BonusCoinsOnEnemyDeath();
                    }

                    int exp = Random.Range(3, 7);
                    global::ModernRogue.NewInventorySystem.Instance.EarnCoins(coins);
                    global::ModernRogue.NewInventorySystem.Instance.AddExperience(exp);
                    if (global::ModernRogue.RunProgressionSystem.Instance != null)
                    {
                        global::ModernRogue.RunProgressionSystem.Instance.RestoreMana(4f);
                    }

                    global::ModernRogue.SoulKnightDirector.Instance.ShowPickupTip("击杀奖励：金币 +" + coins + "    经验 +" + exp);
                    if (Random.value > 0.78f)
                    {
                        global::ModernRogue.NewInventorySystem.Instance.AddItem(Random.value > 0.55f ? 1 : 2, 1);
                    }
                }

                Destroy(gameObject);
                return;
            }

            if (global::ModernRogue.SoulKnightDirector.Instance == null)
            {
                CombatVfxUtility.SpawnEnemyDeathBurst(transform.position + Vector3.up * 0.45f);
            }

            if (LootManager.Instance != null)
            {
                LootManager.Instance.SpawnRandomDropBurst(transform.position, 1, 3);
            }
            else
            {
                Debug.LogWarning($"{name}: LootManager.Instance is null, drop skipped.");
            }

            Destroy(gameObject);
        }

        private void PlayHitFeedback(Vector3 sourcePosition)
        {
            if (enemyRigidbody != null)
            {
                Vector3 knockbackDirection = transform.position - sourcePosition;
                knockbackDirection.y = 0f;
                if (knockbackDirection.sqrMagnitude > 0.001f)
                {
                    enemyRigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
                }
            }
            else if (enemyRigidbody2D != null)
            {
                Vector2 knockbackDirection = (Vector2)transform.position - (Vector2)sourcePosition;
                if (knockbackDirection.sqrMagnitude > 0.001f)
                {
                    enemyRigidbody2D.velocity = Vector2.zero;
                    enemyRigidbody2D.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
                }
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(HitFlashRoutine());
            CombatVfxUtility.SpawnHitFlashBurst(transform.position + Vector3.up * 0.45f);
        }

        private IEnumerator HitFlashRoutine()
        {
            ApplyFlashColor(Color.white);
            yield return new WaitForSeconds(hitFlashDuration);
            RestoreOriginalColors();
            flashRoutine = null;
        }

        private void ApplyFlashColor(Color color)
        {
            if (cachedRenderers == null)
            {
                return;
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        private void RestoreOriginalColors()
        {
            if (cachedRenderers == null)
            {
                return;
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = originalColors != null && i < originalColors.Length ? originalColors[i] : Color.red;
                }
            }
        }
    }
}
