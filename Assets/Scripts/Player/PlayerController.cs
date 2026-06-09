using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private bool cameraRelativeMovement = true;
        [SerializeField] private bool lockYPosition = true;

        [Header("Camera")]
        [SerializeField] private Camera followCamera;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 12f, -12f);
        [SerializeField] private float cameraFollowSmooth = 10f;
        [SerializeField] private bool lockCameraToPlayer = true;

        [Header("Facing")]
        [SerializeField] private string faceMarkerName = "FaceMarker";
        [SerializeField] private Vector3 faceMarkerLocalPosition = new Vector3(0f, 0.25f, 0.34f);
        [SerializeField] private Vector3 faceMarkerScale = new Vector3(0.12f, 0.12f, 0.12f);

        private Rigidbody playerRigidbody;
        private Vector3 moveInput;
        private Vector3 moveDirection;
        private Quaternion targetFacingRotation;
        private bool hasTargetFacingRotation;

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
            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (lockYPosition)
            {
                playerRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
            }

            if (followCamera == null && Camera.main != null)
            {
                followCamera = Camera.main;
            }

            if (followCamera != null && followCamera.GetComponent<AudioListener>() == null)
            {
                followCamera.gameObject.AddComponent<AudioListener>();
            }

            EnsureFaceMarker();
            SetSprite("Player_Front");
        }

        private void Update()
        {
            if (followCamera == null)
            {
                followCamera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
                if (followCamera != null && followCamera.GetComponent<AudioListener>() == null)
                {
                    followCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            moveInput = Vector3.ClampMagnitude(moveInput, 1f);

            if (cameraRelativeMovement && followCamera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(followCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(followCamera.transform.right, Vector3.up).normalized;

                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.forward;
                }

                if (right.sqrMagnitude < 0.001f)
                {
                    right = Vector3.right;
                }

                moveDirection = (forward * moveInput.z + right * moveInput.x).normalized;
            }
            else
            {
                moveDirection = moveInput.normalized;
            }

            UpdateFacingTarget();
        }

        private void FixedUpdate()
        {
            if (playerRigidbody == null)
            {
                return;
            }

            Vector3 targetPosition = playerRigidbody.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            playerRigidbody.MovePosition(targetPosition);

            if (hasTargetFacingRotation)
            {
                Quaternion nextRotation = Quaternion.Slerp(playerRigidbody.rotation, targetFacingRotation, turnSpeed * Time.fixedDeltaTime);
                playerRigidbody.MoveRotation(nextRotation);
            }
        }

        private void LateUpdate()
        {
            if (!lockCameraToPlayer || followCamera == null)
            {
                return;
            }

            Vector3 targetPosition = transform.position + cameraOffset;
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, targetPosition, cameraFollowSmooth * Time.deltaTime);
            followCamera.transform.LookAt(transform.position + Vector3.up * 0.8f);
        }

        private void UpdateFacingTarget()
        {
            Camera activeCamera = followCamera != null ? followCamera : Camera.main;
            if (activeCamera == null)
            {
                hasTargetFacingRotation = false;
                return;
            }

            Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = hitPoint - transform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    targetFacingRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                    hasTargetFacingRotation = true;
                    return;
                }
            }

            hasTargetFacingRotation = false;
        }

        private void EnsureFaceMarker()
        {
            Transform marker = transform.Find(faceMarkerName);
            if (marker == null)
            {
                marker = transform.Find("PlayerEye");
            }

            if (marker == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                markerObject.name = faceMarkerName;
                Object.Destroy(markerObject.GetComponent<Collider>());
                markerObject.transform.SetParent(transform, false);
                marker = markerObject.transform;
            }
            else if (marker.name != faceMarkerName)
            {
                marker.name = faceMarkerName;
            }

            marker.SetParent(transform, false);
            marker.localPosition = faceMarkerLocalPosition;
            marker.localScale = faceMarkerScale;

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.sharedMaterial = null;
                renderer.material.color = new Color(1f, 0.92f, 0.1f, 1f);
            }

        }
    }
}
