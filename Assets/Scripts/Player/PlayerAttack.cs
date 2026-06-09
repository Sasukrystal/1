using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] private float attackCooldown = 0.3f;
        [SerializeField] private float attackRadius = 1.5f;
        [SerializeField] private float attackForwardOffset = 1.2f;
        [SerializeField] private float attackArcAngle = 80f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifetime = 2.5f;

        private PlayerStats playerStats;
        private float nextAttackTime;
        private GameObject projectilePrefab;

        public float LastAttackTime { get; private set; } = -999f;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            EnsureProjectilePrefab();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            PerformAttack();
        }

        private void PerformAttack()
        {
            LastAttackTime = Time.time;

            Weapon currentWeapon = GetEquippedWeapon();
            Weapon.WeaponType weaponType = currentWeapon != null ? currentWeapon.WpType : Weapon.WeaponType.Dagger;

            if (IsRangedWeapon(weaponType))
            {
                FireProjectile(currentWeapon);
                return;
            }

            PerformMeleeAttack(currentWeapon);
        }

        private void PerformMeleeAttack(Weapon currentWeapon)
        {
            Vector3 attackCenter = transform.position + transform.forward * attackForwardOffset;
            Collider[] hitColliders = Physics.OverlapSphere(attackCenter, attackRadius);
            float halfArc = attackArcAngle * 0.5f;
            int attackDamage = ResolveAttackDamage(currentWeapon);

            CombatVfxUtility.SpawnMeleeArc(transform.position + Vector3.up * 0.18f, transform.forward, attackForwardOffset + attackRadius * 0.55f, attackArcAngle, new Color(1f, 0.86f, 0.15f, 1f), 0.14f);

            for (int i = 0; i < hitColliders.Length; i++)
            {
                Collider hitCollider = hitColliders[i];
                if (hitCollider == null)
                {
                    continue;
                }

                EnemyStats enemyStats = hitCollider.GetComponentInParent<EnemyStats>();
                if (enemyStats != null)
                {
                    Vector3 toTarget = enemyStats.transform.position - transform.position;
                    toTarget.y = 0f;

                    if (toTarget.sqrMagnitude <= 0.0001f)
                    {
                        continue;
                    }

                    float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                    if (angle <= halfArc)
                    {
                        enemyStats.TakeDamage(attackDamage, transform.position);
                    }

                    continue;
                }

                DestructibleCrate destructibleCrate = hitCollider.GetComponentInParent<DestructibleCrate>();
                if (destructibleCrate != null)
                {
                    Vector3 toTarget = destructibleCrate.transform.position - transform.position;
                    toTarget.y = 0f;

                    if (toTarget.sqrMagnitude <= 0.0001f)
                    {
                        continue;
                    }

                    float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                    if (angle <= halfArc)
                    {
                        destructibleCrate.TakeDamage(attackDamage);
                    }
                }

                ModernRogue.LoopRewardChest rewardChest = hitCollider.GetComponentInParent<ModernRogue.LoopRewardChest>();
                if (rewardChest != null)
                {
                    Vector3 toTarget = rewardChest.transform.position - transform.position;
                    toTarget.y = 0f;

                    if (toTarget.sqrMagnitude <= 0.0001f)
                    {
                        continue;
                    }

                    float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                    if (angle <= halfArc)
                    {
                        rewardChest.TakeDamage(attackDamage);
                    }
                }
            }
        }

        private void FireProjectile(Weapon currentWeapon)
        {
            if (projectilePrefab == null)
            {
                EnsureProjectilePrefab();
            }

            if (projectilePrefab == null)
            {
                return;
            }

            GameObject projectileObject = Instantiate(projectilePrefab, transform.position + transform.forward * 1.2f + Vector3.up * 0.9f, Quaternion.LookRotation(transform.forward, Vector3.up));
            projectileObject.SetActive(true);

            PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(ResolveAttackDamage(currentWeapon), transform.forward, projectileSpeed, projectileLifetime);
            }
        }

        private int ResolveAttackDamage(Weapon currentWeapon)
        {
            int baseDamage = playerStats != null ? playerStats.ATK : 1;
            if (currentWeapon != null)
            {
                baseDamage += Mathf.Max(0, currentWeapon.Damage);
            }

            return Mathf.Max(1, baseDamage);
        }

        private static bool IsRangedWeapon(Weapon.WeaponType weaponType)
        {
            return weaponType == Weapon.WeaponType.Wand;
        }

        private Weapon GetEquippedWeapon()
        {
            CharacterPanel panel = CharacterPanel.Instance;
            if (panel == null)
            {
                return null;
            }

            EquipmentSlot[] equipmentSlots = panel.GetComponentsInChildren<EquipmentSlot>(true);
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                EquipmentSlot slot = equipmentSlots[i];
                if (slot == null || slot.transform.childCount <= 0)
                {
                    continue;
                }

                ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
                if (itemUI != null && itemUI.Item is Weapon weapon)
                {
                    return weapon;
                }
            }

            return null;
        }

        private void EnsureProjectilePrefab()
        {
            if (projectilePrefab != null)
            {
                return;
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "RuntimeProjectilePrefab";
            Object.Destroy(projectileObject.GetComponent<Collider>());

            SphereCollider sphereCollider = projectileObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.18f;

            Rigidbody rigidbody = projectileObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            PlayerProjectile projectile = projectileObject.AddComponent<PlayerProjectile>();

            Renderer renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CombatVfxUtility.CreateSolidMaterial(new Color(1f, 0.88f, 0.18f, 1f));
            }

            Light projectileLight = projectileObject.GetComponent<Light>();
            if (projectileLight == null)
            {
                projectileLight = projectileObject.AddComponent<Light>();
            }

            projectileLight.type = LightType.Point;
            projectileLight.color = new Color(1f, 0.92f, 0.2f, 1f);
            projectileLight.range = 3f;
            projectileLight.intensity = 2f;

            projectileObject.SetActive(false);
            projectileObject.hideFlags = HideFlags.HideAndDontSave;
            projectilePrefab = projectileObject;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 center = transform.position + transform.forward * attackForwardOffset;
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}
