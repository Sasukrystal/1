using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public abstract class EnemyLoopAgent : MonoBehaviour
    {
        protected int attack = 5;
        protected float moveSpeed = 3f;
        protected Transform player;
        protected Rigidbody2D body;
        protected Bagsys.RogueLike.EnemyStats stats;
        protected CombatRoomBounds2D roomBounds;
        protected float nextAttackTime;

        public virtual void Configure(int attackValue, float speed)
        {
            attack = attackValue;
            moveSpeed = speed;
        }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stats = GetComponent<Bagsys.RogueLike.EnemyStats>();
            roomBounds = GetComponent<CombatRoomBounds2D>();
        }

        protected void CachePlayer()
        {
            if (player != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        protected Vector2 DirectionToPlayer()
        {
            CachePlayer();
            if (player == null)
            {
                return transform.right;
            }

            Vector2 direction = (Vector2)player.position - body.position;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : (Vector2)transform.right;
        }

        protected void Move(Vector2 direction, float speedMultiplier = 1f)
        {
            if (body == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            if (roomBounds == null)
            {
                roomBounds = GetComponent<CombatRoomBounds2D>();
            }

            float elementalMultiplier = ElementalCoreCombatSystem.Instance != null ? ElementalCoreCombatSystem.Instance.GetEnemyMoveMultiplier(gameObject) : 1f;
            Vector2 nextPosition = body.position + direction.normalized * moveSpeed * speedMultiplier * elementalMultiplier * Time.fixedDeltaTime;
            if (roomBounds != null)
            {
                nextPosition = roomBounds.Clamp(nextPosition);
            }

            body.MovePosition(nextPosition);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.MoveRotation(Mathf.LerpAngle(body.rotation, angle, Time.fixedDeltaTime * 10f));
        }

        protected void DamagePlayer()
        {
            if (player == null)
            {
                return;
            }

            PlayerController2D controller = player.GetComponent<PlayerController2D>();
            if (controller != null && controller.IsDodging)
            {
                return;
            }

            Bagsys.RogueLike.CharacterStats characterStats = player.GetComponent<Bagsys.RogueLike.CharacterStats>();
            if (characterStats != null)
            {
                characterStats.TakeDamage(attack);
            }

            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.TakeDamage(attack);
            }
        }
    }

    public class SlimeAI : EnemyLoopAgent
    {
        private enum SlimeState
        {
            Chase,
            Windup,
            Lunge,
            Recover
        }

        private SlimeState state;
        private Vector2 lockedDirection;
        private float stateEndTime;
        private bool lungeDamageApplied;
        private SpriteRenderer spriteRenderer;
        private Color normalColor;
        private const float AttackRange = 4.45f;
        private const float WindupTime = 0.62f;
        private const float LungeTime = 0.24f;
        private const float RecoverTime = 0.72f;

        protected override void Awake()
        {
            base.Awake();
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalColor = spriteRenderer != null ? spriteRenderer.color : Color.red;
        }

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            Vector2 direction = DirectionToPlayer();
            float distance = player != null ? Vector2.Distance(transform.position, player.position) : 99f;
            if (state == SlimeState.Chase)
            {
                if (distance > AttackRange)
                {
                    Move(direction, 0.58f);
                    return;
                }

                if (Time.time >= nextAttackTime)
                {
                    BeginWindup(direction);
                }

                return;
            }

            if (state == SlimeState.Windup)
            {
                FaceDirection(lockedDirection);
                if (Time.time >= stateEndTime)
                {
                    state = SlimeState.Lunge;
                    stateEndTime = Time.time + LungeTime;
                    lungeDamageApplied = false;
                }

                return;
            }

            if (state == SlimeState.Lunge)
            {
                Move(lockedDirection, 2.65f);
                TryLungeHit();
                if (Time.time >= stateEndTime)
                {
                    state = SlimeState.Recover;
                    stateEndTime = Time.time + RecoverTime;
                    nextAttackTime = Time.time + 0.95f;
                    RestoreColor();
                }

                return;
            }

            if (state == SlimeState.Recover && Time.time >= stateEndTime)
            {
                state = SlimeState.Chase;
            }
        }

        private void BeginWindup(Vector2 direction)
        {
            lockedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : (Vector2)transform.right;
            state = SlimeState.Windup;
            stateEndTime = Time.time + WindupTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.55f, 0.35f, 1f);
            }

            RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
            if (visual != null)
            {
                visual.PlayAction(WindupTime + LungeTime);
            }

            SpawnAttackTell(lockedDirection);
        }

        private void TryLungeHit()
        {
            if (lungeDamageApplied || player == null)
            {
                return;
            }

            Vector2 hitCenter = (Vector2)transform.position + lockedDirection * 0.95f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, 0.78f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Player"))
                {
                    continue;
                }

                lungeDamageApplied = true;
                DamagePlayer();
                return;
            }
        }

        private void SpawnAttackTell(Vector2 direction)
        {
        }

        private void FaceDirection(Vector2 direction)
        {
            if (body == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.MoveRotation(Mathf.LerpAngle(body.rotation, angle, Time.fixedDeltaTime * 14f));
        }

        private void RestoreColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }

    public class SlimeTellFade : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color color;
        private float age;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            color = spriteRenderer != null ? spriteRenderer.color : Color.red;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(0.34f, 0f, age / 0.42f);
                spriteRenderer.color = color;
            }

            if (age >= 0.42f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class SkeletonArcherAI : EnemyLoopAgent
    {
        private enum ArcherState
        {
            Reposition,
            Aim,
            Recover
        }

        private ArcherState state;
        private Vector2 aimDirection;
        private float stateEndTime;
        private const float IdealMinRange = 5.2f;
        private const float IdealMaxRange = 7.4f;
        private const float AimTime = 0.58f;
        private const float RecoverTime = 1.15f;

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            Vector2 direction = DirectionToPlayer();
            float distance = player != null ? Vector2.Distance(transform.position, player.position) : 99f;
            if (state == ArcherState.Reposition)
            {
                if (distance < IdealMinRange)
                {
                    Move(-direction, 0.62f);
                }
                else if (distance > IdealMaxRange)
                {
                    Move(direction, 0.54f);
                }
                else
                {
                    FaceDirection(direction);
                }

                if (Time.time >= nextAttackTime && distance <= IdealMaxRange + 1.4f)
                {
                    BeginAim(direction);
                }

                return;
            }

            if (state == ArcherState.Aim)
            {
                FaceDirection(aimDirection);
                if (Time.time >= stateEndTime)
                {
                    Shoot(aimDirection);
                    state = ArcherState.Recover;
                    stateEndTime = Time.time + RecoverTime;
                    nextAttackTime = Time.time + 1.85f;
                }

                return;
            }

            if (state == ArcherState.Recover && Time.time >= stateEndTime)
            {
                state = ArcherState.Reposition;
            }
        }

        private void BeginAim(Vector2 direction)
        {
            aimDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : (Vector2)transform.right;
            state = ArcherState.Aim;
            stateEndTime = Time.time + AimTime;
            RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
            if (visual != null)
            {
                visual.PlayAction(AimTime + 0.35f);
            }

            SpawnAimTell(aimDirection);
        }

        private void FaceDirection(Vector2 direction)
        {
            if (body == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.MoveRotation(Mathf.LerpAngle(body.rotation, angle, Time.fixedDeltaTime * 12f));
        }

        private void SpawnAimTell(Vector2 direction)
        {
        }

        private void Shoot(Vector2 direction)
        {
            GameObject bullet = new GameObject("SkeletonArrowBullet2D", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(LoopEnemyBullet));
            bullet.transform.position = transform.position + (Vector3)(direction * 0.7f);
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = bullet.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.LoadProjectileSprite("Projectile_SkeletonArrow");
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            bullet.transform.localScale = Vector3.one * 0.42f;

            GameObject glow = new GameObject("ArrowGlow", typeof(SpriteRenderer));
            glow.transform.SetParent(bullet.transform, false);
            glow.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
            SpriteRenderer glowRenderer = glow.GetComponent<SpriteRenderer>();
            glowRenderer.sprite = Art2DUtility.GetCircleSprite();
            glowRenderer.color = new Color(1f, 0.55f, 0.55f, 0.32f);
            glowRenderer.sortingOrder = 9;
            glow.transform.localScale = new Vector3(0.55f, 0.22f, 1f);

            BoxCollider2D collider = bullet.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.55f, 0.18f);

            Rigidbody2D rigidbody = bullet.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.velocity = direction.normalized * 9.5f;
            bullet.GetComponent<LoopEnemyBullet>().Initialize(attack);
        }
    }

    public static class BossUiUtility
    {
        public static Canvas ResolveScreenCanvas()
        {
#if UNITY_2023_1_OR_NEWER
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
#endif
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvases[i];
                }
            }

            GameObject canvasObject = new GameObject("BossScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }

    public class FloorBossAI : EnemyLoopAgent
    {
        private bool phaseTwo;
        private bool slamWindup;
        private Vector2 slamPoint;
        private float slamResolveTime;
        private float nextShardTime;

        protected override void Awake()
        {
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            float hpRatio = stats != null && stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!phaseTwo && hpRatio <= 0.5f)
            {
                phaseTwo = true;
                moveSpeed *= 1.12f;
                transform.localScale *= 1.16f;
            }

            if (slamWindup)
            {
                if (Time.time >= slamResolveTime)
                {
                    ResolveSlam();
                }

                return;
            }

            Vector2 direction = DirectionToPlayer();
            float distance = player != null ? Vector2.Distance(transform.position, player.position) : 99f;
            if (distance > 4.2f)
            {
                Move(direction, phaseTwo ? 0.74f : 0.62f);
            }
            else if (distance < 2.6f)
            {
                Move(-direction, 0.38f);
            }

            float cooldown = phaseTwo ? 1.65f : 2.25f;
            if (player != null && distance <= 6.4f && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + cooldown;
                BeginSlam();
            }

            if (phaseTwo && Time.time >= nextShardTime)
            {
                nextShardTime = Time.time + 2.9f;
                GameAudioService.Ensure()?.PlayBossTitanShardBurst();
                ThrowRockRing();
            }
        }

        private void BeginSlam()
        {
            slamWindup = true;
            GameAudioService.Ensure()?.PlayBossTitanSlamWindup();
            RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
            if (visual != null)
            {
                visual.PlayAction(0.95f);
            }

            CachePlayer();
            slamPoint = player != null ? (Vector2)player.position : (Vector2)transform.position;
            slamResolveTime = Time.time + 0.68f;
            BossDelayedCircle2D.Create(slamPoint, phaseTwo ? 3.15f : 2.35f, attack + 8, 0.68f, new Color(0.45f, 0.32f, 0.18f, 0.38f));
            BossWarningLine2D.Create(transform.position, (slamPoint - (Vector2)transform.position).normalized, Vector2.Distance(transform.position, slamPoint), 0.32f, new Color(0.7f, 0.45f, 0.2f, 0.42f), 0.65f);
        }

        private void ResolveSlam()
        {
            slamWindup = false;
            GameAudioService.Ensure()?.PlayBossTitanSlam();
            GameAudioService.Ensure()?.PlayBossTitanShake();
            if (body != null)
            {
                body.MovePosition(roomBounds != null ? roomBounds.Clamp(slamPoint) : slamPoint);
            }
            else
            {
                transform.position = slamPoint;
            }

            StompKnockback(phaseTwo ? 4.8f : 3.8f);
        }

        private void StompKnockback(float radius)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Player"))
                {
                    continue;
                }

                Rigidbody2D targetBody = hits[i].GetComponentInParent<Rigidbody2D>();
                if (targetBody != null)
                {
                    Vector2 away = ((Vector2)hits[i].transform.position - (Vector2)transform.position).normalized;
                    targetBody.AddForce(away * 8.5f, ForceMode2D.Impulse);
                }
            }
        }

        private void ThrowRockRing()
        {
            Sprite shardSprite = Art2DUtility.LoadBossVfxSprite("VFX_Boss_RockShard");
            if (shardSprite == null)
            {
                return;
            }

            int count = 10;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                GameObject bullet = new GameObject("TitanRockShard", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(LoopEnemyBullet));
                bullet.transform.position = transform.position + (Vector3)(direction * 0.9f);
                bullet.transform.localScale = Vector3.one * 0.34f;
                SpriteRenderer renderer = bullet.GetComponent<SpriteRenderer>();
                renderer.sprite = shardSprite;
                renderer.color = Color.white;
                renderer.sortingOrder = 12;
                CircleCollider2D collider = bullet.GetComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.38f;
                Rigidbody2D rigidbody = bullet.GetComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.velocity = direction * 6.4f;
                bullet.GetComponent<LoopEnemyBullet>().Initialize(Mathf.Max(1, attack - 3));
            }
        }
    }

    public class EmberMageBossAI : EnemyLoopAgent
    {
        private bool phaseTwo;
        private float nextFanTime;
        private float nextRuneTime;
        private static Sprite emberBoltSprite;
        private static Sprite flameRuneSprite;

        protected override void Awake()
        {
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            CachePlayer();
            if (player == null)
            {
                return;
            }

            Vector2 direction = DirectionToPlayer();
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance < 5.2f)
            {
                Move(-direction, 0.42f);
            }
            else if (distance > 7.5f)
            {
                Move(direction, 0.36f);
            }
        }

        private void Update()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!phaseTwo && hpRatio <= 0.5f)
            {
                phaseTwo = true;
                transform.localScale *= 1.16f;
            }

            if (Time.time >= nextFanTime)
            {
                nextFanTime = Time.time + (phaseTwo ? 1.25f : 1.75f);
                Vector2 direction = DirectionToPlayer();
                GameAudioService.Ensure()?.PlayBossEmberFanWindup();
                RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
                if (visual != null)
                {
                    visual.PlayAction(0.85f);
                }

                BossWarningLine2D.Create(transform.position, direction, 4.8f, 0.22f, new Color(1f, 0.42f, 0.08f, 0.42f), 0.48f);
                Invoke(nameof(FireFan), 0.48f);
            }

            if (Time.time >= nextRuneTime)
            {
                nextRuneTime = Time.time + (phaseTwo ? 2.35f : 3.35f);
                DropFlameRunes();
            }
        }

        private void FireFan()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            GameAudioService.Ensure()?.PlayBossEmberFanCast();
            Vector2 baseDirection = DirectionToPlayer();
            int count = phaseTwo ? 7 : 5;
            float spread = phaseTwo ? 68f : 48f;
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;
                SpawnBossProjectile("EmberBolt", direction, new Color(1f, 0.36f, 0.08f), phaseTwo ? 8.2f : 7.1f, attack);
            }
        }

        private void DropFlameRunes()
        {
            CachePlayer();
            if (player == null)
            {
                return;
            }

            GameAudioService.Ensure()?.PlayBossEmberRuneDrop();
            int count = phaseTwo ? 5 : 3;
            Sprite runeSprite = ResolveFlameRuneSprite();
            for (int i = 0; i < count; i++)
            {
                Vector2 center = (Vector2)player.position + Random.insideUnitCircle * 2.6f;
                float runeRadius = phaseTwo ? 0.72f : 0.58f;
                BossDelayedCircle2D.Create(
                    center,
                    runeRadius,
                    attack + 5,
                    0.72f,
                    new Color(1f, 0.92f, 0.82f, 0.88f),
                    runeSprite);
            }
        }

        private static Sprite ResolveEmberBoltSprite()
        {
            if (emberBoltSprite != null)
            {
                return emberBoltSprite;
            }

            Sprite orb = Art2DUtility.LoadProjectileSprite("Projectile_MageOrb");
            if (orb != null && !Art2DUtility.IsFallbackSprite(orb))
            {
                emberBoltSprite = orb;
                return emberBoltSprite;
            }

            orb = Art2DUtility.LoadBossVfxSprite("EnhancedBossBullet");
            emberBoltSprite = orb;
            return emberBoltSprite;
        }

        private static Sprite ResolveFlameRuneSprite()
        {
            if (flameRuneSprite != null)
            {
                return flameRuneSprite;
            }

            flameRuneSprite = Art2DUtility.LoadBossVfxSprite("VFX_Boss_FireRune");
            return flameRuneSprite;
        }

        private void SpawnBossProjectile(string name, Vector2 direction, Color color, float speed, int damage)
        {
            Sprite sprite = ResolveEmberBoltSprite();
            if (sprite == null)
            {
                return;
            }

            GameObject bullet = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(LoopEnemyBullet));
            bullet.transform.position = transform.position + (Vector3)(direction.normalized * 0.95f);
            bullet.transform.localScale = Vector3.one * 0.24f;
            SpriteRenderer renderer = bullet.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 12;
            CircleCollider2D collider = bullet.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.26f;
            Rigidbody2D rigidbody = bullet.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.velocity = direction.normalized * speed;
            bullet.GetComponent<LoopEnemyBullet>().Initialize(damage);
        }
    }

    public class StormGuardBossAI : EnemyLoopAgent
    {
        private bool phaseTwo;
        private bool dashing;
        private Vector2 dashDirection;
        private float dashEndTime;
        private float nextDashTime;
        private float nextStrikeTime;
        private bool dashImpactPlayed;

        protected override void Awake()
        {
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            if (dashing)
            {
                if (!dashImpactPlayed)
                {
                    dashImpactPlayed = true;
                    GameAudioService.Ensure()?.PlayBossStormDashImpact();
                }

                Move(dashDirection, phaseTwo ? 3.7f : 3.05f);
                if (Time.time >= dashEndTime)
                {
                    dashing = false;
                    dashImpactPlayed = false;
                    GameAudioService.Ensure()?.PlayBossStormSlash();
                    nextDashTime = Time.time + (phaseTwo ? 1.55f : 2.15f);
                }

                return;
            }

            CachePlayer();
            if (player != null)
            {
                Vector2 direction = DirectionToPlayer();
                float distance = Vector2.Distance(transform.position, player.position);
                if (distance > 4.6f)
                {
                    Move(direction, 0.58f);
                }
            }
        }

        private void Update()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!phaseTwo && hpRatio <= 0.5f)
            {
                phaseTwo = true;
                transform.localScale *= 1.12f;
            }

            if (!dashing && Time.time >= nextDashTime)
            {
                Vector2 direction = DirectionToPlayer();
                GameAudioService.Ensure()?.PlayBossStormCharge();
                RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
                if (visual != null)
                {
                    visual.PlayAction(0.65f);
                }

                BossWarningLine2D.Create(transform.position, direction, 6.2f, 0.36f, new Color(0.25f, 0.8f, 1f, 0.4f), 0.42f);
                dashDirection = direction;
                dashing = true;
                dashImpactPlayed = false;
                dashEndTime = Time.time + 0.32f;
            }

            if (Time.time >= nextStrikeTime)
            {
                nextStrikeTime = Time.time + (phaseTwo ? 1.9f : 2.65f);
                RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
                if (visual != null)
                {
                    visual.PlayAction(0.8f);
                }

                CastLightningCross();
            }
        }

        private void CastLightningCross()
        {
            CachePlayer();
            if (player == null)
            {
                return;
            }

            GameAudioService.Ensure()?.PlayBossStormLightningHit();
            Vector2 center = player.position;
            BossLightningLine2D.SpawnTelegraphAnchor(center);
            BossLightningLine2D.Create(center, true, attack + 4, 0.7f);
            BossLightningLine2D.Create(center, false, attack + 4, 0.7f);
            if (phaseTwo)
            {
                Vector2 offset = Random.insideUnitCircle * 2.2f;
                BossLightningLine2D.Create(center + offset, true, attack + 2, 0.82f);
                BossLightningLine2D.Create(center - offset, false, attack + 2, 0.82f);
            }
        }
    }

    public class BroodQueenBossAI : EnemyLoopAgent
    {
        private bool phaseTwo;
        private float nextSummonTime;
        private float nextPoolTime;

        protected override void Awake()
        {
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (stats != null && stats.IsDead)
            {
                return;
            }

            CachePlayer();
            if (player == null)
            {
                return;
            }

            Vector2 direction = DirectionToPlayer();
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > 3.8f)
            {
                Move(direction, 0.44f);
            }
            else if (distance < 2.2f)
            {
                Move(-direction, 0.34f);
            }
        }

        private void Update()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!phaseTwo && hpRatio <= 0.5f)
            {
                phaseTwo = true;
                transform.localScale *= 1.18f;
            }

            if (Time.time >= nextSummonTime)
            {
                nextSummonTime = Time.time + (phaseTwo ? 3.2f : 4.4f);
                GameAudioService.Ensure()?.PlayBossBroodSummon();
                RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
                if (visual != null)
                {
                    visual.PlayAction(0.85f);
                }

                SummonBroodlings();
            }

            if (Time.time >= nextPoolTime)
            {
                nextPoolTime = Time.time + (phaseTwo ? 2.2f : 3.1f);
                GameAudioService.Ensure()?.PlayBossBroodPoisonDrop();
                RogueActorVisual2D visual = GetComponent<RogueActorVisual2D>();
                if (visual != null)
                {
                    visual.PlayAction(0.75f);
                }

                SpawnPoisonPool();
            }
        }

        private void SummonBroodlings()
        {
            int count = phaseTwo ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                Vector2 position = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(1.15f, 2.15f);
                Bagsys.RogueLike.EnemyStats enemy = SoulKnightEnemyFactory.SpawnEnemy(transform.parent, position, false, 1);
                enemy.gameObject.name = "幼虫仆从";
                enemy.transform.localScale = Vector3.one * 0.76f;
                CombatRoomBounds2D bounds = enemy.gameObject.AddComponent<CombatRoomBounds2D>();
                bounds.Initialize(transform.position, new Vector2(13.2f, 8.4f));
            }

            GameAudioService.Ensure()?.PlayBossBroodHatch();
        }

        private void SpawnPoisonPool()
        {
            CachePlayer();
            if (player == null)
            {
                return;
            }

            Vector2 center = (Vector2)player.position + Random.insideUnitCircle * 1.8f;
            BossPoisonPool2D.Create(center, phaseTwo ? 1.55f : 1.25f, attack, 0.58f, 4.2f);
        }
    }

    public class BossTopHealthBar2D : MonoBehaviour
    {
        private sealed class BossEntry
        {
            public BossTopHealthBar2D owner;
            public Bagsys.RogueLike.CharacterStats stats;
            public string displayName;
            public Color barColor;
        }

        private static readonly System.Collections.Generic.List<BossEntry> ActiveBosses = new System.Collections.Generic.List<BossEntry>();
        private static GameObject sharedRoot;
        private static RectTransform sharedFillRect;
        private static Image sharedFill;
        private static Text sharedLabel;
        private static float displayedRatio = 1f;
        private static bool forceImmediateRefresh;

        private Bagsys.RogueLike.CharacterStats stats;
        private string bossName;
        private Color color;

        public void Initialize(string displayName, Color barColor)
        {
            bossName = displayName;
            color = barColor;
            stats = GetComponent<Bagsys.RogueLike.CharacterStats>();

            SKHealthBar2D floatingBar = GetComponent<SKHealthBar2D>();
            if (floatingBar != null)
            {
                Destroy(floatingBar);
            }

            Transform floatingRoot = transform.Find("SKHealthBar2D");
            if (floatingRoot != null)
            {
                Destroy(floatingRoot.gameObject);
            }

            Register();
        }

        private void OnDestroy()
        {
            Unregister(this);
        }

        private void Update()
        {
            RefreshSharedBar(false);
        }

        public static void NotifyBossHealthChanged()
        {
            forceImmediateRefresh = true;
            RefreshSharedBar(true);
        }

        public static void DestroySharedBar()
        {
            ActiveBosses.Clear();
            if (sharedRoot != null)
            {
                Destroy(sharedRoot);
                sharedRoot = null;
                sharedFillRect = null;
                sharedFill = null;
                sharedLabel = null;
            }

            displayedRatio = 1f;
            forceImmediateRefresh = false;
        }

        private void Register()
        {
            PurgeDeadEntries();
            for (int i = 0; i < ActiveBosses.Count; i++)
            {
                if (ActiveBosses[i].owner == this)
                {
                    ActiveBosses[i].stats = stats;
                    ActiveBosses[i].displayName = bossName;
                    ActiveBosses[i].barColor = color;
                    EnsureSharedUi();
                    RefreshSharedBar(true);
                    return;
                }
            }

            ActiveBosses.Add(new BossEntry
            {
                owner = this,
                stats = stats,
                displayName = bossName,
                barColor = color
            });
            EnsureSharedUi();
            RefreshSharedBar(true);
        }

        private static void Unregister(BossTopHealthBar2D owner)
        {
            for (int i = ActiveBosses.Count - 1; i >= 0; i--)
            {
                if (ActiveBosses[i].owner == owner)
                {
                    ActiveBosses.RemoveAt(i);
                }
            }

            if (ActiveBosses.Count == 0)
            {
                DestroySharedBar();
                return;
            }

            RefreshSharedBar(true);
        }

        private static void PurgeDeadEntries()
        {
            for (int i = ActiveBosses.Count - 1; i >= 0; i--)
            {
                BossEntry entry = ActiveBosses[i];
                if (entry.owner == null || entry.stats == null || entry.stats.IsDead)
                {
                    ActiveBosses.RemoveAt(i);
                }
            }
        }

        private static void RefreshSharedBar(bool fromDamageEvent)
        {
            if (sharedFillRect == null)
            {
                return;
            }

            PurgeDeadEntries();
            if (ActiveBosses.Count == 0)
            {
                DestroySharedBar();
                return;
            }

            int totalCurrent = 0;
            int totalMax = 0;
            for (int i = 0; i < ActiveBosses.Count; i++)
            {
                BossEntry entry = ActiveBosses[i];
                if (entry.owner != null)
                {
                    entry.stats = entry.owner.GetComponent<Bagsys.RogueLike.CharacterStats>();
                }

                Bagsys.RogueLike.CharacterStats entryStats = entry.stats;
                if (entryStats == null || entryStats.IsDead)
                {
                    continue;
                }

                totalCurrent += entryStats.CurrentHP;
                totalMax += entryStats.MaxHP;
            }

            float targetRatio = totalMax > 0 ? totalCurrent / (float)totalMax : 0f;
            if (forceImmediateRefresh || fromDamageEvent)
            {
                displayedRatio = targetRatio;
                forceImmediateRefresh = false;
            }
            else
            {
                displayedRatio = Mathf.Lerp(displayedRatio, targetRatio, Time.deltaTime * 12f);
            }

            ApplyFillRatio(displayedRatio);
            RefreshSharedLabel(totalCurrent, totalMax);
            sharedRoot.SetActive(totalMax > 0 && totalCurrent > 0);
        }

        private static void ApplyFillRatio(float ratio)
        {
            if (sharedFillRect == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(ratio);
            sharedFillRect.anchorMin = new Vector2(0f, 0f);
            sharedFillRect.anchorMax = new Vector2(clamped, 1f);
            sharedFillRect.offsetMin = Vector2.zero;
            sharedFillRect.offsetMax = Vector2.zero;
        }

        private static void RefreshSharedLabel(int totalCurrent, int totalMax)
        {
            if (sharedLabel == null)
            {
                return;
            }

            if (ActiveBosses.Count == 0)
            {
                sharedLabel.text = string.Empty;
                return;
            }

            if (ActiveBosses.Count == 1)
            {
                sharedLabel.text = ActiveBosses[0].displayName + "    " + totalCurrent + " / " + totalMax;
                if (sharedFill != null)
                {
                    sharedFill.color = ActiveBosses[0].barColor;
                }

                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < ActiveBosses.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" · ");
                }

                builder.Append(ActiveBosses[i].displayName);
            }

            builder.Append("    ");
            builder.Append(totalCurrent);
            builder.Append(" / ");
            builder.Append(totalMax);
            sharedLabel.text = builder.ToString();
            if (sharedFill != null)
            {
                sharedFill.color = new Color(0.15f, 0.42f, 1f, 1f);
            }
        }

        private static void EnsureSharedUi()
        {
            if (sharedRoot != null)
            {
                sharedRoot.SetActive(true);
                return;
            }

            Canvas canvas = BossUiUtility.ResolveScreenCanvas();
            if (canvas == null)
            {
                return;
            }

            GameObject old = GameObject.Find("BossTopHealthBar");
            if (old != null)
            {
                Destroy(old);
            }

            sharedRoot = new GameObject("BossTopHealthBar", typeof(RectTransform), typeof(Image));
            sharedRoot.transform.SetParent(canvas.transform, false);
            RectTransform rect = sharedRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(760f, 34f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            sharedRoot.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.92f);
            RuntimeUIVisuals.StyleImage(sharedRoot.GetComponent<Image>(), new Color(0.02f, 0.02f, 0.03f, 0.92f));

            GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(sharedRoot.transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = new Vector2(4f, 4f);
            trackRect.offsetMax = new Vector2(-4f, -4f);
            RuntimeUIVisuals.StyleImage(trackObject.GetComponent<Image>(), new Color(0.08f, 0.09f, 0.12f, 1f));

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            sharedFillRect = fillObject.GetComponent<RectTransform>();
            sharedFillRect.anchorMin = Vector2.zero;
            sharedFillRect.anchorMax = Vector2.one;
            sharedFillRect.pivot = new Vector2(0f, 0.5f);
            sharedFillRect.offsetMin = Vector2.zero;
            sharedFillRect.offsetMax = Vector2.zero;
            sharedFill = fillObject.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(sharedFill, new Color(0.15f, 0.42f, 1f, 1f));
            ApplyFillRatio(displayedRatio);

            sharedLabel = new GameObject("Name", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            sharedLabel.transform.SetParent(sharedRoot.transform, false);
            sharedLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            sharedLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            sharedLabel.rectTransform.offsetMin = new Vector2(8f, 0f);
            sharedLabel.rectTransform.offsetMax = new Vector2(-8f, 0f);
            sharedLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sharedLabel.fontSize = 14;
            sharedLabel.fontStyle = FontStyle.Bold;
            sharedLabel.alignment = TextAnchor.MiddleCenter;
            sharedLabel.color = new Color(1f, 1f, 1f, 0.98f);
            sharedLabel.raycastTarget = false;
            Outline outline = sharedLabel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    public class BossWarningLine2D : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color color;
        private float life;
        private float age;

        public static void Create(Vector2 origin, Vector2 direction, float length, float width, Color color, float lifetime)
        {
            GameObject obj = new GameObject("BossWarningLine", typeof(SpriteRenderer), typeof(BossWarningLine2D));
            obj.transform.position = origin + direction.normalized * (length * 0.5f);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            obj.transform.localScale = new Vector3(length, width, 1f);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 14;
            BossWarningLine2D warning = obj.GetComponent<BossWarningLine2D>();
            warning.life = lifetime;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            color = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(color.a, 0f, age / Mathf.Max(0.01f, life));
                spriteRenderer.color = color;
            }

            if (age >= life)
            {
                Destroy(gameObject);
            }
        }
    }

    public class BossDelayedCircle2D : MonoBehaviour
    {
        private float radius;
        private int damage;
        private float delay;
        private float age;
        private SpriteRenderer spriteRenderer;
        private Color color;

        public static void Create(Vector2 center, float radius, int damage, float delay, Color color)
        {
            Create(center, radius, damage, delay, color, null);
        }

        public static void Create(Vector2 center, float radius, int damage, float delay, Color color, Sprite sprite)
        {
            GameObject obj = new GameObject("BossDelayedCircle", typeof(SpriteRenderer), typeof(BossDelayedCircle2D));
            obj.transform.position = center;
            obj.transform.localScale = Vector3.one * radius * 2f;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = sprite != null ? 14 : 13;
            BossDelayedCircle2D circle = obj.GetComponent<BossDelayedCircle2D>();
            circle.radius = radius;
            circle.damage = damage;
            circle.delay = delay;
            circle.usesArtSprite = sprite != null;
        }

        private bool usesArtSprite;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            color = spriteRenderer != null ? spriteRenderer.color : Color.red;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float pulse = usesArtSprite
                    ? Mathf.Lerp(0.86f, 1.06f, Mathf.PingPong(age * 4.5f, 1f))
                    : Mathf.Lerp(0.72f, 1.08f, Mathf.PingPong(age * 5f, 1f));
                transform.localScale = Vector3.one * radius * 2f * pulse;
                float targetAlpha = usesArtSprite ? 0.72f : 0.34f;
                color.a = Mathf.Lerp(targetAlpha, usesArtSprite ? 0.95f : 0.56f, Mathf.Clamp01(age / Mathf.Max(0.01f, delay)));
                spriteRenderer.color = color;
            }

            if (age >= delay)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (usesArtSprite)
            {
                GameAudioService.Ensure()?.PlayBossEmberRuneExplosion();
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Player"))
                {
                    continue;
                }

                PlayerController2D controller = hits[i].GetComponentInParent<PlayerController2D>();
                if (controller != null && controller.IsDodging)
                {
                    continue;
                }

                Bagsys.RogueLike.CharacterStats characterStats = hits[i].GetComponentInParent<Bagsys.RogueLike.CharacterStats>();
                if (characterStats != null)
                {
                    characterStats.TakeDamage(damage);
                }

                if (NewInventorySystem.Instance != null)
                {
                    NewInventorySystem.Instance.TakeDamage(damage);
                }
            }

            Destroy(gameObject);
        }
    }

    public class BossLightningLine2D : MonoBehaviour
    {
        private bool vertical;
        private int damage;
        private float delay;
        private float age;
        private SpriteRenderer mainRenderer;
        private SpriteRenderer glowRenderer;
        private SpriteRenderer sparkRenderer;
        private Color mainColor;
        private Color glowColor;
        private Color sparkColor;
        private static Sprite lightningSprite;

        public static void Create(Vector2 center, bool vertical, int damage, float delay)
        {
            GameObject root = new GameObject(vertical ? "VerticalLightningTell" : "HorizontalLightningTell", typeof(BossLightningLine2D));
            root.transform.position = center;
            BossLightningLine2D line = root.GetComponent<BossLightningLine2D>();
            line.vertical = vertical;
            line.damage = damage;
            line.delay = delay;

            Sprite boltSprite = ResolveLightningSprite() ?? Art2DUtility.GetCircleSprite();
            Vector3 mainScale = vertical ? new Vector3(0.36f, 9.2f, 1f) : new Vector3(13.5f, 0.36f, 1f);
            Vector3 glowScale = vertical ? new Vector3(0.82f, 10.2f, 1f) : new Vector3(14.8f, 0.82f, 1f);
            line.glowRenderer = CreateChildSprite(root.transform, "BoltGlow", Art2DUtility.GetCircleSprite(), new Color(0.28f, 0.68f, 1f, 0.16f), 12, glowScale);
            line.mainRenderer = CreateChildSprite(root.transform, "BoltMain", boltSprite, new Color(0.55f, 0.94f, 1f, 0.74f), 14, mainScale);
            line.sparkRenderer = CreateChildSprite(root.transform, "BoltSpark", boltSprite, new Color(0.88f, 0.98f, 1f, 0.62f), 15, Vector3.one * 0.52f);
            line.mainColor = line.mainRenderer.color;
            line.glowColor = line.glowRenderer.color;
            line.sparkColor = line.sparkRenderer.color;
        }

        public static void SpawnTelegraphAnchor(Vector2 center)
        {
            GameObject anchor = new GameObject("LightningCrossAnchor", typeof(SpriteRenderer), typeof(BossLightningBurst2D));
            anchor.transform.position = center;
            anchor.transform.localScale = Vector3.one * 0.48f;
            SpriteRenderer renderer = anchor.GetComponent<SpriteRenderer>();
            Sprite boltSprite = ResolveLightningSprite();
            renderer.sprite = boltSprite != null ? boltSprite : Art2DUtility.GetCircleSprite();
            renderer.color = new Color(0.72f, 0.94f, 1f, 0.48f);
            renderer.sortingOrder = 13;
            anchor.GetComponent<BossLightningBurst2D>().Initialize(0.62f, 1.8f);
        }

        private static SpriteRenderer CreateChildSprite(Transform parent, string name, Sprite sprite, Color color, int order, Vector3 scale)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = scale;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite ResolveLightningSprite()
        {
            if (lightningSprite != null)
            {
                return lightningSprite;
            }

            lightningSprite = Art2DUtility.LoadBossVfxSprite("VFX_Boss_LightningLine");
            return lightningSprite;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float flicker = Mathf.Lerp(0.45f, 1f, Mathf.PingPong(age * 11f, 1f));
            if (mainRenderer != null)
            {
                mainColor.a = Mathf.Lerp(0.4f, 0.9f, flicker);
                mainRenderer.color = mainColor;
                mainRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 36f) * (vertical ? 2.2f : 1.4f));
            }

            if (glowRenderer != null)
            {
                glowColor.a = Mathf.Lerp(0.12f, 0.3f, flicker);
                glowRenderer.color = glowColor;
            }

            if (sparkRenderer != null)
            {
                sparkColor.a = Mathf.Lerp(0.35f, 0.82f, flicker);
                sparkRenderer.color = sparkColor;
                float pulse = 0.72f + Mathf.PingPong(age * 9f, 1f) * 0.38f;
                sparkRenderer.transform.localScale = Vector3.one * pulse;
            }

            if (age >= delay)
            {
                Strike();
            }
        }

        private void Strike()
        {
            Vector2 size = vertical ? new Vector2(0.58f, 9.2f) : new Vector2(13.8f, 0.58f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Player"))
                {
                    continue;
                }

                PlayerController2D controller = hits[i].GetComponentInParent<PlayerController2D>();
                if (controller != null && controller.IsDodging)
                {
                    continue;
                }

                Bagsys.RogueLike.CharacterStats characterStats = hits[i].GetComponentInParent<Bagsys.RogueLike.CharacterStats>();
                if (characterStats != null)
                {
                    characterStats.TakeDamage(damage);
                }

                if (NewInventorySystem.Instance != null)
                {
                    NewInventorySystem.Instance.TakeDamage(damage);
                }
            }

            SpawnStrikeBurst(transform.position, vertical);
            Destroy(gameObject);
        }

        private static void SpawnStrikeBurst(Vector2 center, bool primary)
        {
            if (!primary)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                GameObject ring = new GameObject("LightningBurst", typeof(SpriteRenderer), typeof(BossLightningBurst2D));
                ring.transform.position = center;
                ring.transform.localScale = Vector3.one * (0.42f + i * 0.34f);
                SpriteRenderer renderer = ring.GetComponent<SpriteRenderer>();
                renderer.sprite = Art2DUtility.GetCircleSprite();
                renderer.color = new Color(0.72f, 0.94f, 1f, 0.38f - i * 0.08f);
                renderer.sortingOrder = 16;
                ring.GetComponent<BossLightningBurst2D>().Initialize(0.16f + i * 0.05f, 3.4f);
            }
        }
    }

    public class BossLightningBurst2D : MonoBehaviour
    {
        private float lifetime = 0.2f;
        private float expandSpeed = 3.2f;
        private float age;
        private SpriteRenderer spriteRenderer;
        private Color color;

        public void Initialize(float duration, float scaleSpeed)
        {
            lifetime = Mathf.Max(0.05f, duration);
            expandSpeed = scaleSpeed;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                color = spriteRenderer.color;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.localScale *= 1f + Time.deltaTime * expandSpeed;
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(color.a, 0f, age / lifetime);
                spriteRenderer.color = color;
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class BossPoisonPool2D : MonoBehaviour
    {
        private float radius;
        private int damage;
        private float delay;
        private float duration;
        private float nextDamageTime;
        private float age;
        private bool armed;
        private SpriteRenderer spriteRenderer;
        private Color color;

        public static void Create(Vector2 center, float radius, int damage, float delay, float duration)
        {
            GameObject obj = new GameObject("BossPoisonPool", typeof(SpriteRenderer), typeof(BossPoisonPool2D));
            obj.transform.position = center;
            obj.transform.localScale = Vector3.one * radius * 2f;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.LoadSprite("Environment/GroundEffects/PoisonPool");
            renderer.color = new Color(1f, 1f, 1f, 0.72f);
            renderer.sortingOrder = 12;
            BossPoisonPool2D pool = obj.GetComponent<BossPoisonPool2D>();
            pool.radius = radius;
            pool.damage = Mathf.Max(1, damage / 2);
            pool.delay = delay;
            pool.duration = duration;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            color = spriteRenderer != null ? spriteRenderer.color : Color.green;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (!armed && age >= delay)
            {
                armed = true;
                age = 0f;
                GameAudioService.Ensure()?.PlayBossBroodPoisonBurst();
                if (spriteRenderer != null)
                {
                    color.a = 0.5f;
                    spriteRenderer.color = color;
                }
            }

            if (!armed)
            {
                return;
            }

            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + 0.62f;
                DamagePlayerInside();
            }

            if (age >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void DamagePlayerInside()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Player"))
                {
                    continue;
                }

                PlayerController2D controller = hits[i].GetComponentInParent<PlayerController2D>();
                if (controller != null && controller.IsDodging)
                {
                    continue;
                }

                Bagsys.RogueLike.CharacterStats characterStats = hits[i].GetComponentInParent<Bagsys.RogueLike.CharacterStats>();
                if (characterStats != null)
                {
                    characterStats.TakeDamage(damage);
                }

                if (NewInventorySystem.Instance != null)
                {
                    NewInventorySystem.Instance.TakeDamage(damage);
                }
            }
        }
    }

    public class LoopEnemyBullet : MonoBehaviour
    {
        private int damage;

        public void Initialize(int damageValue)
        {
            damage = Mathf.Max(1, damageValue);
            Destroy(gameObject, 4f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerController2D controller = other.GetComponentInParent<PlayerController2D>();
            if (controller != null && controller.IsDodging)
            {
                Destroy(gameObject);
                return;
            }

            Bagsys.RogueLike.CharacterStats stats = other.GetComponentInParent<Bagsys.RogueLike.CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
            }

            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }

    public class LoopRewardChest : MonoBehaviour
    {
        private DungeonLoopDirector director;
        private bool bossReward;
        private int hp = 1;

        public void Initialize(DungeonLoopDirector owner, bool isBossReward)
        {
            director = owner;
            bossReward = isBossReward;
        }

        public void TakeDamage(int damage)
        {
            hp -= Mathf.Max(1, damage);
            if (hp > 0)
            {
                return;
            }

            if (director == null)
            {
                director = DungeonLoopDirector.Instance;
            }

            if (director != null)
            {
                director.GiveStageReward(bossReward);
            }

            NewRoomController room = GetComponentInParent<NewRoomController>();
            if (room != null)
            {
                room.SpawnNextPortal();
            }

            Destroy(gameObject);
        }
    }

    public class LoopEventInteractable : MonoBehaviour
    {
        private DungeonLoopDirector director;
        private bool isHealingFountain;
        private bool used;

        public static void Create(Transform parent, string name, string label, bool healingFountain, Color color)
        {
            SpriteRenderer renderer = Art2DUtility.CreateSpriteObject(parent, name, Vector2.zero, healingFountain ? new Vector2(1.4f, 1.4f) : new Vector2(1.25f, 1.65f), "Floor_Sprite", color, 5);
            CircleCollider2D collider = renderer.gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.4f;

            LoopEventInteractable interactable = renderer.gameObject.AddComponent<LoopEventInteractable>();
            interactable.director = DungeonLoopDirector.Instance;
            interactable.isHealingFountain = healingFountain;
            interactable.CreateLabel(label);
        }

        private void Update()
        {
            if (used || !Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector2.Distance(transform.position, player.transform.position) > 2.8f)
            {
                return;
            }

            if (director == null)
            {
                director = DungeonLoopDirector.Instance;
            }

            if (director == null)
            {
                return;
            }

            used = true;
            if (isHealingFountain)
            {
                director.HealPlayerToFull();
            }
            else
            {
                director.GiveEventReward();
            }

            NewRoomController room = GetComponentInParent<NewRoomController>();
            if (room != null)
            {
                room.SpawnNextPortal();
            }
        }

        private void CreateLabel(string label)
        {
            Canvas canvas = new GameObject("EventLabel", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);
            canvas.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            canvas.transform.localScale = Vector3.one * 0.012f;
            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 90f);

            Text text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(canvas.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }
    }
}
