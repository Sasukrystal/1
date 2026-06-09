using System.Collections.Generic;
using UnityEngine;

namespace Bagsys.RogueLike
{
    public enum DirectedRoomType
    {
        Spawn,
        Combat,
        Event,
        Shop,
        Boss
    }

    [DisallowMultipleComponent]
    public class DungeonDirector : MonoBehaviour
    {
        [SerializeField] private bool disableLegacyDungeonGenerator = true;
        [SerializeField] private float roomSpacing = 34f;
        [SerializeField] private Vector3 roomSize = new Vector3(26f, 1f, 22f);

        private readonly List<DirectedRoom> rooms = new List<DirectedRoom>();

        private void Start()
        {
            BuildRun();
        }

        [ContextMenu("Build Directed Run")]
        public void BuildRun()
        {
            if (disableLegacyDungeonGenerator)
            {
                DungeonGenerator legacyGenerator = Object.FindObjectOfType<DungeonGenerator>();
                if (legacyGenerator != null && legacyGenerator.gameObject != gameObject)
                {
                    legacyGenerator.enabled = false;
                }
            }

            Transform oldRoot = transform.Find("DirectedDungeon");
            if (oldRoot != null)
            {
                Destroy(oldRoot.gameObject);
            }

            rooms.Clear();
            Transform root = new GameObject("DirectedDungeon").transform;
            root.SetParent(transform, false);

            DirectedRoomType[] route =
            {
                DirectedRoomType.Spawn,
                DirectedRoomType.Combat,
                DirectedRoomType.Event,
                DirectedRoomType.Shop,
                DirectedRoomType.Boss
            };

            for (int i = 0; i < route.Length; i++)
            {
                GameObject roomObject = new GameObject("Room_" + i + "_" + route[i]);
                roomObject.transform.SetParent(root, false);
                roomObject.transform.localPosition = new Vector3(i * roomSpacing, 0f, 0f);
                DirectedRoom room = roomObject.AddComponent<DirectedRoom>();
                room.Initialize(this, route[i], i, roomSize);
                rooms.Add(room);
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && rooms.Count > 0)
            {
                Vector3 spawn = rooms[0].transform.position + Vector3.up;
                player.transform.position = spawn;
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.position = spawn;
                    body.velocity = Vector3.zero;
                }
            }
        }

        public void OnBossDefeated()
        {
            CharacterProgressionPanel panel = CharacterProgressionPanel.Instance != null ? CharacterProgressionPanel.Instance : Object.FindObjectOfType<CharacterProgressionPanel>(true);
            if (panel != null)
            {
                panel.AddSoulShards(3);
            }
        }

        public void SpawnLootBurst(Vector3 position, int minCount, int maxCount)
        {
            if (LootManager.Instance != null)
            {
                LootManager.Instance.SpawnLoot(position, minCount, maxCount);
            }
        }
    }

    [DisallowMultipleComponent]
    public class DirectedRoom : MonoBehaviour
    {
        private DungeonDirector director;
        private DirectedRoomType roomType;
        private int roomIndex;
        private Vector3 roomSize;
        private readonly List<GameObject> gates = new List<GameObject>();
        private readonly List<EnemyStats> enemies = new List<EnemyStats>();
        private bool entered;
        private bool cleared;
        private bool rewardSpawned;

        public void Initialize(DungeonDirector owner, DirectedRoomType type, int index, Vector3 size)
        {
            director = owner;
            roomType = type;
            roomIndex = index;
            roomSize = size;
            BuildGeometry();
            cleared = roomType == DirectedRoomType.Spawn || roomType == DirectedRoomType.Shop || roomType == DirectedRoomType.Event;
            SetGates(false);
            if (roomType == DirectedRoomType.Event)
            {
                SpawnEventObject();
            }
            else if (roomType == DirectedRoomType.Shop)
            {
                SpawnShopObject();
            }
        }

        private void Update()
        {
            if (!entered || cleared)
            {
                return;
            }

            CleanupEnemyList();
            if (enemies.Count == 0)
            {
                ClearRoom();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || entered)
            {
                return;
            }

            entered = true;
            if (roomType == DirectedRoomType.Combat || roomType == DirectedRoomType.Boss)
            {
                SetGates(true);
                SpawnEncounter();
            }
        }

        private void BuildGeometry()
        {
            UnityEngine.Material floor = RuntimeVisualUtility.CreateMaterial("DirectedRoom_Floor", new Color(0.2f, 0.22f, 0.27f), 0f, 0.28f);
            UnityEngine.Material wall = RuntimeVisualUtility.CreateMaterial("DirectedRoom_Wall", new Color(0.08f, 0.09f, 0.12f), 0f, 0.2f);
            UnityEngine.Material accent = RuntimeVisualUtility.CreateMaterial("DirectedRoom_Accent", GetAccentColor(), 0f, 0.6f);

            RuntimeVisualUtility.CreatePrimitiveChild(transform, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), roomSize, floor, false);
            float halfX = roomSize.x * 0.5f;
            float halfZ = roomSize.z * 0.5f;
            CreateWall("NorthWall", new Vector3(0f, 1.35f, halfZ), new Vector3(roomSize.x, 3.2f, 0.8f), wall);
            CreateWall("SouthWall", new Vector3(0f, 1.35f, -halfZ), new Vector3(roomSize.x, 3.2f, 0.8f), wall);

            float doorHalf = 2.2f;
            float sideSegmentLength = halfZ - doorHalf;
            CreateWall("EastWallTop", new Vector3(halfX, 1.35f, doorHalf + sideSegmentLength * 0.5f), new Vector3(0.8f, 3.2f, sideSegmentLength), wall);
            CreateWall("EastWallBottom", new Vector3(halfX, 1.35f, -(doorHalf + sideSegmentLength * 0.5f)), new Vector3(0.8f, 3.2f, sideSegmentLength), wall);
            CreateWall("WestWallTop", new Vector3(-halfX, 1.35f, doorHalf + sideSegmentLength * 0.5f), new Vector3(0.8f, 3.2f, sideSegmentLength), wall);
            CreateWall("WestWallBottom", new Vector3(-halfX, 1.35f, -(doorHalf + sideSegmentLength * 0.5f)), new Vector3(0.8f, 3.2f, sideSegmentLength), wall);

            gates.Add(RuntimeVisualUtility.CreatePrimitiveChild(transform, "NorthGate", PrimitiveType.Cube, new Vector3(0f, 1.5f, halfZ - 0.25f), new Vector3(4.2f, 3.2f, 1.1f), accent, false));
            gates.Add(RuntimeVisualUtility.CreatePrimitiveChild(transform, "SouthGate", PrimitiveType.Cube, new Vector3(0f, 1.5f, -halfZ + 0.25f), new Vector3(4.2f, 3.2f, 1.1f), accent, false));
            gates.Add(RuntimeVisualUtility.CreatePrimitiveChild(transform, "EastGate", PrimitiveType.Cube, new Vector3(halfX - 0.25f, 1.5f, 0f), new Vector3(1.1f, 3.2f, 4.2f), accent, false));
            gates.Add(RuntimeVisualUtility.CreatePrimitiveChild(transform, "WestGate", PrimitiveType.Cube, new Vector3(-halfX + 0.25f, 1.5f, 0f), new Vector3(1.1f, 3.2f, 4.2f), accent, false));

            RuntimeVisualUtility.CreatePrimitiveChild(transform, "RoomSigil", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(2.5f, 0.04f, 2.5f), accent);

            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(roomSize.x * 0.82f, 3f, roomSize.z * 0.82f);
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale, UnityEngine.Material material)
        {
            GameObject wall = RuntimeVisualUtility.CreatePrimitiveChild(transform, name, PrimitiveType.Cube, position, scale, material, false);
            wall.GetComponent<Collider>().isTrigger = false;
        }

        private void SpawnEncounter()
        {
            enemies.Clear();
            if (roomType == DirectedRoomType.Boss)
            {
                enemies.Add(CreateEnemy(RogueEnemyTier.BossTitan, transform.position + new Vector3(0f, 1f, 2f)));
                return;
            }

            enemies.Add(CreateEnemy(RogueEnemyTier.Slime, transform.position + new Vector3(-4f, 1f, 1f)));
            enemies.Add(CreateEnemy(RogueEnemyTier.Slime, transform.position + new Vector3(0f, 1f, -2f)));
            enemies.Add(CreateEnemy(RogueEnemyTier.Slime, transform.position + new Vector3(4f, 1f, 1f)));
            enemies.Add(CreateEnemy(RogueEnemyTier.EliteThrower, transform.position + new Vector3(0f, 1f, 5f)));
        }

        private EnemyStats CreateEnemy(RogueEnemyTier tier, Vector3 position)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.SetParent(transform, true);
            enemy.transform.position = position;
            enemy.transform.localScale = tier == RogueEnemyTier.BossTitan ? Vector3.one * 1.55f : Vector3.one * 0.85f;
            TrySetTag(enemy, "Enemy");

            Rigidbody body = enemy.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = enemy.AddComponent<Rigidbody>();
            }

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            EnemyStats stats = enemy.AddComponent<EnemyStats>();
            if (tier == RogueEnemyTier.BossTitan)
            {
                stats.ConfigureBaseStats(240, 22, 4);
            }
            else if (tier == RogueEnemyTier.EliteThrower)
            {
                stats.ConfigureBaseStats(70, 12, 2);
            }
            else
            {
                stats.ConfigureBaseStats(42, 9, 1);
            }

            RuntimeCharacterVisual visual = enemy.AddComponent<RuntimeCharacterVisual>();
            visual.Configure(RuntimeCharacterVisualStyle.Enemy);
            enemy.AddComponent<ProceduralCharacterAnimator>().Configure(RuntimeCharacterVisualStyle.Enemy);
            RogueEnemyAgent agent = enemy.AddComponent<RogueEnemyAgent>();
            agent.Configure(tier);
            enemy.AddComponent<ModernRogue.SmoothWorldHealthBar>();
            return stats;
        }

        private void ClearRoom()
        {
            cleared = true;
            SetGates(false);
            if (roomType == DirectedRoomType.Boss)
            {
                director.OnBossDefeated();
            }

            SpawnRewardChest();
        }

        private void SpawnRewardChest()
        {
            if (rewardSpawned)
            {
                return;
            }

            rewardSpawned = true;
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = roomType == DirectedRoomType.Boss ? "BossRewardChest" : "ClearRewardChest";
            chest.transform.SetParent(transform, false);
            chest.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            chest.transform.localScale = new Vector3(1.25f, 1.1f, 1.25f);
            chest.GetComponent<Renderer>().sharedMaterial = RuntimeVisualUtility.CreateMaterial("RewardChest", new Color(0.9f, 0.62f, 0.22f), 0f, 0.42f);
            RewardChest rewardChest = chest.AddComponent<RewardChest>();
            rewardChest.Initialize(director, roomType == DirectedRoomType.Boss ? 5 : 3, roomType == DirectedRoomType.Boss);
        }

        private void SpawnEventObject()
        {
            GameObject shrine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shrine.name = "PrayerShrine";
            shrine.transform.SetParent(transform, false);
            shrine.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            shrine.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
            shrine.GetComponent<Renderer>().sharedMaterial = RuntimeVisualUtility.CreateMaterial("PrayerShrine", GetAccentColor(), 0f, 0.7f);
            shrine.AddComponent<EventShrine>().Initialize(director);
        }

        private void SpawnShopObject()
        {
            RuntimeVisualUtility.CreatePrimitiveChild(transform, "ShopCounter", PrimitiveType.Cube, new Vector3(0f, 0.65f, 2.8f), new Vector3(5f, 1.1f, 1.2f), RuntimeVisualUtility.CreateMaterial("ShopCounter", new Color(0.5f, 0.32f, 0.16f)), false);
        }

        private void SetGates(bool locked)
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].SetActive(locked);
                }
            }
        }

        private void CleanupEnemyList()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null || !enemies[i].gameObject.activeInHierarchy)
                {
                    enemies.RemoveAt(i);
                }
            }
        }

        private Color GetAccentColor()
        {
            switch (roomType)
            {
                case DirectedRoomType.Spawn: return new Color(0.3f, 0.62f, 1f);
                case DirectedRoomType.Event: return new Color(0.74f, 0.44f, 1f);
                case DirectedRoomType.Shop: return new Color(0.35f, 1f, 0.58f);
                case DirectedRoomType.Boss: return new Color(1f, 0.15f, 0.08f);
                default: return new Color(1f, 0.48f, 0.18f);
            }
        }

        private static void TrySetTag(GameObject target, string tagName)
        {
            try
            {
                target.tag = tagName;
            }
            catch
            {
                Debug.LogWarning("Tag '" + tagName + "' is missing.");
            }
        }
    }

    public class RewardChest : MonoBehaviour
    {
        private DungeonDirector director;
        private int lootCount;
        private bool createsPortal;
        private bool opened;
        private Transform player;

        public void Initialize(DungeonDirector owner, int count, bool portal)
        {
            director = owner;
            lootCount = count;
            createsPortal = portal;
        }

        private void Update()
        {
            if (opened)
            {
                return;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (player != null && (player.position - transform.position).sqrMagnitude <= 5f && Input.GetKeyDown(KeyCode.F))
            {
                Open();
            }
        }

        private void Open()
        {
            opened = true;
            if (director != null)
            {
                director.SpawnLootBurst(transform.position, lootCount, lootCount + 2);
            }

            if (createsPortal)
            {
                GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portal.name = "NextFloorPortal";
                portal.transform.position = transform.position + Vector3.forward * 2.2f;
                portal.transform.localScale = new Vector3(1.8f, 0.08f, 1.8f);
                portal.GetComponent<Renderer>().sharedMaterial = RuntimeVisualUtility.CreateMaterial("NextFloorPortal", new Color(0.2f, 0.55f, 1f), 0f, 0.8f);
                Light light = portal.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.2f, 0.55f, 1f);
                light.range = 8f;
                light.intensity = 1.8f;
            }

            Destroy(gameObject);
        }
    }

    public class EventShrine : MonoBehaviour
    {
        private DungeonDirector director;
        private bool used;

        public void Initialize(DungeonDirector owner)
        {
            director = owner;
        }

        private void Update()
        {
            if (used)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && (player.transform.position - transform.position).sqrMagnitude <= 6f && Input.GetKeyDown(KeyCode.F))
            {
                used = true;
                if (director != null)
                {
                    director.SpawnLootBurst(transform.position + Vector3.up * 0.2f, 1, 3);
                }

                CharacterProgressionPanel panel = CharacterProgressionPanel.Instance != null ? CharacterProgressionPanel.Instance : Object.FindObjectOfType<CharacterProgressionPanel>(true);
                if (panel != null)
                {
                    panel.AddSoulShards(1);
                }
            }
        }
    }
}
