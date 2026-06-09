using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("AI")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float detectRange = 10f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.2f;

        private Rigidbody enemyRigidbody;
        private Transform target;
        private CharacterStats targetStats;
        private float nextAttackTime;

        public float LastAttackTime { get; private set; } = -999f;

        public T LoadAssetFromResources<T>(string assetName) where T : Object
        {
            return RuntimeArtBinder.LoadAssetFromResources<T>(assetName);
        }

        public void SetSprite(string path)
        {
            RuntimeArtBinder binder = GetComponent<RuntimeArtBinder>();
            if (binder == null)
            {
                binder = gameObject.AddComponent<RuntimeArtBinder>();
            }

            binder.SetSprite(path);
        }

        private void Awake()
        {
            enemyRigidbody = GetComponent<Rigidbody>();
            enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            enemyRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            SetSprite("Enemy_Slime");
        }

        private void Update()
        {
            if (target == null)
            {
                FindTarget();
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance > detectRange)
            {
                return;
            }

            if (distance <= attackRange)
            {
                TryAttack();
                return;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 nextPosition = enemyRigidbody.position + direction * moveSpeed * Time.fixedDeltaTime;
            enemyRigidbody.MovePosition(nextPosition);

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            enemyRigidbody.MoveRotation(Quaternion.Slerp(enemyRigidbody.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }

        private void FindTarget()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                return;
            }

            target = playerObject.transform;
            targetStats = playerObject.GetComponent<CharacterStats>();
        }

        private void TryAttack()
        {
            if (targetStats == null || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            LastAttackTime = Time.time;
            CombatVfxUtility.SpawnMeleeArc(transform.position + Vector3.up * 0.18f, transform.forward, 1.25f, 55f, new Color(1f, 0.2f, 0.15f, 1f), 0.11f);
            targetStats.TakeDamage(5);
        }
    }
}
