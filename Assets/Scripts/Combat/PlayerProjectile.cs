using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerProjectile : MonoBehaviour
    {
        [SerializeField] private float defaultLifetime = 2.5f;

        private Rigidbody projectileRigidbody;
        private int damage;
        private bool isInitialized;
        private bool hasImpacted;
        private TrailRenderer trailRenderer;
        private Light projectileLight;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileRigidbody.useGravity = false;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;

            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }

            trailRenderer.time = 0.18f;
            trailRenderer.startWidth = 0.18f;
            trailRenderer.endWidth = 0.02f;
            trailRenderer.minVertexDistance = 0.01f;
            trailRenderer.material = CombatVfxUtility.CreateSolidMaterial(new Color(1f, 0.9f, 0.2f, 1f));
            trailRenderer.startColor = new Color(1f, 0.92f, 0.18f, 0.9f);
            trailRenderer.endColor = new Color(1f, 0.78f, 0.12f, 0f);

            projectileLight = GetComponent<Light>();
            if (projectileLight == null)
            {
                projectileLight = gameObject.AddComponent<Light>();
            }

            projectileLight.type = LightType.Point;
            projectileLight.color = new Color(1f, 0.92f, 0.2f, 1f);
            projectileLight.range = 3.2f;
            projectileLight.intensity = 1.8f;
        }

        public void Initialize(int damageAmount, Vector3 direction, float speed, float lifetime)
        {
            damage = Mathf.Max(1, damageAmount);
            isInitialized = true;
            if (projectileRigidbody == null)
            {
                projectileRigidbody = GetComponent<Rigidbody>();
            }

            projectileRigidbody.velocity = direction.normalized * speed;
            projectileRigidbody.angularVelocity = Vector3.zero;
            Destroy(gameObject, lifetime > 0f ? lifetime : defaultLifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || hasImpacted || other == null)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                return;
            }

            hasImpacted = true;
            Vector3 impactPoint = other.ClosestPoint(transform.position);
            CombatVfxUtility.SpawnProjectileImpact(impactPoint);

            EnemyStats enemyStats = other.GetComponentInParent<EnemyStats>();
            if (enemyStats != null)
            {
                enemyStats.TakeDamage(damage, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
