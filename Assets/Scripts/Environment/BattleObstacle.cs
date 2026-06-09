using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class BattleObstacle : MonoBehaviour
    {
        [SerializeField] private Vector3 size = new Vector3(1.5f, 2.5f, 1.5f);

        private Rigidbody obstacleRigidbody;
        private BoxCollider obstacleCollider;

        private void Awake()
        {
            obstacleCollider = GetComponent<BoxCollider>();
            obstacleRigidbody = GetComponent<Rigidbody>();

            if (obstacleRigidbody == null)
            {
                obstacleRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            obstacleRigidbody.isKinematic = true;
            obstacleRigidbody.useGravity = false;
            obstacleRigidbody.constraints = RigidbodyConstraints.FreezeAll;

            ApplySize(size);
            ApplyVisual();
        }

        public void Configure(Vector3 newSize, Color color)
        {
            size = newSize;
            ApplySize(size);
            ApplyVisual(color);
        }

        private void ApplySize(Vector3 newSize)
        {
            if (obstacleCollider == null)
            {
                obstacleCollider = GetComponent<BoxCollider>();
            }

            obstacleCollider.size = Vector3.one;
            transform.localScale = newSize;
        }

        private void ApplyVisual()
        {
            ApplyVisual(new Color(0.45f, 0.45f, 0.45f));
        }

        private void ApplyVisual(Color color)
        {
            GameObject visual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(transform, false);
                Object.Destroy(visual.GetComponent<Collider>());
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                UnityEngine.Material obstacleMaterial = Resources.Load<UnityEngine.Material>("Materials/DungeonStone");
                Texture2D obstacleTexture = Resources.Load<Texture2D>("Materials/DungeonBrick");

                if (obstacleMaterial != null)
                {
                    if (obstacleTexture != null)
                    {
                        obstacleMaterial.mainTexture = obstacleTexture;
                    }
                    else
                    {
                        obstacleMaterial.color = color;
                    }

                    renderer.sharedMaterial = obstacleMaterial;
                }
                else
                {
                    renderer.material.color = color;
                }
            }
        }
    }
}
