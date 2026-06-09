using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.4f;
        [SerializeField] private float dodgeSpeed = 13.5f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.55f;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private Vector2 dodgeDirection;
        private float dodgeEndTime;
        private float nextDodgeTime;
        private PlayerClassVfx2D classVfx;

        public bool IsDodging => Time.time < dodgeEndTime;
        public Vector2 AimDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            classVfx = GetComponent<PlayerClassVfx2D>();
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
                moveInput = Vector2.zero;
                return;
            }

            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            UpdateAimDirection();
            if (GameSettingsService.WasPressed("Dodge"))
            {
                StartDodge();
            }
        }

        private void FixedUpdate()
        {
            if (PlayerLifeState2D.Instance != null && PlayerLifeState2D.Instance.IsDeadOrDying)
            {
                return;
            }

            float speedMultiplier = NewInventorySystem.Instance != null ? NewInventorySystem.Instance.GetMoveSpeedMultiplier() : 1f;
            Vector2 velocity = IsDodging ? dodgeDirection * dodgeSpeed : moveInput * moveSpeed * speedMultiplier;
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
            body.MoveRotation(0f);
        }

        private void UpdateAimDirection()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            AimDirection = PlayerAim2D.ResolveAimDirection(transform);
        }

        private void StartDodge()
        {
            RunProgressionSystem run = RunProgressionSystem.Instance;
            HeroClass heroClass = run != null ? run.CurrentClass : HeroClass.Warrior;
            bool freeWindCharge = false;
            if (Time.time < nextDodgeTime)
            {
                freeWindCharge = ElementalCoreCombatSystem.Instance != null && ElementalCoreCombatSystem.Instance.TryConsumeWindDodgeCharge();
                if (!freeWindCharge)
                {
                    return;
                }
            }

            if (!freeWindCharge && heroClass == HeroClass.Mage && run != null && !run.TrySpendMana(12f))
            {
                return;
            }

            dodgeDirection = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : AimDirection;
            float duration = heroClass == HeroClass.Archer ? dodgeDuration * 1.35f : dodgeDuration;
            float cooldown = heroClass == HeroClass.Mage ? 0.08f : dodgeCooldown;
            float speed = heroClass == HeroClass.Archer ? dodgeSpeed * 1.16f : dodgeSpeed;
            dodgeEndTime = Time.time + duration;
            nextDodgeTime = freeWindCharge ? Time.time : Time.time + cooldown;
            if (heroClass == HeroClass.Archer)
            {
                dodgeDirection *= speed / dodgeSpeed;
            }

            if (NewInventorySystem.Instance != null && NewInventorySystem.Instance.HasWindDashShockwave())
            {
                DashShockwave();
            }

            if (ElementalCoreCombatSystem.Instance != null)
            {
                ElementalCoreCombatSystem.Instance.OnPlayerDodge();
            }

            if (classVfx != null)
            {
                if (heroClass == HeroClass.Mage)
                {
                    classVfx.ShowMageDodge(dodgeDirection);
                }
                else if (heroClass == HeroClass.Archer)
                {
                    classVfx.ShowArcherDodge(dodgeDirection);
                }
                else
                {
                    classVfx.ShowWarriorDodge(dodgeDirection);
                }
            }

            if (heroClass == HeroClass.Warrior || heroClass == HeroClass.Archer)
            {
                GameAudioService.Ensure()?.PlayDodgeRoll();
            }
        }

        private void DashShockwave()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.8f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].CompareTag("Enemy"))
                {
                    continue;
                }

                Bagsys.RogueLike.EnemyStats enemy = hits[i].GetComponentInParent<Bagsys.RogueLike.EnemyStats>();
                if (enemy != null)
                {
                    enemy.TakeDamage(4, transform.position);
                }
            }
        }
    }
}
