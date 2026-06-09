using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class PlayerAttack2D : MonoBehaviour
    {
        [SerializeField] private float attackRadius = 1.25f;
        [SerializeField] private float attackForwardOffset = 0.9f;
        [SerializeField] private float attackArcAngle = 95f;

        private float nextAttackTime;
        private bool chargingBow;
        private float bowChargeStartTime;
        private PlayerClassVfx2D classVfx;
        private PlayerController2D controller;

        public float LastAttackTime { get; private set; } = -999f;
        public bool IsChargingBow => chargingBow;
        public float CurrentBowChargeRatio => chargingBow
            ? Mathf.Clamp01((Time.time - bowChargeStartTime) / 2f)
            : 0f;

        private void Awake()
        {
            classVfx = GetComponent<PlayerClassVfx2D>();
            controller = GetComponent<PlayerController2D>();
        }

        private void Update()
        {
            if (classVfx == null)
            {
                classVfx = GetComponent<PlayerClassVfx2D>();
            }

            if (RogueUiPause.BlocksGameplayInput || GameStartMenuPanel.IsVisible
                || (PlayerLifeState2D.Instance != null && PlayerLifeState2D.Instance.IsDeadOrDying))
            {
                chargingBow = false;
                if (classVfx != null)
                {
                    classVfx.CancelBowCharge();
                }

                return;
            }

            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;

            if (heroClass == HeroClass.Warrior)
            {
                TickWarrior(run);
            }
            else if (heroClass == HeroClass.Archer)
            {
                TickArcher();
            }
            else
            {
                TickMage(run);
            }
        }

        private void TickWarrior(RunProgressionSystem run)
        {
            if (GameSettingsService.WasPressed("Block") && run != null)
            {
                run.BeginWarriorShield();
                GameAudioService.Ensure()?.PlayShieldBlock();
                if (classVfx != null)
                {
                    classVfx.ShowWarriorShield();
                }
            }

            if (GameSettingsService.WasPressed("Attack") && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + ResolveAttackCooldown(0.28f);
                LastAttackTime = Time.time;
                GameAudioService.Ensure()?.PlayWarriorSlash();
                PerformMeleeAttack();
            }
        }

        private void TickArcher()
        {
            if (GameSettingsService.WasPressed("Attack"))
            {
                chargingBow = true;
                bowChargeStartTime = Time.time;
                GameAudioService.Ensure()?.PlayBowDraw();
                if (classVfx != null)
                {
                    classVfx.BeginBowCharge();
                }
            }

            if (chargingBow && classVfx != null)
            {
                classVfx.UpdateBowCharge(Mathf.Clamp01((Time.time - bowChargeStartTime) / 2f));
            }

            if (chargingBow && GameSettingsService.WasReleased("Attack"))
            {
                float charge = Mathf.Clamp01((Time.time - bowChargeStartTime) / 2f);
                chargingBow = false;
                if (classVfx != null)
                {
                    classVfx.EndBowCharge(charge);
                }

                if (Time.time < nextAttackTime)
                {
                    return;
                }

                nextAttackTime = Time.time + ResolveAttackCooldown(0.34f);
                LastAttackTime = Time.time;
                GameAudioService.Ensure()?.PlayBowShoot();
                int damage = Mathf.RoundToInt(ResolveAttackDamage() * Mathf.Lerp(0.85f, 2.15f, charge));
                float speed = Mathf.Lerp(10f, 15.5f, charge);
                float lifetime = Mathf.Lerp(1.2f, 2.9f, charge);
                bool piercing = charge >= 0.98f;
                FireProjectile(damage, speed, lifetime, piercing, new Color(1f, 1f, 1f, 1f), piercing ? 1.05f : 0.82f, piercing ? "Projectile_ChargedArrow" : "Projectile_Arrow");
                if (classVfx != null)
                {
                    classVfx.ShowArcherShot(ResolveAimDirection(), piercing, charge);
                }
            }
        }

        private void TickMage(RunProgressionSystem run)
        {
            if (GameSettingsService.WasPressed("Attack") && Time.time >= nextAttackTime)
            {
                if (run != null && !run.TrySpendMana(10f))
                {
                    return;
                }

                nextAttackTime = Time.time + ResolveAttackCooldown(0.55f);
                LastAttackTime = Time.time;
                FireProjectile(Mathf.RoundToInt(ResolveAttackDamage() * 1.28f), 9.5f, 2.2f, false, Color.white, 0.56f, "Projectile_MageOrb");
                if (classVfx != null)
                {
                    classVfx.ShowMageCast(ResolveAimDirection());
                }
            }
        }

        private void PerformMeleeAttack()
        {
            Vector2 origin = ResolveCombatOrigin();
            Vector2 forward = ResolveAimDirection();
            Vector2 center = origin + forward * attackForwardOffset;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius);
            float halfArc = attackArcAngle * 0.5f;
            int damage = ResolveAttackDamage();
            bool hitSomething = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.transform == transform)
                {
                    continue;
                }

                Vector2 toTarget = (Vector2)hit.transform.position - origin;
                if (toTarget.sqrMagnitude <= 0.0001f || Vector2.Angle(forward, toTarget.normalized) > halfArc)
                {
                    continue;
                }

                Bagsys.RogueLike.EnemyStats enemyStats = hit.GetComponentInParent<Bagsys.RogueLike.EnemyStats>();
                if (enemyStats != null)
                {
                    enemyStats.TakeDamage(damage, transform.position);
                    hitSomething = true;
                    continue;
                }

                LoopRewardChest chest = hit.GetComponentInParent<LoopRewardChest>();
                if (chest != null)
                {
                    chest.TakeDamage(damage);
                    hitSomething = true;
                    continue;
                }

                SoulKnightChest soulChest = hit.GetComponentInParent<SoulKnightChest>();
                if (soulChest != null)
                {
                    soulChest.TakeDamage(damage);
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                GameAudioService.Ensure()?.PlayWarriorHit();
            }
        }

        private void FireProjectile(int damage, float speed, float lifetime, bool piercing, Color color, float scale, string spriteName)
        {
            Vector2 forward = ResolveAimDirection();
            Vector2 spawn = ResolveProjectileSpawn(forward);
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            if (heroClass == HeroClass.Archer)
            {
                Vector2 mouseWorld = PlayerAim2D.ResolveMouseWorld(transform);
                Vector2 toMouse = mouseWorld - spawn;
                if (toMouse.sqrMagnitude > 0.0001f)
                {
                    forward = toMouse.normalized;
                }
            }

            GameObject bullet = new GameObject("PlayerProjectile2D", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(PlayerProjectile2D));
            bullet.transform.position = spawn;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = bullet.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.LoadProjectileSprite(spriteName);
            renderer.color = color;
            renderer.sortingOrder = 12;
            bullet.transform.localScale = ResolveProjectileScale(renderer.sprite, scale);

            CircleCollider2D collider = bullet.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;

            Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = forward.normalized * speed;

            bullet.GetComponent<PlayerProjectile2D>().Initialize(damage, lifetime, piercing, heroClass == HeroClass.Archer);
        }

        private Vector2 ResolveProjectileSpawn(Vector2 forward)
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            if (heroClass == HeroClass.Archer)
            {
                if (classVfx == null)
                {
                    classVfx = GetComponent<PlayerClassVfx2D>();
                }

                if (classVfx != null)
                {
                    return classVfx.GetArcherMuzzleWorldPosition(forward);
                }
            }

            return (Vector2)transform.position + forward.normalized * 0.85f + Vector2.up * 0.18f;
        }

        private static Vector3 ResolveProjectileScale(Sprite sprite, float targetLength)
        {
            if (sprite == null)
            {
                return Vector3.one * targetLength;
            }

            float spriteLength = sprite.bounds.size.x;
            if (spriteLength <= 0.001f)
            {
                return Vector3.one * targetLength;
            }

            float uniform = targetLength / spriteLength;
            return new Vector3(uniform, uniform, 1f);
        }

        private Vector2 ResolveAimDirection()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController2D>();
            }

            Vector2 direction = controller != null ? controller.AimDirection : Vector2.right;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            return direction.normalized;
        }

        private Vector2 ResolveCombatOrigin()
        {
            PlayerVisualHub2D hub = PlayerVisualHub2D.Get(transform);
            if (hub != null)
            {
                return hub.BodyCenter;
            }

            if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer renderer))
            {
                return renderer.bounds.center;
            }

            return transform.position;
        }

        private void SpawnSlashFeedback(Vector2 center, Vector2 forward)
        {
            Sprite slashSprite = Art2DUtility.LoadSprite("VFX_Warrior_SlashArc");
            if (slashSprite != Art2DUtility.GetFallbackSprite())
            {
                GameObject slashSpriteObject = new GameObject("MeleeArcSlashSprite2D", typeof(SpriteRenderer), typeof(ClassVfxFade));
                slashSpriteObject.transform.position = center;
                slashSpriteObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
                slashSpriteObject.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                SpriteRenderer spriteRenderer = slashSpriteObject.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = slashSprite;
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.78f);
                spriteRenderer.sortingOrder = 18;
                return;
            }

            GameObject slash = new GameObject("MeleeArcSlash2D", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeleeArcFade));
            slash.transform.position = center;
            slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
            MeshFilter meshFilter = slash.GetComponent<MeshFilter>();
            meshFilter.mesh = BuildArcMesh(attackRadius * 1.15f, attackArcAngle, 18);
            MeshRenderer renderer = slash.GetComponent<MeshRenderer>();
            renderer.material = new UnityEngine.Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(0.55f, 0.9f, 1f, 0.48f);
            renderer.sortingOrder = 18;
        }

        private static Mesh BuildArcMesh(float radius, float angle, int segments)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            float half = angle * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float currentAngle = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0f) * radius;
            }

            for (int i = 0; i < segments; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private int ResolveAttackDamage()
        {
            if (NewInventorySystem.Instance != null)
            {
                ItemData weapon = null;
                ItemSlot weaponSlot = NewInventorySystem.Instance.CurrentWeaponSlot;
                if (weaponSlot != null && !weaponSlot.IsEmpty)
                {
                    weapon = GameDataModel.GetItem(weaponSlot.itemId);
                }

                int metaBonus = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.GetWeaponMetaBonus() : 0;
                int damage = Mathf.Max(1, NewInventorySystem.Instance.PlayerStats.TotalAtk(weapon) + metaBonus);
                PlayerStatBlock stats = NewInventorySystem.Instance.PlayerStats;
                float critRate = Mathf.Clamp(stats.coreCritRateBonus + stats.runCritRateBonus + stats.metaCritRateBonus, 0f, 0.95f);
                if (Random.value < critRate)
                {
                    damage = Mathf.RoundToInt(damage * 1.5f);
                }

                return damage;
            }

            Bagsys.RogueLike.PlayerStats legacyStats = GetComponent<Bagsys.RogueLike.PlayerStats>();
            return legacyStats != null ? Mathf.Max(1, legacyStats.ATK) : 10;
        }

        private float ResolveAttackCooldown(float baseCooldown)
        {
            if (NewInventorySystem.Instance == null)
            {
                return baseCooldown;
            }

            PlayerStatBlock stats = NewInventorySystem.Instance.PlayerStats;
            float speedBonus = Mathf.Clamp(stats.coreAttackSpeedBonus + stats.runAttackSpeedBonus + stats.metaAttackSpeedBonus, 0f, 0.95f);
            if (ElementalCoreCombatSystem.Instance != null)
            {
                speedBonus += ElementalCoreCombatSystem.Instance.TemporaryAttackSpeedBonus;
            }

            return Mathf.Max(0.12f, baseCooldown / (1f + speedBonus));
        }
    }

    public class MeleeArcFade : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private Color color;
        private float age;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            color = meshRenderer != null && meshRenderer.material != null ? meshRenderer.material.color : Color.white;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.12f, age / 0.12f);
            if (meshRenderer != null && meshRenderer.material != null)
            {
                color.a = Mathf.Lerp(0.48f, 0f, age / 0.12f);
                meshRenderer.material.color = color;
            }

            if (age >= 0.12f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class PlayerProjectile2D : MonoBehaviour
    {
        private int damage;
        private bool piercing;
        private bool piercedOnce;
        private bool playArrowHitSfx;

        public void Initialize(int attackDamage, float lifetime)
        {
            Initialize(attackDamage, lifetime, false, false);
        }

        public void Initialize(int attackDamage, float lifetime, bool canPierce)
        {
            Initialize(attackDamage, lifetime, canPierce, false);
        }

        public void Initialize(int attackDamage, float lifetime, bool canPierce, bool archerArrow)
        {
            damage = Mathf.Max(1, attackDamage);
            piercing = canPierce;
            piercedOnce = false;
            playArrowHitSfx = archerArrow;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.CompareTag("Player"))
            {
                return;
            }

            Bagsys.RogueLike.EnemyStats enemyStats = other.GetComponentInParent<Bagsys.RogueLike.EnemyStats>();
            if (enemyStats != null)
            {
                enemyStats.TakeDamage(damage, transform.position);
                if (playArrowHitSfx)
                {
                    GameAudioService.Ensure()?.PlayArrowHit();
                }

                if (piercing && !piercedOnce)
                {
                    piercedOnce = true;
                    damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.5f));
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            SoulKnightChest soulChest = other.GetComponentInParent<SoulKnightChest>();
            if (soulChest != null)
            {
                soulChest.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            LoopRewardChest loopChest = other.GetComponentInParent<LoopRewardChest>();
            if (loopChest != null)
            {
                loopChest.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
