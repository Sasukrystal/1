using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public class BossStageController : MonoBehaviour
    {
        private SoulKnightDirector director;
        private Bagsys.RogueLike.EnemyStats bossStats;
        private GameObject bossObject;
        private GameObject magicStone;
        private bool magicStoneSpawned;

        public static void BuildBossStage(Transform root, SoulKnightDirector director)
        {
            CreateBossRoom(root);
            GameObject controllerObject = new GameObject("BossStageController");
            controllerObject.transform.SetParent(root, false);
            BossStageController controller = controllerObject.AddComponent<BossStageController>();
            controller.Initialize(root, director);
        }

        public void Initialize(Transform root, SoulKnightDirector owner)
        {
            director = owner;
            bossObject = CreateBoss(root);
            bossStats = bossObject.GetComponent<Bagsys.RogueLike.EnemyStats>();
        }

        private void Update()
        {
            if (!magicStoneSpawned && bossObject == null)
            {
                SpawnMagicStone();
            }
        }

        private static void CreateBossRoom(Transform root)
        {
            SoulKnightDungeonBuilder.CreateSprite(root, "BossFloor", Vector2.zero, new Vector2(26f, 18f), "Floor_Sprite", new Color(0.12f, 0.12f, 0.18f), -10);
            Color wallColor = new Color(0.05f, 0.055f, 0.075f);
            CreateWall(root, "BossNorthWall", new Vector2(0f, 9.25f), new Vector2(26f, 0.55f), wallColor);
            CreateWall(root, "BossSouthWall", new Vector2(0f, -9.25f), new Vector2(26f, 0.55f), wallColor);
            CreateWall(root, "BossWestWall", new Vector2(-13.25f, 0f), new Vector2(0.55f, 18f), wallColor);
            CreateWall(root, "BossEastWall", new Vector2(13.25f, 0f), new Vector2(0.55f, 18f), wallColor);
        }

        private static void CreateWall(Transform root, string name, Vector2 pos, Vector2 scale, Color color)
        {
            GameObject wall = SoulKnightDungeonBuilder.CreateSprite(root, name, pos, scale, "Wall_Sprite", color, 2).gameObject;
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private GameObject CreateBoss(Transform root)
        {
            SpriteRenderer renderer = SoulKnightDungeonBuilder.CreateSprite(root, "SoulKnight_Boss", new Vector2(0f, 1.8f), new Vector2(2.6f, 2.6f), "Boss_Sprite", new Color(0.18f, 0.35f, 1f), 5);
            GameObject boss = renderer.gameObject;
            boss.tag = "Enemy";

            Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = boss.AddComponent<CircleCollider2D>();
            collider.radius = 0.58f;

            Bagsys.RogueLike.EnemyStats stats = boss.AddComponent<Bagsys.RogueLike.EnemyStats>();
            stats.ConfigureBaseStats(500, 22, 0);
            BossTopHealthBar2D topBar = boss.AddComponent<BossTopHealthBar2D>();
            topBar.Initialize("SoulKnight Boss", new Color(0.15f, 0.42f, 1f, 1f));
            boss.AddComponent<SoulKnightBossBrain>().Initialize(director);
            return boss;
        }

        private void SpawnMagicStone()
        {
            magicStoneSpawned = true;
            magicStone = SoulKnightDungeonBuilder.CreateSprite(transform.parent, "MagicStone", Vector2.zero, new Vector2(1.2f, 1.2f), "Projectile_Wand", new Color(0.5f, 1f, 0.95f), 8).gameObject;
            CircleCollider2D collider = magicStone.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.58f;
            magicStone.AddComponent<MagicStone>().Initialize(director);
        }
    }

    public class SoulKnightBossBrain : MonoBehaviour
    {
        private SoulKnightDirector director;
        private Bagsys.RogueLike.EnemyStats stats;
        private Rigidbody2D body;
        private Transform player;
        private float nextAttackTime;
        private float nextShockwaveTime;
        private bool phaseTwo;
        private Color originalColor;
        private SpriteRenderer spriteRenderer;

        public void Initialize(SoulKnightDirector owner)
        {
            director = owner;
        }

        private void Awake()
        {
            stats = GetComponent<Bagsys.RogueLike.EnemyStats>();
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.blue;
        }

        private void FixedUpdate()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            CachePlayer();
            if (player != null && body != null)
            {
                Vector2 direction = ((Vector2)player.position - body.position).normalized;
                body.MovePosition(body.position + direction * 1.45f * Time.fixedDeltaTime);
            }
        }

        private void Update()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            CachePlayer();
            float ratio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!phaseTwo && ratio <= 0.5f)
            {
                phaseTwo = true;
                transform.localScale *= 1.5f;
            }

            float cooldown = phaseTwo ? 0.9f : 1.45f;
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + cooldown;
                FireFan();
            }

            if (phaseTwo && Time.time >= nextShockwaveTime)
            {
                nextShockwaveTime = Time.time + 3.2f;
                Shockwave();
            }
        }

        private void CachePlayer()
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

        private void FireFan()
        {
            if (player == null)
            {
                return;
            }

            Vector2 baseDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            int count = phaseTwo ? 9 : 5;
            float spread = phaseTwo ? 72f : 46f;
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;
                SpawnBossBullet(direction);
            }
        }

        private void SpawnBossBullet(Vector2 direction)
        {
            GameObject bullet = new GameObject("BossScatterBullet", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SKDamageBullet2D));
            bullet.transform.position = transform.position + (Vector3)(direction * 0.9f);
            bullet.transform.localScale = Vector3.one * 0.38f;
            SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
            bulletRenderer.sprite = Art2DUtility.LoadSprite("Projectile_Wand");
            bulletRenderer.color = new Color(1f, 0.28f, 0.65f);
            bulletRenderer.sortingOrder = 12;

            CircleCollider2D collider = bullet.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.42f;

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.velocity = direction.normalized * (phaseTwo ? 8.8f : 7.2f);
            bullet.GetComponent<SKDamageBullet2D>().Initialize(14);
        }

        private void Shockwave()
        {
            if (spriteRenderer != null)
            {
                HitFlash2D.Flash(this, spriteRenderer, originalColor);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 7f);
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

                Bagsys.RogueLike.CharacterStats stats = hits[i].GetComponentInParent<Bagsys.RogueLike.CharacterStats>();
                if (stats != null)
                {
                    stats.TakeDamage(18);
                }

                Rigidbody2D rb = hits[i].GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 away = ((Vector2)hits[i].transform.position - (Vector2)transform.position).normalized;
                    rb.AddForce(away * 8f, ForceMode2D.Impulse);
                }
            }
        }
    }

    public class SKDamageBullet2D : MonoBehaviour
    {
        private int damage;

        public void Initialize(int amount)
        {
            damage = Mathf.Max(1, amount);
            Destroy(gameObject, 5f);
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

    public class MagicStone : MonoBehaviour
    {
        private SoulKnightDirector director;

        public void Initialize(SoulKnightDirector owner)
        {
            director = owner;
        }

        private void Update()
        {
            transform.Rotate(Vector3.forward, 95f * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (director == null)
            {
                director = SoulKnightDirector.Instance;
            }

            if (director != null)
            {
                director.ShowSettlement();
            }
        }
    }
}
