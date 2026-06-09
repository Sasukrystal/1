using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class DestructibleCrate : MonoBehaviour
    {
        [SerializeField] private int maxHp = 3;
        [SerializeField] private int minLootCount = 1;
        [SerializeField] private int maxLootCount = 2;

        private int currentHp;
        private bool isBroken;
        private BoxCollider crateCollider;
        private Rigidbody crateRigidbody;

        private void Awake()
        {
            currentHp = maxHp;
            crateCollider = GetComponent<BoxCollider>();
            crateRigidbody = GetComponent<Rigidbody>();

            if (crateRigidbody == null)
            {
                crateRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            crateRigidbody.isKinematic = true;
            crateRigidbody.useGravity = false;
            crateRigidbody.constraints = RigidbodyConstraints.FreezeAll;

            ApplyVisual();
        }

        public void TakeDamage(int damage)
        {
            if (isBroken)
            {
                return;
            }

            currentHp -= Mathf.Max(1, damage);
            if (currentHp > 0)
            {
                return;
            }

            BreakCrate();
        }

        private void BreakCrate()
        {
            if (isBroken)
            {
                return;
            }

            isBroken = true;

            if (LootManager.Instance != null)
            {
                LootManager.Instance.SpawnLoot(transform.position, minLootCount, maxLootCount);
            }

            Destroy(gameObject);
        }

        private void ApplyVisual()
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
                UnityEngine.Material crateMaterial = Resources.Load<UnityEngine.Material>("Materials/LootGold");
                Texture2D crateTexture = Resources.Load<Texture2D>("Materials/LootGold");

                if (crateMaterial != null)
                {
                    if (crateTexture != null)
                    {
                        crateMaterial.mainTexture = crateTexture;
                    }

                    renderer.sharedMaterial = crateMaterial;
                }
                else
                {
                    renderer.material.color = new Color(0.74f, 0.56f, 0.28f);
                }
            }
        }
    }
}
