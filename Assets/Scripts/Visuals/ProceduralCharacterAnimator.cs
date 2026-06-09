using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private RuntimeCharacterVisualStyle style = RuntimeCharacterVisualStyle.Player;
        [SerializeField] private float walkBobAmplitude = 0.055f;
        [SerializeField] private float walkBobFrequency = 8.5f;
        [SerializeField] private float leanAmount = 4.5f;
        [SerializeField] private float attackPulseDuration = 0.22f;

        private const string VisualRootName = "RuntimeStylizedVisual";

        private Transform visualRoot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform bladeMarker;
        private Transform directionCrest;
        private Vector3 lastPosition;
        private Vector3 rootStartPosition;
        private Quaternion rootStartRotation;
        private Quaternion leftArmStartRotation;
        private Quaternion rightArmStartRotation;
        private Quaternion bladeStartRotation;
        private Quaternion crestStartRotation;
        private PlayerAttack playerAttack;
        private EnemyAI enemyAI;

        public void Configure(RuntimeCharacterVisualStyle newStyle)
        {
            style = newStyle;
            CacheReferences(true);
        }

        private void Awake()
        {
            lastPosition = transform.position;
            playerAttack = GetComponent<PlayerAttack>();
            enemyAI = GetComponent<EnemyAI>();
            CacheReferences(true);
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            CacheReferences(true);
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
            {
                CacheReferences(true);
                if (visualRoot == null)
                {
                    return;
                }
            }

            Vector3 frameVelocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(frameVelocity, Vector3.up);
            float speed = Mathf.Clamp01(planarVelocity.magnitude / 5.5f);
            float phase = Time.time * walkBobFrequency;
            float bob = Mathf.Sin(phase) * walkBobAmplitude * speed;
            float sideSway = Mathf.Sin(phase * 0.5f) * 0.025f * speed;
            float attackPulse = ResolveAttackPulse();

            Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity.normalized * speed);
            float leanX = -localVelocity.z * leanAmount;
            float leanZ = localVelocity.x * leanAmount;

            visualRoot.localPosition = rootStartPosition + new Vector3(sideSway, bob + attackPulse * 0.04f, 0f);
            visualRoot.localRotation = rootStartRotation * Quaternion.Euler(leanX - attackPulse * 5f, 0f, leanZ);

            AnimateLimbs(speed, phase, attackPulse);
        }

        private void CacheReferences(bool resetStartPose)
        {
            visualRoot = transform.Find(VisualRootName);
            if (visualRoot == null)
            {
                return;
            }

            leftArm = visualRoot.Find("LeftArm");
            rightArm = visualRoot.Find("RightArm");
            bladeMarker = visualRoot.Find("BladeMarker");
            directionCrest = visualRoot.Find("DirectionCrest");

            if (!resetStartPose)
            {
                return;
            }

            rootStartPosition = visualRoot.localPosition;
            rootStartRotation = visualRoot.localRotation;
            leftArmStartRotation = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmStartRotation = rightArm != null ? rightArm.localRotation : Quaternion.identity;
            bladeStartRotation = bladeMarker != null ? bladeMarker.localRotation : Quaternion.identity;
            crestStartRotation = directionCrest != null ? directionCrest.localRotation : Quaternion.identity;
        }

        private float ResolveAttackPulse()
        {
            float lastAttackTime = -999f;
            if (playerAttack != null)
            {
                lastAttackTime = playerAttack.LastAttackTime;
            }
            else if (enemyAI != null)
            {
                lastAttackTime = enemyAI.LastAttackTime;
            }

            float age = Time.time - lastAttackTime;
            if (age < 0f || age > attackPulseDuration)
            {
                return 0f;
            }

            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, attackPulseDuration));
            return Mathf.Sin(t * Mathf.PI);
        }

        private void AnimateLimbs(float speed, float phase, float attackPulse)
        {
            float walkSwing = Mathf.Sin(phase) * 18f * speed;
            float enemyScale = style == RuntimeCharacterVisualStyle.Enemy ? 0.55f : 1f;

            if (leftArm != null)
            {
                leftArm.localRotation = leftArmStartRotation * Quaternion.Euler(walkSwing * enemyScale, 0f, -attackPulse * 22f);
            }

            if (rightArm != null)
            {
                rightArm.localRotation = rightArmStartRotation * Quaternion.Euler(-walkSwing * enemyScale - attackPulse * 58f, 0f, attackPulse * 28f);
            }

            if (bladeMarker != null)
            {
                bladeMarker.localRotation = bladeStartRotation * Quaternion.Euler(attackPulse * 88f, attackPulse * 18f, -attackPulse * 24f);
            }

            if (directionCrest != null)
            {
                directionCrest.localRotation = crestStartRotation * Quaternion.Euler(attackPulse * 18f, 0f, 0f);
            }
        }
    }
}
