using System.Collections;
using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public class TinySwordsPlayerVisual2D : MonoBehaviour
    {
        private const string ResourcePath = "ArtIntegration/Player/Player_TinySwordsWarrior_Art";
        private const string VisualInstanceName = "TinySwordsWarriorRuntimeVisual";
        private const float MoveThreshold = 0.03f;
        private const float VisualScale = 2.1f;
        private const float VisualZOffset = -1f;
        private const int VisualSortingOrder = 120;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int GuardHash = Animator.StringToHash("Guard");

        private GameObject visualInstance;
        private Animator animator;
        private SpriteRenderer[] spriteRenderers;
        private Rigidbody2D body;
        private PlayerAttack2D attack;
        private PlayerController2D controller;
        private float lastAttackTime = -999f;
        private bool warnedMissingPrefab;
        private Transform feetAnchor;
        private Transform centerAnchor;
        private Vector3 visualBaseLocalPosition;
        private Quaternion visualBaseLocalRotation;
        private Color[] cachedRendererColors;
        private bool deathPoseActive;
        private Coroutine hitFlashRoutine;

        public bool TryGetVisualAnchors(out Transform feet, out Transform center)
        {
            feet = null;
            center = null;
            if (!IsWarriorClass() || visualInstance == null || !visualInstance.activeInHierarchy)
            {
                return false;
            }

            feet = feetAnchor;
            center = centerAnchor;
            return feet != null || center != null;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            attack = GetComponent<PlayerAttack2D>();
            controller = GetComponent<PlayerController2D>();
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.Changed += HandleProgressionChanged;
            }

            HideLegacyVisuals();
            EnsureVisualInstance();
            ApplyClassVisibility();
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
            ApplyClassVisibility();
        }

        private void ApplyClassVisibility()
        {
            HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
            if (visualInstance != null)
            {
                visualInstance.SetActive(heroClass == HeroClass.Warrior);
            }

            HideLegacyVisuals();
        }

        private void OnEnable()
        {
            ApplyClassVisibility();
            if (IsWarriorClass())
            {
                EnsureVisualInstance();
            }
        }

        private static bool IsWarriorClass()
        {
            return RunProgressionSystem.Instance == null || RunProgressionSystem.Instance.CurrentClass == HeroClass.Warrior;
        }

        private void LateUpdate()
        {
            if (PlayerLifeState2D.Instance != null && PlayerLifeState2D.Instance.IsDeadOrDying)
            {
                return;
            }

            HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
            if (heroClass != HeroClass.Warrior)
            {
                ApplyClassVisibility();
                return;
            }

            EnsureVisualInstance();
            if (visualInstance == null)
            {
                return;
            }

            if (!visualInstance.activeSelf)
            {
                visualInstance.SetActive(true);
            }

            visualInstance.transform.localPosition = new Vector3(0f, 0f, VisualZOffset);
            visualInstance.transform.localScale = Vector3.one * VisualScale;
            FollowPlayer();
            ApplyFeetAlignment();
            UpdateFacing();
            UpdateAnimator();
        }

        private void EnsureVisualInstance()
        {
            if (visualInstance != null)
            {
                ApplyClassVisibility();
                return;
            }

            Transform existing = transform.Find(VisualInstanceName);
            if (existing != null)
            {
                visualInstance = existing.gameObject;
            }
            else
            {
                if (!IsWarriorClass())
                {
                    return;
                }

                GameObject prefab = Resources.Load<GameObject>(ResourcePath);
                if (prefab == null)
                {
                    if (!warnedMissingPrefab)
                    {
                        Debug.LogWarning("TinySwordsPlayerVisual2D: Missing Resources prefab at " + ResourcePath);
                        warnedMissingPrefab = true;
                    }

                    return;
                }

                visualInstance = Instantiate(prefab, transform);
                visualInstance.name = VisualInstanceName;
            }

            DisableVisualPhysics();
            CacheVisualComponents();
            CacheVisualAnchors();
            ApplyRendererSettings();
            HideLegacyVisuals();
            visualInstance.transform.localPosition = new Vector3(0f, 0f, VisualZOffset);
            visualInstance.transform.localScale = Vector3.one * VisualScale;
            ApplyClassVisibility();
        }

        private void CacheVisualAnchors()
        {
            feetAnchor = null;
            centerAnchor = null;
            if (visualInstance == null)
            {
                return;
            }

            feetAnchor = FindChildTransform(visualInstance.transform, "FeetPoint");
            centerAnchor = FindChildTransform(visualInstance.transform, "CenterPoint");
        }

        private static Transform FindChildTransform(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private void CacheVisualComponents()
        {
            animator = visualInstance.GetComponentInChildren<Animator>(true);
            spriteRenderers = visualInstance.GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void DisableVisualPhysics()
        {
            Rigidbody2D[] bodies = visualInstance.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].simulated = false;
            }

            Collider2D[] colliders = visualInstance.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void ApplyRendererSettings()
        {
            if (spriteRenderers == null)
            {
                return;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = VisualSortingOrder;
                renderer.enabled = renderer.sprite != null;
            }
        }

        private void FollowPlayer()
        {
            if (visualInstance == null)
            {
                return;
            }

            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one * VisualScale;
        }

        private void ApplyFeetAlignment()
        {
            if (visualInstance == null)
            {
                return;
            }

            visualInstance.transform.localPosition = new Vector3(0f, 0f, VisualZOffset);
            if (feetAnchor != null)
            {
                float correctionY = transform.position.y - feetAnchor.position.y;
                visualInstance.transform.localPosition = new Vector3(0f, correctionY, VisualZOffset);
                return;
            }

            PlayerVisualAlign2D.AlignVisualRootFeet(transform, visualInstance.transform, VisualZOffset);
        }

        private void UpdateFacing()
        {
            if (spriteRenderers == null)
            {
                return;
            }

            if (controller == null)
            {
                controller = GetComponent<PlayerController2D>();
            }

            Vector2 aimDirection = controller != null ? controller.AimDirection : Vector2.right;
            bool faceLeft = aimDirection.x < -0.01f;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].flipX = faceLeft;
                }
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (attack == null)
            {
                attack = GetComponent<PlayerAttack2D>();
            }

            Vector2 velocity = body != null ? body.velocity : Vector2.zero;
            bool isMoving = velocity.sqrMagnitude > MoveThreshold * MoveThreshold;
            animator.SetBool(IsMovingHash, isMoving);

            bool guard = false;
            RunProgressionSystem run = RunProgressionSystem.Instance;
            if (attack != null && attack.LastAttackTime > lastAttackTime + 0.001f)
            {
                lastAttackTime = attack.LastAttackTime;
                animator.SetTrigger(AttackHash);
            }
            else if (!RogueUiPause.BlocksGameplayInput && !GameStartMenuPanel.IsVisible && GameSettingsService.WasPressed("Attack")
                && (run == null || run.CurrentClass == HeroClass.Warrior))
            {
                animator.SetTrigger(AttackHash);
            }

            if (run != null)
            {
                guard = run.IsShielding;
            }

            if (!RogueUiPause.BlocksGameplayInput && !GameStartMenuPanel.IsVisible && GameSettingsService.IsHeld("Block"))
            {
                guard = true;
            }

            animator.SetBool(GuardHash, guard);
        }

        public void PlayHitFlash()
        {
            if (visualInstance == null)
            {
                return;
            }

            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(HitFlashRoutine());
        }

        public void PlayDeathPose()
        {
            if (visualInstance == null)
            {
                return;
            }

            deathPoseActive = true;
            visualBaseLocalPosition = visualInstance.transform.localPosition;
            visualBaseLocalRotation = visualInstance.transform.localRotation;
            visualInstance.transform.localRotation = visualBaseLocalRotation * Quaternion.Euler(0f, 0f, 86f);
            visualInstance.transform.localPosition = visualBaseLocalPosition + new Vector3(0f, -0.22f, 0f);
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        Color color = spriteRenderers[i].color;
                        spriteRenderers[i].color = new Color(0.55f, 0.55f, 0.62f, color.a);
                    }
                }
            }
        }

        public void ResetDeathPose()
        {
            deathPoseActive = false;
            if (visualInstance == null)
            {
                return;
            }

            visualInstance.transform.localRotation = visualBaseLocalRotation;
            visualInstance.transform.localPosition = visualBaseLocalPosition;
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        spriteRenderers[i].color = Color.white;
                    }
                }
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (spriteRenderers == null)
            {
                yield break;
            }

            if (cachedRendererColors == null || cachedRendererColors.Length != spriteRenderers.Length)
            {
                cachedRendererColors = new Color[spriteRenderers.Length];
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                cachedRendererColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = new Color(1f, 1f, 1f, cachedRendererColors[i].a);
                }
            }

            yield return new WaitForSeconds(0.07f);
            if (deathPoseActive)
            {
                hitFlashRoutine = null;
                yield break;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = Color.white;
                }
            }

            hitFlashRoutine = null;
        }

        private void HideLegacyVisuals()
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            Transform legacy = transform.Find("SKPlayerVisual");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(heroClass != HeroClass.Warrior);
            }

            if (visualInstance != null && heroClass != HeroClass.Warrior)
            {
                visualInstance.SetActive(false);
            }

            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                Color color = rootRenderer.color;
                color.a = 0f;
                rootRenderer.color = color;
                rootRenderer.enabled = false;
            }
        }
    }
}
