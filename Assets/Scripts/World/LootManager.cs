using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class LootManager : MonoBehaviour
    {
        public static LootManager Instance { get; private set; }

        [Header("Drop Prefab")]
        [SerializeField] private GameObject dropItemPrefab;

        [Header("Spawn")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        public GameObject DropItemPrefab => dropItemPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDropPrefab();
        }

        public void SetDropPrefab(GameObject prefab)
        {
            dropItemPrefab = prefab;
        }

        public void SpawnRandomDropBurst(Vector3 position, int minCount, int maxCount)
        {
            EnsureDropPrefab();
            if (dropItemPrefab == null)
            {
                Debug.LogWarning("LootManager: dropItemPrefab is not assigned.");
                return;
            }

            int dropCount = Random.Range(minCount, maxCount + 1);
            for (int i = 0; i < dropCount; i++)
            {
                int itemId = Random.Range(1, 19);
                Vector3 spawnPosition = position + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.35f, 0.35f), 0f);
                SpawnDropItem(itemId, 1, spawnPosition);
            }
        }

        public void SpawnLoot(Vector3 position, int minCount = 1, int maxCount = 2)
        {
            SpawnRandomDropBurst(position, minCount, maxCount);
        }

        public GameObject SpawnDropItem(int itemId, int count, Vector3 position)
        {
            EnsureDropPrefab();
            if (dropItemPrefab == null)
            {
                Debug.LogWarning("LootManager: dropItemPrefab is not assigned.");
                return null;
            }

            GameObject dropInstance = Instantiate(dropItemPrefab, position + spawnOffset, Quaternion.identity);
            dropInstance.hideFlags = HideFlags.None;
            dropInstance.SetActive(true);

            WorldItem worldItem = dropInstance.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.Initialize(itemId, count);
            }

            Rigidbody2D body = dropInstance.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = dropInstance.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.velocity = Random.insideUnitCircle * 1.5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            return dropInstance;
        }

        private void EnsureDropPrefab()
        {
            if (dropItemPrefab != null)
            {
                return;
            }

            GameObject runtimePrefab = new GameObject("RuntimeDropItemPrefab", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D));
            runtimePrefab.name = "RuntimeDropItemPrefab";
            runtimePrefab.transform.localScale = new Vector3(0.34f, 0.34f, 1f);

            SpriteRenderer renderer = runtimePrefab.GetComponent<SpriteRenderer>();
            renderer.sprite = global::ModernRogue.Art2DUtility.LoadSprite("Weapon_Dagger");
            renderer.color = new Color(1f, 0.78f, 0.16f, 1f);
            renderer.sortingOrder = 7;

            CircleCollider2D collider = runtimePrefab.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;

            if (runtimePrefab.GetComponent<WorldItem>() == null)
            {
                runtimePrefab.AddComponent<WorldItem>();
            }

            Rigidbody2D rigidbody = runtimePrefab.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            runtimePrefab.SetActive(false);
            runtimePrefab.hideFlags = HideFlags.HideAndDontSave;
            dropItemPrefab = runtimePrefab;
        }
    }
}
