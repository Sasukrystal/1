using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class WorldItem : MonoBehaviour
    {
        [Header("Item Stack")]
        [SerializeField] private int itemId;
        [SerializeField] private int count = 1;

        [Header("Pickup")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode pickupKey = KeyCode.F;
        [SerializeField] private bool autoPickupOnTriggerEnter;
        [SerializeField] private float pickupRange = 1.5f;

        private bool playerInRange;
        private Transform playerTransform;

        public int ItemId => itemId;
        public int Count => count;

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

        public void Initialize(int newItemId, int newCount)
        {
            itemId = newItemId;
            count = Mathf.Max(1, newCount);
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = global::ModernRogue.Art2DUtility.LoadSprite("Weapon_Dagger");
            spriteRenderer.color = new Color(1f, 0.78f, 0.16f, 1f);
            spriteRenderer.sortingOrder = 7;
        }

        private void Reset()
        {
            EnsureCollider();
        }

        private void Update()
        {
            CachePlayerTransform();

            if (!playerInRange && playerTransform != null)
            {
                float rangeSqr = pickupRange * pickupRange;
                playerInRange = (playerTransform.position - transform.position).sqrMagnitude <= rangeSqr;
            }

            if (playerInRange && Input.GetKeyDown(pickupKey))
            {
                TryPickup();
            }
        }

        private void EnsureCollider()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D == null)
            {
                collider2D = gameObject.AddComponent<CircleCollider2D>();
            }

            collider2D.isTrigger = true;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void CachePlayerTransform()
        {
            if (playerTransform != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            playerTransform = other.transform;
            playerInRange = true;

            if (autoPickupOnTriggerEnter)
            {
                TryPickup();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInRange = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            playerTransform = other.transform;
            playerInRange = true;

            if (autoPickupOnTriggerEnter)
            {
                TryPickup();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInRange = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider == null || !collision.collider.CompareTag(playerTag))
            {
                return;
            }

            playerTransform = collision.transform;
            playerInRange = true;

            if (autoPickupOnTriggerEnter)
            {
                TryPickup();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider != null && collision.collider.CompareTag(playerTag))
            {
                playerInRange = false;
            }
        }

        private bool TryPickup()
        {
            if (count <= 0)
            {
                Destroy(gameObject);
                return true;
            }

            if (global::ModernRogue.NewInventorySystem.Instance != null)
            {
                if (!global::ModernRogue.NewInventorySystem.Instance.AddItem(itemId, count))
                {
                    Debug.Log("WorldItem: modern inventory is full or item id is invalid, pickup aborted.");
                    return false;
                }

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.RecordRecentPickup(itemId, count);
                }

                Destroy(gameObject);
                return true;
            }

            if (global::Knapsack.Instance == null)
            {
                Debug.Log("WorldItem: no inventory system is available.");
                return false;
            }

            int storedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (!global::Knapsack.Instance.StoreItem(itemId))
                {
                    break;
                }

                storedCount++;
            }

            if (storedCount <= 0)
            {
                Debug.Log("WorldItem: inventory is full, pickup aborted.");
                return false;
            }

            count -= storedCount;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RecordRecentPickup(itemId, storedCount);
            }

            if (count <= 0)
            {
                Destroy(gameObject);
                return true;
            }

            return false;
        }
    }
}
