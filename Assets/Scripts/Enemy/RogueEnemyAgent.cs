using UnityEngine;

namespace Bagsys.RogueLike
{
    public enum RogueEnemyTier
    {
        Slime,
        EliteThrower,
        BossTitan
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyStats))]
    public class RogueEnemyAgent : MonoBehaviour
    {
        [SerializeField] private RogueEnemyTier tier = RogueEnemyTier.Slime;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float preferredRange = 6f;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField] private int contactDamage = 7;
        [SerializeField] private int projectileDamage = 9;
        [SerializeField] private int shockwaveDamage = 18;

        private Rigidbody body;
        private EnemyStats stats;
        private Transform target;
        private float nextAttackTime;
        private float nextShockwaveTime;
        private bool bossEnraged;
        private Vector3 baseScale;
        private Renderer[] renderers;

        public RogueEnemyTier Tier => tier;

        public void Configure(RogueEnemyTier newTier)
        {
            tier = newTier;
            ApplyTierTuning();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            stats = GetComponent<EnemyStats>();
            baseScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>(true);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            ApplyTierTuning();
            EnsureVisualHelpers();
        }

        private void FixedUpdate()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            CacheTarget();
            if (target == null)
            {
                return;
            }

            switch (tier)
            {
                case RogueEnemyTier.EliteThrower:
                    TickEliteThrower();
                    break;
                case RogueEnemyTier.BossTitan:
                    TickBoss();
                    break;
                default:
                    TickSlime();
                    break;
            }
        }

        private void CacheTarget()
        {
            if (target != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void TickSlime()
        {
            Vector3 toTarget = FlatToTarget();
            MoveToward(toTarget.normalized, moveSpeed);
            if (toTarget.magnitude <= 1.25f)
            {
                TryContactDamage();
            }
        }

        private void TickEliteThrower()
        {
            Vector3 toTarget = FlatToTarget();
            float distance = toTarget.magnitude;
            Vector3 direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
            if (distance < preferredRange * 0.75f)
            {
                MoveToward(-direction, moveSpeed * 0.8f);
            }
            else if (distance > preferredRange * 1.15f)
            {
                MoveToward(direction, moveSpeed * 0.75f);
            }

            Face(direction);
            TryThrowProjectile(direction);
        }

        private void TickBoss()
        {
            Vector3 toTarget = FlatToTarget();
            float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 0f;
            if (!bossEnraged && hpRatio <= 0.7f)
            {
                bossEnraged = true;
                moveSpeed *= 1.24f;
                attackCooldown *= 0.78f;
            }

            Vector3 targetScale = bossEnraged ? baseScale * 1.28f : baseScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * 4f);
            FlashBossMaterial();

            MoveToward(toTarget.normalized, moveSpeed);
            if (toTarget.magnitude <= 1.8f)
            {
                TryContactDamage();
            }

            if (bossEnraged && Time.time >= nextShockwaveTime)
            {
                nextShockwaveTime = Time.time + 3.2f;
                DoShockwave();
            }
        }

        private Vector3 FlatToTarget()
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            return toTarget;
        }

        private void MoveToward(Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            body.MovePosition(body.position + direction.normalized * speed * Time.fixedDeltaTime);
            Face(direction);
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(direction, Vector3.up), Time.fixedDeltaTime * 10f));
        }

        private void TryContactDamage()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            CharacterStats targetStats = target.GetComponent<CharacterStats>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(contactDamage);
            }

            CombatVfxUtility.SpawnMeleeArc(transform.position + Vector3.up * 0.2f, transform.forward, 1.4f, 80f, new Color(1f, 0.18f, 0.12f, 1f), 0.12f);
        }

        private void TryThrowProjectile(Vector3 direction)
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "EliteBoneProjectile";
            projectile.transform.position = transform.position + Vector3.up * 0.9f + direction.normalized * 0.8f;
            projectile.transform.localScale = Vector3.one * 0.34f;
            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = RuntimeVisualUtility.CreateMaterial("EnemyProjectile", new Color(0.8f, 0.88f, 1f, 1f), 0f, 0.5f);
            }

            EnemyProjectile enemyProjectile = projectile.AddComponent<EnemyProjectile>();
            enemyProjectile.Initialize(direction, 8.2f, projectileDamage, 4f);
        }

        private void DoShockwave()
        {
            CombatVfxUtility.SpawnMeleeArc(transform.position + Vector3.up * 0.12f, transform.forward, 4.6f, 360f, new Color(1f, 0.18f, 0.08f, 1f), 0.22f);
            Collider[] hits = Physics.OverlapSphere(transform.position, 4.6f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || !hit.CompareTag("Player"))
                {
                    continue;
                }

                CharacterStats targetStats = hit.GetComponentInParent<CharacterStats>();
                if (targetStats != null)
                {
                    targetStats.TakeDamage(shockwaveDamage);
                }

                Rigidbody targetBody = hit.GetComponentInParent<Rigidbody>();
                if (targetBody != null)
                {
                    Vector3 away = hit.transform.position - transform.position;
                    away.y = 0f;
                    targetBody.AddForce(away.normalized * 8f + Vector3.up * 1.2f, ForceMode.Impulse);
                }
            }
        }

        private void FlashBossMaterial()
        {
            if (!bossEnraged || renderers == null)
            {
                return;
            }

            Color flash = Color.Lerp(new Color(0.72f, 0.12f, 0.14f), new Color(1f, 0.42f, 0.1f), Mathf.PingPong(Time.time * 3f, 1f));
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = flash;
                }
            }
        }

        private void ApplyTierTuning()
        {
            switch (tier)
            {
                case RogueEnemyTier.EliteThrower:
                    moveSpeed = 2.7f;
                    preferredRange = 7f;
                    attackCooldown = 1.55f;
                    projectileDamage = 11;
                    gameObject.name = "EliteThrower";
                    break;
                case RogueEnemyTier.BossTitan:
                    moveSpeed = 2.2f;
                    attackCooldown = 1.4f;
                    contactDamage = 14;
                    gameObject.name = "BossTitan";
                    transform.localScale = Vector3.one * 1.55f;
                    baseScale = transform.localScale;
                    break;
                default:
                    moveSpeed = 3.5f;
                    attackCooldown = 0.9f;
                    contactDamage = 6;
                    gameObject.name = "Slime";
                    break;
            }
        }

        private void EnsureVisualHelpers()
        {
            if (GetComponent<FloatingHealthBar>() == null)
            {
                gameObject.AddComponent<FloatingHealthBar>();
            }

            RuntimeArtBinder binder = GetComponent<RuntimeArtBinder>();
            if (binder == null)
            {
                binder = gameObject.AddComponent<RuntimeArtBinder>();
            }

            string artName = tier == RogueEnemyTier.BossTitan ? "Boss_Titan" : "Enemy_Slime";
            if (tier == RogueEnemyTier.EliteThrower)
            {
                artName = "Enemy_Skeleton";
            }

            binder.SetSprite(artName);
        }
    }
}
