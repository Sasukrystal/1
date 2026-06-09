using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class SKCharacterVisual2D : MonoBehaviour
    {
        private const float VisualRootScale = 2.1f;
        private const float CharacterSpriteScale = 2.05f;
        private const float LegacyArtSpriteScale = 0.28f;
        private const int CharacterSortingOrder = 120;
        private const float VisualZOffset = -1f;

        private Transform visualRoot;
        private SpriteRenderer characterRenderer;
        private RuntimeSpriteAnimator2D animator;
        private Rigidbody2D body;
        private PlayerAttack2D attack;
        private PlayerController2D controller;
        private HeroClass configuredClass = (HeroClass)(-1);
        private bool usingTinySwordsArcher;
        private bool skVisualReady = true;
        private Vector3 lastPosition;
        private float seed;

        private void Awake()
        {
            seed = Random.value * 10f;
            body = GetComponent<Rigidbody2D>();
            attack = GetComponent<PlayerAttack2D>();
            controller = GetComponent<PlayerController2D>();
            BuildVisualRoot();
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.Changed += HandleProgressionChanged;
            }

            ConfigureForClass(RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior);
            lastPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.Changed -= HandleProgressionChanged;
            }
        }

        private void HandleProgressionChanged()
        {
            HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
            if (heroClass != configuredClass)
            {
                ConfigureForClass(heroClass);
            }
        }

        private void LateUpdate()
        {
            if (PlayerLifeState2D.Instance != null && PlayerLifeState2D.Instance.IsDeadOrDying)
            {
                return;
            }

            HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
            bool useSkVisual = heroClass != HeroClass.Warrior && skVisualReady;
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(useSkVisual);
            }

            if (!useSkVisual)
            {
                return;
            }

            if (heroClass != configuredClass)
            {
                ConfigureForClass(heroClass);
                if (!skVisualReady)
                {
                    if (visualRoot != null)
                    {
                        visualRoot.gameObject.SetActive(false);
                    }

                    return;
                }
            }

            ApplyRendererSettings();
            UpdateFacing();
            ApplyFeetAlignment();

            Vector3 delta = transform.position - lastPosition;
            UpdateAnimationState(heroClass, delta);
            lastPosition = transform.position;
        }

        private void BuildVisualRoot()
        {
            Transform old = transform.Find("SKPlayerVisual");
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            GameObject root = new GameObject("SKPlayerVisual");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0f, VisualZOffset);
            root.transform.localScale = Vector3.one * VisualRootScale;
            visualRoot = root.transform;

            GameObject sprite = new GameObject("CharacterSprite", typeof(SpriteRenderer), typeof(RuntimeSpriteAnimator2D));
            sprite.transform.SetParent(root.transform, false);
            sprite.transform.localScale = Vector3.one * CharacterSpriteScale;
            characterRenderer = sprite.GetComponent<SpriteRenderer>();
            characterRenderer.sortingLayerName = "Player";
            characterRenderer.sortingOrder = CharacterSortingOrder;
            animator = sprite.GetComponent<RuntimeSpriteAnimator2D>();
        }

        private void ApplyRendererSettings()
        {
            if (characterRenderer != null)
            {
                characterRenderer.enabled = true;
                characterRenderer.sortingLayerName = "Player";
                characterRenderer.sortingOrder = CharacterSortingOrder;
            }
        }

        private void UpdateFacing()
        {
            if (characterRenderer == null)
            {
                return;
            }

            if (controller == null)
            {
                controller = GetComponent<PlayerController2D>();
            }

            Vector2 aimDirection = controller != null ? controller.AimDirection : Vector2.right;
            characterRenderer.flipX = aimDirection.x < -0.01f;
        }

        private void ConfigureForClass(HeroClass heroClass)
        {
            configuredClass = heroClass;
            usingTinySwordsArcher = false;
            if (animator == null)
            {
                return;
            }

            animator.ClearClips();

            if (characterRenderer != null)
            {
                characterRenderer.color = Color.white;
                characterRenderer.transform.localRotation = Quaternion.identity;
                characterRenderer.transform.localPosition = Vector3.zero;
                characterRenderer.sprite = null;
            }

            if (heroClass == HeroClass.Archer && ConfigureArcherFromTinySwords())
            {
                usingTinySwordsArcher = true;
                skVisualReady = true;
                ApplyCharacterSpriteScale();
                animator.SetState("Idle", 4f, false, true);
                return;
            }

            if (heroClass == HeroClass.Archer && TryConfigureArcherIdleFallback())
            {
                usingTinySwordsArcher = true;
                skVisualReady = true;
                ApplyCharacterSpriteScale();
                animator.SetState("Idle", 4f, false, true);
                Debug.LogWarning("SKCharacterVisual2D: TinySwords archer using Idle-only fallback.");
                return;
            }

            if (heroClass == HeroClass.Archer)
            {
                skVisualReady = false;
                Debug.LogWarning("SKCharacterVisual2D: TinySwords archer sheets missing; legacy archer art is disabled.");
                return;
            }

            skVisualReady = true;
            ApplyCharacterSpriteScale();
            string folder = "AnimationSprites/Player/" + heroClass;
            string prefix = heroClass.ToString();
            animator.SetClip("Idle", Art2DUtility.LoadSequence(folder, prefix + "_Idle_01", prefix + "_Idle_02"));
            animator.SetClip("Run", Art2DUtility.LoadSequence(folder, prefix + "_Run_01", prefix + "_Run_02", prefix + "_Run_03", prefix + "_Run_04"));
            if (heroClass == HeroClass.Warrior)
            {
                animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, "Warrior_Attack_01", "Warrior_Attack_02", "Warrior_Attack_03"));
                animator.SetClip("Special", Art2DUtility.LoadSequence(folder, "Warrior_ShieldBlock_01", "Warrior_ShieldBlock_02"));
            }
            else
            {
                animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, "Mage_Cast_01", "Mage_Cast_02", "Mage_Cast_03"));
                animator.SetClip("Special", Art2DUtility.LoadSequence(folder, "Mage_Channel_01", "Mage_Channel_02"));
            }

            animator.SetClip("Dodge", Art2DUtility.LoadSequence(folder, prefix + "_Dodge_01", prefix + "_Dodge_02"));
            animator.SetClip("Hit", Art2DUtility.LoadSequence(folder, prefix + "_Hit_01"));
            animator.SetState("Idle", 4f, false, true);
        }

        private void ApplyFeetAlignment()
        {
            if (visualRoot == null || characterRenderer == null)
            {
                return;
            }

            float bob = Mathf.Sin((Time.time + seed) * 8f) * 0.025f;
            characterRenderer.transform.localPosition = new Vector3(0f, bob, 0f);
            PlayerVisualAlign2D.AlignVisualRootFeet(transform, visualRoot, VisualZOffset);
        }

        private bool ConfigureArcherFromTinySwords()
        {
            bool hasIdle = TinySwordsExternalSpriteLibrary.TryGetArcherClip("Idle", out Sprite[] idle) && idle != null && idle.Length > 0;
            bool hasRun = TinySwordsExternalSpriteLibrary.TryGetArcherClip("Run", out Sprite[] run) && run != null && run.Length > 0;
            bool hasAttack = TinySwordsExternalSpriteLibrary.TryGetArcherClip("Attack", out Sprite[] attackClip) && attackClip != null && attackClip.Length > 0;

            if (!hasIdle || !hasRun || !hasAttack)
            {
                return false;
            }

            animator.SetClip("Idle", idle);
            animator.SetClip("Run", run);
            animator.SetClip("Attack", attackClip);

            if (TinySwordsExternalSpriteLibrary.TryGetArcherClip("Special", out Sprite[] special) && special != null && special.Length > 0)
            {
                animator.SetClip("Special", special);
            }

            if (TinySwordsExternalSpriteLibrary.TryGetArcherClip("Dodge", out Sprite[] dodge) && dodge != null && dodge.Length > 0)
            {
                animator.SetClip("Dodge", dodge);
            }

            if (TinySwordsExternalSpriteLibrary.TryGetArcherClip("Hit", out Sprite[] hit) && hit != null && hit.Length > 0)
            {
                animator.SetClip("Hit", hit);
            }

            return true;
        }

        private bool TryConfigureArcherIdleFallback()
        {
            if (!TinySwordsExternalSpriteLibrary.TryGetArcherClip("Idle", out Sprite[] idle) || idle == null || idle.Length == 0)
            {
                return false;
            }

            animator.SetClip("Idle", idle);
            if (TinySwordsExternalSpriteLibrary.TryGetArcherClip("Run", out Sprite[] run) && run != null && run.Length > 0)
            {
                animator.SetClip("Run", run);
            }

            if (TinySwordsExternalSpriteLibrary.TryGetArcherClip("Attack", out Sprite[] attack) && attack != null && attack.Length > 0)
            {
                animator.SetClip("Attack", attack);
            }

            return true;
        }

        private void ApplyCharacterSpriteScale()
        {
            if (characterRenderer == null)
            {
                return;
            }

            float scale = usingTinySwordsArcher ? CharacterSpriteScale : LegacyArtSpriteScale;
            characterRenderer.transform.localScale = Vector3.one * scale;
        }

        private void UpdateAnimationState(HeroClass heroClass, Vector3 delta)
        {
            if (animator == null)
            {
                return;
            }

            if (controller != null && controller.IsDodging)
            {
                animator.SetState("Dodge", 13f);
                return;
            }

            if (heroClass == HeroClass.Warrior && RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsShielding)
            {
                animator.SetState("Special", 8f);
                return;
            }

            if (heroClass == HeroClass.Archer && attack != null && attack.IsChargingBow)
            {
                animator.SetState("Special", 8f);
                return;
            }

            if (attack != null && Time.time - attack.LastAttackTime < 0.18f)
            {
                animator.SetState("Attack", 16f, false, true);
                return;
            }

            if (delta.sqrMagnitude > 0.00005f || (body != null && body.velocity.sqrMagnitude > 0.08f))
            {
                animator.SetState("Run", 10f);
                return;
            }

            animator.SetState("Idle", 4f);
        }

        public void PlayHitFlash()
        {
            if (animator != null)
            {
                animator.PlayOnce("Hit", animator.CurrentState == "Run" ? "Run" : "Idle", 12f);
            }
        }

        public void PlayDeathPose()
        {
            if (animator != null)
            {
                animator.PlayOnce("Hit", "Idle", 0f);
            }

            if (characterRenderer != null)
            {
                characterRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 82f);
                characterRenderer.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                characterRenderer.color = new Color(0.55f, 0.55f, 0.62f, 0.92f);
            }
        }

        public void ResetDeathPose()
        {
            if (characterRenderer != null)
            {
                characterRenderer.transform.localRotation = Quaternion.identity;
                characterRenderer.transform.localPosition = Vector3.zero;
                characterRenderer.color = Color.white;
            }
        }
    }
}
