using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class BossEncounterEnhancer2D : MonoBehaviour
    {
        private Bagsys.RogueLike.EnemyStats stats;
        private Transform player;
        private Color bossColor = Color.white;
        private string displayName = "Boss";
        private bool triggeredSixtySix;
        private bool triggeredThirtyThree;
        private bool enteredPhaseTwo;
        private float nextPressureTime;
        private float spin;

        public void Initialize(Color color, string bossDisplayName)
        {
            bossColor = color;
            displayName = bossDisplayName;
        }

        private void Awake()
        {
            stats = GetComponent<Bagsys.RogueLike.EnemyStats>();
        }

        private void Update()
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            CachePlayer();
            spin += Time.deltaTime * (enteredPhaseTwo ? 120f : 58f);
            float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 1f;
            if (!triggeredSixtySix && hpRatio <= 0.66f)
            {
                triggeredSixtySix = true;
                TriggerThresholdBurst("第一阶段崩裂", 10, 0.62f);
            }

            if (!enteredPhaseTwo && hpRatio <= 0.5f)
            {
                enteredPhaseTwo = true;
                TriggerPhaseTwo();
            }

            if (!triggeredThirtyThree && hpRatio <= 0.33f)
            {
                triggeredThirtyThree = true;
                TriggerThresholdBurst("濒死反击", 16, 0.76f);
            }

            if (enteredPhaseTwo && Time.time >= nextPressureTime)
            {
                nextPressureTime = Time.time + 4.6f;
                SpawnRoomPressure();
            }
        }

        private void TriggerPhaseTwo()
        {
            ShowBossText(displayName + "\n二阶段！");
            SpawnShockRing(new Color(bossColor.r, bossColor.g, bossColor.b, 0.6f), 3.6f);
            TriggerThresholdBurst("二阶段爆发", 14, 0.68f);
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowToast(displayName + " 进入二阶段\n攻击节奏加快，并开始压迫场地。");
            }
        }

        private void TriggerThresholdBurst(string label, int projectileCount, float delay)
        {
            ShowBossText(label);
            SpawnShockRing(new Color(1f, 1f, 1f, 0.45f), 2.8f);
            BossDelayedCircle2D.Create(transform.position, 2.25f, Mathf.Max(1, stats.ATK + 4), delay, new Color(bossColor.r, bossColor.g, bossColor.b, 0.34f));
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * (360f / projectileCount);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnBossBullet(direction, enteredPhaseTwo ? 8.2f : 6.8f, Mathf.Max(1, stats.ATK - 2));
            }
        }

        private void SpawnRoomPressure()
        {
            if (player == null)
            {
                return;
            }

            Vector2 playerPosition = player.position;
            BossDelayedCircle2D.Create(playerPosition, 1.75f, Mathf.Max(1, stats.ATK + 3), 0.82f, new Color(bossColor.r, bossColor.g, bossColor.b, 0.28f));
            BossWarningLine2D.Create(transform.position, ((Vector2)player.position - (Vector2)transform.position).normalized, 9.5f, 0.22f, new Color(bossColor.r, bossColor.g, bossColor.b, 0.42f), 0.58f);

            int count = 8;
            for (int i = 0; i < count; i++)
            {
                float angle = spin + i * (360f / count);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnBossBullet(direction, 5.8f, Mathf.Max(1, stats.ATK - 4));
            }
        }

        private void SpawnBossBullet(Vector2 direction, float speed, int damage)
        {
            GameObject bullet = new GameObject("EnhancedBossBullet", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(LoopEnemyBullet));
            bullet.transform.position = transform.position + (Vector3)(direction.normalized * 1.25f);
            bullet.transform.localScale = Vector3.one * (enteredPhaseTwo ? 0.42f : 0.34f);
            SpriteRenderer renderer = bullet.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = new Color(bossColor.r, bossColor.g, bossColor.b, enteredPhaseTwo ? 1f : 0.86f);
            renderer.sortingOrder = 15;
            CircleCollider2D collider = bullet.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.42f;
            Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = direction.normalized * speed;
            bullet.GetComponent<LoopEnemyBullet>().Initialize(damage);
        }

        private void SpawnShockRing(Color color, float size)
        {
            GameObject obj = new GameObject("BossShockRing", typeof(SpriteRenderer), typeof(BossEnhancerFade));
            obj.transform.position = transform.position;
            obj.transform.localScale = Vector3.one * size;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 18;
            obj.GetComponent<BossEnhancerFade>().Initialize(color, 0.48f, 1.8f);
        }

        private void ShowBossText(string text)
        {
            GameObject obj = new GameObject("BossPhaseText", typeof(TextMesh), typeof(BossTextFloat));
            obj.transform.position = transform.position + Vector3.up * 1.9f;
            TextMesh mesh = obj.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = 0.045f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(1f, 0.88f, 0.28f, 1f);
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 60;
            }
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
    }

    public class BossEnhancerFade : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color color;
        private float lifetime = 0.4f;
        private float scaleTo = 1.5f;
        private float age;

        public void Initialize(Color startColor, float duration, float targetScale)
        {
            color = startColor;
            lifetime = Mathf.Max(0.05f, duration);
            scaleTo = targetScale;
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
            float t = Mathf.Clamp01(age / lifetime);
            transform.localScale *= Mathf.Lerp(1f, scaleTo, Time.deltaTime * 5f);
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(color.a, 0f, t);
                spriteRenderer.color = color;
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class BossTextFloat : MonoBehaviour
    {
        private TextMesh text;
        private Color color;
        private float age;

        private void Awake()
        {
            text = GetComponent<TextMesh>();
            color = text != null ? text.color : Color.white;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 0.65f);
            if (text != null)
            {
                color.a = Mathf.Lerp(1f, 0f, age / 1.1f);
                text.color = color;
            }

            if (age >= 1.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
