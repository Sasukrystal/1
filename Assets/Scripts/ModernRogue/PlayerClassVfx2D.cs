using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(260)]
    [DisallowMultipleComponent]
    public class PlayerClassVfx2D : MonoBehaviour
    {
        private const float ChargeBarWidth = 0.07f;
        private const float ChargeBarHeight = 0.44f;

        private SpriteRenderer shieldBubble;
        private SpriteRenderer shieldFront;
        private SpriteRenderer mageAura;
        private Transform bowChargeRoot;
        private SpriteRenderer bowChargeFrame;
        private SpriteRenderer bowChargeBack;
        private SpriteRenderer bowChargeFill;
        private PlayerController2D controller;
        private PlayerAttack2D attack;
        private float shieldVisibleUntil;
        private float mageAuraUntil;

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            attack = GetComponent<PlayerAttack2D>();
            DestroyLegacyBowChargeUi();
            BuildArcherBowChargeBar();
            BuildShieldVfx();
            BuildMageAura();
            SetShieldVisible(false);
            SetMageAuraVisible(false);
            SetBowChargeVisible(false);
        }

        private static void DestroyLegacyBowChargeUi()
        {
            DestroyNamedRoot("BowChargeOverlayCanvas");
            DestroyNamedRoot("BowChargeOverlay");
            DestroyNamedRoot("BowChargeVfx");
        }

        private static void DestroyNamedRoot(string objectName)
        {
            GameObject[] matches = Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < matches.Length; i++)
            {
                GameObject candidate = matches[i];
                if (candidate != null && candidate.name == objectName)
                {
                    Object.Destroy(candidate);
                }
            }
        }

        private void LateUpdate()
        {
            if (attack == null)
            {
                attack = GetComponent<PlayerAttack2D>();
            }

            RunProgressionSystem run = RunProgressionSystem.Instance;
            bool shielding = run != null && run.IsShielding;
            SetShieldVisible(shielding || Time.time < shieldVisibleUntil);
            if (shieldBubble != null && shieldBubble.enabled)
            {
                float pulse = Mathf.Lerp(0.92f, 1.08f, Mathf.PingPong(Time.time * 8f, 1f));
                shieldBubble.transform.localScale = Vector3.one * (pulse * 1.18f);
                shieldFront.transform.localScale = new Vector3(0.34f, 1.58f + Mathf.PingPong(Time.time * 5f, 1f) * 0.18f, 1f);
            }

            SetMageAuraVisible(Time.time < mageAuraUntil);
            if (mageAura != null && mageAura.enabled)
            {
                float pulse = Mathf.Lerp(1.25f, 1.78f, Mathf.PingPong(Time.time * 7f, 1f));
                mageAura.transform.localScale = Vector3.one * pulse;
            }

            if (attack != null && attack.IsChargingBow && IsArcherClass())
            {
                UpdateBowChargeBar(attack.CurrentBowChargeRatio);
                SetBowChargeVisible(true);
            }
            else if (bowChargeRoot != null && bowChargeRoot.gameObject.activeSelf)
            {
                SetBowChargeVisible(false);
            }

            SyncBodyAttachedVfx();
        }

        private void SyncBodyAttachedVfx()
        {
            Vector3 bodyCenter = ResolveBodyCenterWorld();
            Vector2 aim = ResolveAimDirection();
            if (shieldBubble != null)
            {
                shieldBubble.transform.position = bodyCenter;
            }

            if (shieldFront != null)
            {
                Vector2 forward = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
                shieldFront.transform.position = bodyCenter + (Vector3)(forward * 1.05f);
                shieldFront.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
            }

            if (mageAura != null)
            {
                mageAura.transform.position = bodyCenter;
            }
        }

        private Vector3 ResolveBodyCenterWorld()
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

        public Vector2 GetArcherMuzzleWorldPosition(Vector2 forward)
        {
            Vector2 direction = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector2.right;
            if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer renderer))
            {
                Bounds bounds = renderer.bounds;
                float forwardOffset = Mathf.Max(0.42f, bounds.extents.x * 0.42f);
                return (Vector2)bounds.center + direction * forwardOffset;
            }

            Vector2 origin = PlayerAim2D.ResolveVisualAimOrigin(transform);
            return origin + direction * 0.72f;
        }

        public void BeginBowCharge()
        {
            if (!IsArcherClass())
            {
                return;
            }

            UpdateBowChargeBar(0f);
            SetBowChargeVisible(true);
        }

        public void UpdateBowCharge(float ratio)
        {
            if (!IsArcherClass())
            {
                return;
            }

            UpdateBowChargeBar(Mathf.Clamp01(ratio));
            SetBowChargeVisible(true);
        }

        public void EndBowCharge(float ratio)
        {
            SetBowChargeVisible(false);
            if (ratio >= 0.98f)
            {
                SpawnPerfectChargeBurst();
            }
        }

        public void CancelBowCharge()
        {
            SetBowChargeVisible(false);
        }

        public void ShowWarriorShield()
        {
            shieldVisibleUntil = Time.time + 1.05f;
            SetShieldVisible(true);
            SpawnShieldClang();
        }

        public void ShowMageDodge(Vector2 direction)
        {
            mageAuraUntil = Time.time + 0.34f;
            SetMageAuraVisible(true);
            SpawnAfterImages(new Color(0.42f, 0.82f, 1f, 0.46f), direction);
            SpawnBlinkSpark(direction);
        }

        public void ShowArcherDodge(Vector2 direction)
        {
            SpawnAfterImages(new Color(0.35f, 1f, 0.62f, 0.34f), direction);
            SpawnMotionLine(direction, new Color(0.62f, 0.92f, 1f, 0.28f), 1.75f, 0.09f);
            SpawnDashBurst(direction, new Color(0.54f, 0.9f, 1f, 0.78f));
        }

        public void ShowWarriorDodge(Vector2 direction)
        {
            SpawnMotionLine(direction, new Color(0.85f, 0.9f, 1f, 0.36f));
        }

        public void ShowMageCast(Vector2 direction)
        {
            SpawnMuzzleRing(direction, new Color(0.45f, 0.82f, 1f, 0.8f), 0.58f);
        }

        public void ShowArcherShot(Vector2 direction, bool piercing, float chargeRatio)
        {
            float outerSize = Mathf.Lerp(0.58f, piercing ? 0.98f : 0.76f, chargeRatio);
            float innerSize = Mathf.Lerp(0.74f, piercing ? 1.26f : 0.94f, chargeRatio);
            SpawnMuzzleRing(direction, piercing ? new Color(1f, 0.82f, 0.16f, 0.9f) : new Color(0.38f, 0.9f, 1f, 0.72f), outerSize);
            SpawnMuzzleRing(direction, piercing ? new Color(1f, 0.94f, 0.6f, 0.4f) : new Color(0.55f, 0.95f, 1f, 0.3f), innerSize);
            if (piercing)
            {
                SpawnMotionLine(direction, new Color(1f, 0.76f, 0.12f, 0.58f), 2.15f, 0.16f);
            }
        }

        private void BuildArcherBowChargeBar()
        {
            Transform existing = transform.Find("ArcherBowChargeBar");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject root = new GameObject("ArcherBowChargeBar");
            root.transform.SetParent(transform, false);
            bowChargeRoot = root.transform;

            bowChargeFrame = CreateBowChargeSlice(root.transform, "Frame", 118, new Color(0.78f, 0.9f, 1f, 0.62f));
            bowChargeBack = CreateBowChargeSlice(root.transform, "Back", 117, new Color(0.04f, 0.06f, 0.09f, 0.78f));
            bowChargeFill = CreateBowChargeSlice(root.transform, "Fill", 119, new Color(0.38f, 0.9f, 1f, 0.95f));

            bowChargeFrame.transform.localScale = new Vector3(ChargeBarWidth + 0.018f, ChargeBarHeight + 0.018f, 1f);
            bowChargeBack.transform.localScale = new Vector3(ChargeBarWidth, ChargeBarHeight, 1f);
            bowChargeFill.transform.localScale = new Vector3(ChargeBarWidth * 0.72f, 0.04f, 1f);
        }

        private static SpriteRenderer CreateBowChargeSlice(Transform parent, string name, int sortingOrder, Color color)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private bool IsArcherClass()
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            return run != null && run.CurrentClass == HeroClass.Archer;
        }

        private void UpdateBowChargeBar(float ratio)
        {
            if (bowChargeRoot == null || bowChargeFill == null || bowChargeBack == null || bowChargeFrame == null)
            {
                return;
            }

            if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer renderer))
            {
                Bounds bounds = renderer.bounds;
                Vector2 aim = ResolveAimDirection();
                Vector2 back = aim.sqrMagnitude > 0.0001f ? -aim.normalized : Vector2.left;
                float sideDistance = Mathf.Max(0.24f, bounds.extents.x * 0.46f);
                bowChargeRoot.position = bounds.center + (Vector3)(back * sideDistance);
            }
            else
            {
                PlayerVisualHub2D hub = PlayerVisualHub2D.Get(transform);
                Vector2 center = hub != null ? hub.BodyCenter : (Vector2)transform.position;
                bowChargeRoot.position = center + Vector2.left * 0.28f;
            }

            bowChargeRoot.rotation = Quaternion.identity;

            ratio = Mathf.Clamp01(ratio);
            float fillHeight = Mathf.Max(0.04f, ChargeBarHeight * ratio);
            bowChargeFill.transform.localScale = new Vector3(ChargeBarWidth * 0.72f, fillHeight, 1f);
            bowChargeFill.transform.localPosition = new Vector3(0f, (-ChargeBarHeight * 0.5f) + (fillHeight * 0.5f), 0f);

            bool ready = ratio >= 0.98f;
            bowChargeFrame.color = ready
                ? new Color(1f, 0.82f, 0.24f, 0.82f)
                : new Color(0.78f, 0.9f, 1f, 0.62f);
            bowChargeFill.color = ready
                ? new Color(1f, 0.84f, 0.2f, 0.98f)
                : Color.Lerp(new Color(0.38f, 0.9f, 1f, 0.92f), new Color(0.82f, 0.98f, 1f, 0.98f), ratio);
        }

        private void SetBowChargeVisible(bool visible)
        {
            if (bowChargeRoot != null)
            {
                bowChargeRoot.gameObject.SetActive(visible);
            }
        }

        private void BuildShieldVfx()
        {
            shieldBubble = CreateCircle(transform, "WarriorShieldBubble", new Vector2(0f, 0f), new Vector2(2.05f, 2.05f), new Color(0.72f, 0.82f, 1f, 0.28f), 34);
            shieldFront = CreateRect(transform, "WarriorShieldFront", new Vector2(1.05f, 0f), new Vector2(0.34f, 1.58f), new Color(0.82f, 0.9f, 1f, 0.78f), 35);
        }

        private void BuildMageAura()
        {
            mageAura = CreateCircle(transform, "MageDodgeAura", Vector2.zero, new Vector2(1.55f, 1.55f), new Color(0.3f, 0.75f, 1f, 0.38f), 33);
        }

        private void SetShieldVisible(bool visible)
        {
            if (shieldBubble != null)
            {
                shieldBubble.enabled = visible;
            }

            if (shieldFront != null)
            {
                shieldFront.enabled = visible;
            }
        }

        private void SetMageAuraVisible(bool visible)
        {
            if (mageAura != null)
            {
                mageAura.enabled = visible;
            }
        }

        private void SpawnPerfectChargeBurst()
        {
            Vector3 origin = PlayerVisualHub2D.Get(transform) != null
                ? (Vector3)PlayerVisualHub2D.Get(transform).BodyCenter
                : transform.position;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnSpark(origin + (Vector3)(direction * 0.45f), direction, new Color(1f, 0.83f, 0.16f, 0.86f), 0.28f);
            }
        }

        private Vector2 ResolveAimDirection()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController2D>();
            }

            if (controller == null || controller.AimDirection.sqrMagnitude < 0.0001f)
            {
                return Vector2.right;
            }

            return controller.AimDirection.normalized;
        }

        private void SpawnShieldClang()
        {
            Vector2 forward = ResolveAimDirection();
            SpawnMuzzleRing(forward, new Color(0.82f, 0.9f, 1f, 0.72f), 0.8f);
        }

        private void SpawnBlinkSpark(Vector2 direction)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector2 rotated = Quaternion.Euler(0f, 0f, Random.Range(-55f, 55f)) * -direction.normalized;
                SpawnSpark(transform.position + (Vector3)(rotated * 0.25f), rotated, new Color(0.4f, 0.86f, 1f, 0.82f), 0.36f);
            }
        }

        private void SpawnAfterImages(Color color, Vector2 direction)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer source = renderers[i];
                if (source == null || source.sprite == null || source.color.a < 0.05f || source.sortingOrder < 7)
                {
                    continue;
                }

                string lowerName = source.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("shadow") || lowerName.Contains("charge") || lowerName.Contains("overlay")
                    || lowerName.Contains("frame") || lowerName.Contains("aura") || lowerName.Contains("shield")
                    || lowerName.Contains("bubble") || lowerName.Contains("vfx") || lowerName.Contains("health"))
                {
                    continue;
                }

                GameObject ghost = new GameObject("ClassDodgeAfterImage", typeof(SpriteRenderer), typeof(ClassVfxFade));
                ghost.transform.position = source.transform.position - (Vector3)(direction.normalized * 0.18f);
                ghost.transform.rotation = source.transform.rotation;
                ghost.transform.localScale = source.transform.lossyScale;
                SpriteRenderer renderer = ghost.GetComponent<SpriteRenderer>();
                renderer.sprite = source.sprite;
                renderer.color = color;
                renderer.sortingOrder = source.sortingOrder - 1;
                ghost.GetComponent<ClassVfxFade>().Initialize(color, 0.26f, 1.08f);
            }
        }

        private void SpawnMotionLine(Vector2 direction, Color color)
        {
            SpawnMotionLine(direction, color, 1.35f, 0.08f);
        }

        private void SpawnMotionLine(Vector2 direction, Color color, float length, float thickness)
        {
            Vector2 origin = (Vector2)ResolveEffectOrigin(direction) - direction.normalized * 0.28f;
            GameObject line = new GameObject("ClassMotionLine", typeof(SpriteRenderer), typeof(ClassVfxFade));
            line.transform.position = origin;
            line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            line.transform.localScale = new Vector3(length, thickness, 1f);
            SpriteRenderer renderer = line.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 22;
            line.GetComponent<ClassVfxFade>().Initialize(color, 0.22f, 1.35f);
        }

        private void SpawnDashBurst(Vector2 direction, Color color)
        {
            Vector3 effectOrigin = ResolveEffectOrigin(-direction.normalized);
            SpawnMuzzleRing(-direction.normalized, new Color(color.r, color.g, color.b, 0.42f), 0.62f);
            for (int i = 0; i < 6; i++)
            {
                Vector2 burstDirection = Quaternion.Euler(0f, 0f, Random.Range(-48f, 48f)) * -direction.normalized;
                SpawnSpark(effectOrigin + (Vector3)(burstDirection * 0.14f), burstDirection, color, 0.28f);
            }
        }

        private void SpawnMuzzleRing(Vector2 direction, Color color, float size)
        {
            GameObject ring = new GameObject("ClassMuzzleRing", typeof(SpriteRenderer), typeof(ClassVfxFade));
            ring.transform.position = ResolveMuzzleWorldPoint(direction);
            ring.transform.localScale = Vector3.one * size;
            SpriteRenderer renderer = ring.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 132;
            ring.GetComponent<ClassVfxFade>().Initialize(color, 0.2f, 1.55f);
        }

        private Vector3 ResolveMuzzleWorldPoint(Vector2 direction)
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            if (heroClass == HeroClass.Archer)
            {
                return ResolveArcherFocusPoint(direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);
            }

            Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            return ResolveBodyCenterWorld() + (Vector3)(forward * 0.85f);
        }

        private Vector3 ResolveArcherFocusPoint(Vector2 direction)
        {
            if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer renderer))
            {
                Bounds bounds = renderer.bounds;
                float forwardOffset = Mathf.Max(0.42f, bounds.extents.x * 0.42f);
                return (Vector2)bounds.center + direction.normalized * forwardOffset;
            }

            Vector2 origin = PlayerAim2D.ResolveVisualAimOrigin(transform);
            return origin + direction.normalized * 0.72f;
        }

        private void SpawnSpark(Vector3 position, Vector2 direction, Color color, float lifetime)
        {
            GameObject spark = new GameObject("ClassSpark", typeof(SpriteRenderer), typeof(ClassSpark2D));
            spark.transform.position = position;
            spark.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            spark.transform.localScale = new Vector3(0.34f, 0.08f, 1f);
            SpriteRenderer renderer = spark.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 133;
            spark.GetComponent<ClassSpark2D>().Initialize(direction.normalized, color, lifetime);
        }

        private SpriteRenderer CreateCircle(Transform parent, string name, Vector2 position, Vector2 scale, Color color, int sorting)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sorting;
            return renderer;
        }

        private SpriteRenderer CreateRect(Transform parent, string name, Vector2 position, Vector2 scale, Color color, int sorting)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sorting;
            return renderer;
        }

        private Vector3 ResolveEffectOrigin(Vector2 direction)
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            if (heroClass == HeroClass.Archer)
            {
                return ResolveArcherFocusPoint(direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);
            }

            return ResolveBodyCenterWorld();
        }

        private bool TryGetPrimaryVisualRenderer(out SpriteRenderer result)
        {
            return PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out result);
        }
    }

    public class ClassVfxFade : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color color;
        private float lifetime = 0.2f;
        private float scaleTo = 1.2f;
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
            transform.localScale *= Mathf.Lerp(1f, scaleTo, Time.deltaTime * 8f);
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

    public class ClassSpark2D : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector2 direction;
        private Color color;
        private float lifetime = 0.3f;
        private float age;

        public void Initialize(Vector2 moveDirection, Color startColor, float duration)
        {
            direction = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : Vector2.right;
            color = startColor;
            lifetime = Mathf.Max(0.05f, duration);
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
            transform.position += (Vector3)(direction * (Time.deltaTime * 2.8f));
            float t = Mathf.Clamp01(age / lifetime);
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
}
