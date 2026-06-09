using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        private int damage = 8;
        private float lifetime = 4f;
        private bool impacted;

        public void Initialize(Vector3 direction, float speed, int damageAmount, float lifeSeconds)
        {
            damage = Mathf.Max(1, damageAmount);
            lifetime = Mathf.Max(0.5f, lifeSeconds);

            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.24f;

            Rigidbody body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.velocity = direction.normalized * speed;

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (impacted || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            impacted = true;
            CharacterStats stats = other.GetComponentInParent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
            }

            CombatVfxUtility.SpawnProjectileImpact(transform.position);
            Destroy(gameObject);
        }
    }
}
