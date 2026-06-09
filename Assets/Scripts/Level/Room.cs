using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagsys.RogueLike
{
    public enum RoomType
    {
        Initial,
        Battle,
        Shop,
        Boss
    }

    public enum RoomState
    {
        Idle,
        Combat,
        Cleared
    }

    [DisallowMultipleComponent]
    public class Room : MonoBehaviour
    {
        [Header("Room Shape")]
        [SerializeField] private Vector3 roomSize = new Vector3(25f, 1f, 25f);
        [SerializeField] private float wallHeight = 4f;
        [SerializeField] private float wallThickness = 1f;
        [SerializeField] private float doorWidth = 4f;
        [SerializeField] private float doorDepth = 1.2f;

        [Header("Battle Setup")]
        [SerializeField] private int battleEnemyMin = 3;
        [SerializeField] private int battleEnemyMax = 5;
        [SerializeField] private int obstacleCount = 4;
        [SerializeField] private int crateCount = 1;

        [Header("Door Animation")]
        [SerializeField] private float doorAnimationDuration = 0.22f;
        [SerializeField] private float doorOpenLift = 3.2f;
        [SerializeField] private float doorAudioPitch = 1f;

        [Header("Room Data")]
        public RoomType roomType = RoomType.Battle;
        public Transform[] doorAnchors;
        public bool isCleared;

        private RoomState roomState = RoomState.Idle;
        private Transform generatedRoot;
        private Transform wallsRoot;
        private Transform gatesRoot;
        private Transform propsRoot;
        private Transform accentsRoot;
        private BoxCollider roomTrigger;
        private AudioSource roomAudioSource;
        private readonly List<GameObject> gates = new List<GameObject>();
        private readonly List<GameObject> spawnedBattleObjects = new List<GameObject>();
        private readonly List<Vector3> closedGatePositions = new List<Vector3>();
        private readonly List<Vector3> openGatePositions = new List<Vector3>();
        private Coroutine doorAnimationRoutine;
        private bool doorsLocked;
        private bool rewardSpawned;

        public RoomState State => roomState;
        public Vector3 RoomSize => roomSize;
        public Vector3 RoomCenter => transform.position;

        private void Awake()
        {
            BuildRuntimeLayout();
        }

        private void Start()
        {
            if (roomType == RoomType.Initial)
            {
                SetCleared(true);
                SetDoorsActive(false);
            }
        }

        private void Update()
        {
            if (roomState == RoomState.Combat)
            {
                CleanupSpawnedObjects();
                if (GetAliveEnemyCount() <= 0)
                {
                    ClearRoom();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            OnPlayerEnter(other.gameObject);
        }

        public void Initialize(RoomType type, bool isStartRoom)
        {
            roomType = type;
            ApplyRoomAccents();
            if (isStartRoom)
            {
                SetCleared(true);
                SetDoorsActive(false);
            }
        }

        public void OnPlayerEnter(GameObject player)
        {
            if (roomState != RoomState.Idle)
            {
                return;
            }

            if (roomType == RoomType.Battle || roomType == RoomType.Boss)
            {
                StartBattle();
            }
        }

        public void SetCleared(bool cleared)
        {
            isCleared = cleared;
            roomState = cleared ? RoomState.Cleared : RoomState.Idle;
        }

        public void SetDoorsActive(bool active)
        {
            doorsLocked = active;

            if (doorAnimationRoutine != null)
            {
                StopCoroutine(doorAnimationRoutine);
            }

            PlayDoorTone(active);
            doorAnimationRoutine = StartCoroutine(AnimateDoors(active));
        }

        public Vector3 GetRandomBattlePoint()
        {
            float xMin = -roomSize.x * 0.35f;
            float xMax = roomSize.x * 0.35f;
            float zMin = -roomSize.z * 0.35f;
            float zMax = roomSize.z * 0.35f;

            for (int i = 0; i < 20; i++)
            {
                Vector3 candidate = RoomCenter + new Vector3(Random.Range(xMin, xMax), 1f, Random.Range(zMin, zMax));
                if (Vector3.Distance(candidate, RoomCenter) > 3f)
                {
                    return candidate;
                }
            }

            return RoomCenter + new Vector3(0f, 1f, 3f);
        }

        public Transform GetDoorAnchor(int index)
        {
            if (doorAnchors == null || index < 0 || index >= doorAnchors.Length)
            {
                return null;
            }

            return doorAnchors[index];
        }

        private void StartBattle()
        {
            roomState = RoomState.Combat;
            SetDoorsActive(true);
            SpawnBattleProps();
            SpawnEnemies();
        }

        private void ClearRoom()
        {
            SetCleared(true);
            SetDoorsActive(false);
            roomState = RoomState.Cleared;
            SpawnClearReward();
            ShowRoomClearedMessage();
            Debug.Log("房间已清理");
        }

        private void SpawnEnemies()
        {
            int enemyCount = Random.Range(battleEnemyMin, battleEnemyMax + 1);
            for (int i = 0; i < enemyCount; i++)
            {
                Vector3 worldPosition = GetRandomBattlePoint();
                GameObject enemyObject = CreateEnemy(worldPosition);
                if (enemyObject != null)
                {
                    spawnedBattleObjects.Add(enemyObject);
                }
            }
        }

        private GameObject CreateEnemy(Vector3 worldPosition)
        {
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "TestEnemy";
            enemyObject.transform.SetParent(transform, true);
            enemyObject.transform.position = worldPosition;
            enemyObject.transform.localScale = Vector3.one * 0.85f;

            Rigidbody rigidbody = enemyObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = enemyObject.AddComponent<Rigidbody>();
            }

            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            if (enemyObject.GetComponent<EnemyAI>() == null)
            {
                enemyObject.AddComponent<EnemyAI>();
            }

            if (enemyObject.GetComponent<EnemyStats>() == null)
            {
                enemyObject.AddComponent<EnemyStats>();
            }

            RuntimeCharacterVisual enemyVisual = enemyObject.GetComponent<RuntimeCharacterVisual>();
            if (enemyVisual == null)
            {
                enemyVisual = enemyObject.AddComponent<RuntimeCharacterVisual>();
            }

            enemyVisual.Configure(RuntimeCharacterVisualStyle.Enemy);

            ProceduralCharacterAnimator enemyAnimator = enemyObject.GetComponent<ProceduralCharacterAnimator>();
            if (enemyAnimator == null)
            {
                enemyAnimator = enemyObject.AddComponent<ProceduralCharacterAnimator>();
            }

            enemyAnimator.Configure(RuntimeCharacterVisualStyle.Enemy);

            ApplyRoomMaterial(enemyObject, "Materials/EnemyRed", new Color(0.85f, 0.2f, 0.2f));
            TrySetTag(enemyObject, "Enemy");
            return enemyObject;
        }

        private void SpawnBattleProps()
        {
            CleanupPropsRoot();

            int targetObstacleCount = Mathf.Max(2, obstacleCount);
            for (int i = 0; i < targetObstacleCount; i++)
            {
                Vector3 localPosition = new Vector3(Random.Range(-roomSize.x * 0.25f, roomSize.x * 0.25f), 0.5f, Random.Range(-roomSize.z * 0.25f, roomSize.z * 0.25f));
                GameObject obstacle = new GameObject($"BattleObstacle_{i}");
                obstacle.transform.SetParent(propsRoot, false);
                obstacle.transform.localPosition = localPosition;
                obstacle.AddComponent<BattleObstacle>().Configure(new Vector3(1.5f, 2.5f, 1.5f), new Color(0.45f, 0.45f, 0.45f));
                spawnedBattleObjects.Add(obstacle);
            }

            for (int i = 0; i < crateCount; i++)
            {
                Vector3 localPosition = new Vector3(Random.Range(-roomSize.x * 0.28f, roomSize.x * 0.28f), 0.5f, Random.Range(-roomSize.z * 0.28f, roomSize.z * 0.28f));
                GameObject crate = new GameObject($"DestructibleCrate_{i}");
                crate.transform.SetParent(propsRoot, false);
                crate.transform.localPosition = localPosition;
                crate.AddComponent<DestructibleCrate>();
                spawnedBattleObjects.Add(crate);
            }
        }

        private int GetAliveEnemyCount()
        {
            EnemyStats[] enemyStats = GetComponentsInChildren<EnemyStats>(true);
            int aliveCount = 0;
            for (int i = 0; i < enemyStats.Length; i++)
            {
                if (enemyStats[i] != null && enemyStats[i].gameObject.activeInHierarchy)
                {
                    aliveCount++;
                }
            }

            return aliveCount;
        }

        private void CleanupSpawnedObjects()
        {
            for (int i = spawnedBattleObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedBattleObjects[i] == null)
                {
                    spawnedBattleObjects.RemoveAt(i);
                }
            }
        }

        private void CleanupPropsRoot()
        {
            if (propsRoot == null)
            {
                return;
            }

            for (int i = propsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = propsRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void BuildRuntimeLayout()
        {
            if (generatedRoot != null)
            {
                return;
            }

            generatedRoot = new GameObject("_RuntimeRoom").transform;
            generatedRoot.SetParent(transform, false);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(generatedRoot, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(roomSize.x, roomSize.y, roomSize.z);

            wallsRoot = new GameObject("Walls").transform;
            wallsRoot.SetParent(generatedRoot, false);

            gatesRoot = new GameObject("Gates").transform;
            gatesRoot.SetParent(generatedRoot, false);

            propsRoot = new GameObject("Props").transform;
            propsRoot.SetParent(generatedRoot, false);

            accentsRoot = new GameObject("Accents").transform;
            accentsRoot.SetParent(generatedRoot, false);

            EnsureAudioSource();
            BuildWallsAndGates();
            BuildGrayboxFeatures();
            BuildRoomTrigger();
            ApplyDungeonVisual(floor);
            ApplyRoomAccents();
        }

        private void BuildRoomTrigger()
        {
            roomTrigger = gameObject.GetComponent<BoxCollider>();
            if (roomTrigger == null)
            {
                roomTrigger = gameObject.AddComponent<BoxCollider>();
            }

            roomTrigger.isTrigger = true;
            roomTrigger.center = new Vector3(0f, 1.5f, 0f);
            roomTrigger.size = new Vector3(roomSize.x * 0.9f, 3f, roomSize.z * 0.9f);
        }

        private void BuildWallsAndGates()
        {
            float halfX = roomSize.x * 0.5f;
            float halfZ = roomSize.z * 0.5f;
            float halfDoor = doorWidth * 0.5f;
            float horizontalSegmentLength = halfX - halfDoor;
            float verticalSegmentLength = halfZ - halfDoor;

            CreateWallSegment("NorthWallLeft", new Vector3(-(halfDoor + horizontalSegmentLength * 0.5f), wallHeight * 0.5f, halfZ), new Vector3(horizontalSegmentLength, wallHeight, wallThickness));
            CreateWallSegment("NorthWallRight", new Vector3(halfDoor + horizontalSegmentLength * 0.5f, wallHeight * 0.5f, halfZ), new Vector3(horizontalSegmentLength, wallHeight, wallThickness));
            CreateGate("NorthGate", new Vector3(0f, wallHeight * 0.5f, halfZ - 0.1f), new Vector3(doorWidth, wallHeight, doorDepth));

            CreateWallSegment("SouthWallLeft", new Vector3(-(halfDoor + horizontalSegmentLength * 0.5f), wallHeight * 0.5f, -halfZ), new Vector3(horizontalSegmentLength, wallHeight, wallThickness));
            CreateWallSegment("SouthWallRight", new Vector3(halfDoor + horizontalSegmentLength * 0.5f, wallHeight * 0.5f, -halfZ), new Vector3(horizontalSegmentLength, wallHeight, wallThickness));
            CreateGate("SouthGate", new Vector3(0f, wallHeight * 0.5f, -halfZ + 0.1f), new Vector3(doorWidth, wallHeight, doorDepth));

            CreateWallSegment("EastWallTop", new Vector3(halfX, wallHeight * 0.5f, halfDoor + verticalSegmentLength * 0.5f), new Vector3(wallThickness, wallHeight, verticalSegmentLength));
            CreateWallSegment("EastWallBottom", new Vector3(halfX, wallHeight * 0.5f, -(halfDoor + verticalSegmentLength * 0.5f)), new Vector3(wallThickness, wallHeight, verticalSegmentLength));
            CreateGate("EastGate", new Vector3(halfX - 0.1f, wallHeight * 0.5f, 0f), new Vector3(doorDepth, wallHeight, doorWidth));

            CreateWallSegment("WestWallTop", new Vector3(-halfX, wallHeight * 0.5f, halfDoor + verticalSegmentLength * 0.5f), new Vector3(wallThickness, wallHeight, verticalSegmentLength));
            CreateWallSegment("WestWallBottom", new Vector3(-halfX, wallHeight * 0.5f, -(halfDoor + verticalSegmentLength * 0.5f)), new Vector3(wallThickness, wallHeight, verticalSegmentLength));
            CreateGate("WestGate", new Vector3(-halfX + 0.1f, wallHeight * 0.5f, 0f), new Vector3(doorDepth, wallHeight, doorWidth));

            doorAnchors = new Transform[4];
            doorAnchors[0] = CreateAnchor("NorthDoorAnchor", new Vector3(0f, 1.5f, halfZ));
            doorAnchors[1] = CreateAnchor("SouthDoorAnchor", new Vector3(0f, 1.5f, -halfZ));
            doorAnchors[2] = CreateAnchor("EastDoorAnchor", new Vector3(halfX, 1.5f, 0f));
            doorAnchors[3] = CreateAnchor("WestDoorAnchor", new Vector3(-halfX, 1.5f, 0f));

            SetDoorsActive(false);
        }

        private void CreateWallSegment(string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(wallsRoot, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = localScale;
            ApplyDungeonVisual(wall);
        }

        private void CreateGate(string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = name;
            gate.transform.SetParent(gatesRoot, false);
            gate.transform.localPosition = localPosition;
            gate.transform.localScale = localScale;
            gate.GetComponent<Collider>().isTrigger = false;
            ApplyDungeonVisual(gate);
            gates.Add(gate);
            closedGatePositions.Add(localPosition);
            openGatePositions.Add(localPosition + new Vector3(0f, doorOpenLift, 0f));
        }

        private Transform CreateAnchor(string name, Vector3 localPosition)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(generatedRoot, false);
            anchor.transform.localPosition = localPosition;
            return anchor.transform;
        }

        private static void ApplyDungeonVisual(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            UnityEngine.Material dungeonMaterial = Resources.Load<UnityEngine.Material>("Materials/DungeonStone");
            Texture2D dungeonTexture = Resources.Load<Texture2D>("Materials/DungeonBrick");

            if (dungeonMaterial == null)
            {
                return;
            }

            if (dungeonTexture != null)
            {
                dungeonMaterial.mainTexture = dungeonTexture;
                dungeonMaterial.mainTextureScale = new Vector2(4f, 4f);
            }

            dungeonMaterial.color = new Color(0.42f, 0.44f, 0.48f, 1f);

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = dungeonMaterial;
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
                Debug.LogWarning($"Tag '{tagName}' is missing. Please add it in Tag Manager.");
            }
        }

        private static void ApplyRoomMaterial(GameObject target, string resourcePath, Color fallbackColor)
        {
            if (target == null)
            {
                return;
            }

            UnityEngine.Material material = Resources.Load<UnityEngine.Material>(resourcePath);
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);

            if (material == null)
            {
                return;
            }

            if (texture != null)
            {
                material.mainTexture = texture;
            }
            else
            {
                material.color = fallbackColor;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private void EnsureAudioSource()
        {
            roomAudioSource = GetComponent<AudioSource>();
            if (roomAudioSource == null)
            {
                roomAudioSource = gameObject.AddComponent<AudioSource>();
            }

            roomAudioSource.playOnAwake = false;
            roomAudioSource.spatialBlend = 0f;
            roomAudioSource.volume = 0.25f;
        }

        private void BuildGrayboxFeatures()
        {
            BuildFloorTiles();
            BuildCornerPillars();
            BuildCenterPlatform();
            BuildWallRibs();
            BuildTorchPosts();
            BuildRoomDressing();
        }

        private void BuildFloorTiles()
        {
            UnityEngine.Material tileMaterial = RuntimeVisualUtility.CreateMaterial("Dungeon_Tile_Inset", new Color(0.24f, 0.25f, 0.29f, 1f), 0f, 0.28f);
            float spacingX = roomSize.x / 5f;
            float spacingZ = roomSize.z / 5f;

            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    if (Mathf.Abs(x) == 2 && Mathf.Abs(z) == 2)
                    {
                        continue;
                    }

                    GameObject tile = RuntimeVisualUtility.CreatePrimitiveChild(
                        propsRoot,
                        $"FloorTile_{x}_{z}",
                        PrimitiveType.Cube,
                        new Vector3(x * spacingX, 0.02f, z * spacingZ),
                        new Vector3(spacingX * 0.82f, 0.05f, spacingZ * 0.82f),
                        tileMaterial);
                    tile.transform.localRotation = Quaternion.Euler(0f, (x + z) % 2 == 0 ? 0f : 2f, 0f);
                }
            }
        }

        private void BuildCornerPillars()
        {
            float halfX = roomSize.x * 0.5f;
            float halfZ = roomSize.z * 0.5f;
            Vector3[] positions =
            {
                new Vector3(-halfX + 1.2f, 1.6f, -halfZ + 1.2f),
                new Vector3(-halfX + 1.2f, 1.6f, halfZ - 1.2f),
                new Vector3(halfX - 1.2f, 1.6f, -halfZ + 1.2f),
                new Vector3(halfX - 1.2f, 1.6f, halfZ - 1.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"CornerPillar_{i}";
                pillar.transform.SetParent(propsRoot, false);
                pillar.transform.localPosition = positions[i];
                pillar.transform.localScale = new Vector3(1.15f, 3.2f, 1.15f);
                ApplyDungeonVisual(pillar);
            }
        }

        private void BuildCenterPlatform()
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "CenterPlatform";
            platform.transform.SetParent(propsRoot, false);
            platform.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            platform.transform.localScale = new Vector3(7f, 0.5f, 7f);
            ApplyDungeonVisual(platform);
        }

        private void BuildWallRibs()
        {
            float halfX = roomSize.x * 0.5f;
            float halfZ = roomSize.z * 0.5f;

            CreateRib("NorthRib", new Vector3(0f, 1.3f, halfZ - 1.1f), new Vector3(roomSize.x * 0.65f, 0.35f, 0.6f));
            CreateRib("SouthRib", new Vector3(0f, 1.3f, -halfZ + 1.1f), new Vector3(roomSize.x * 0.65f, 0.35f, 0.6f));
            CreateRib("EastRib", new Vector3(halfX - 1.1f, 1.3f, 0f), new Vector3(0.6f, 0.35f, roomSize.z * 0.65f));
            CreateRib("WestRib", new Vector3(-halfX + 1.1f, 1.3f, 0f), new Vector3(0.6f, 0.35f, roomSize.z * 0.65f));
        }

        private void BuildTorchPosts()
        {
            if (accentsRoot == null)
            {
                return;
            }

            float halfX = roomSize.x * 0.5f - 2.4f;
            float halfZ = roomSize.z * 0.5f - 2.4f;
            Vector3[] positions =
            {
                new Vector3(-halfX, 1.05f, -halfZ),
                new Vector3(-halfX, 1.05f, halfZ),
                new Vector3(halfX, 1.05f, -halfZ),
                new Vector3(halfX, 1.05f, halfZ)
            };

            UnityEngine.Material postMaterial = RuntimeVisualUtility.CreateMaterial("Torch_Post", new Color(0.12f, 0.08f, 0.055f, 1f), 0f, 0.2f);
            UnityEngine.Material flameMaterial = RuntimeVisualUtility.CreateMaterial("Torch_Flame", new Color(1f, 0.48f, 0.16f, 1f), 0f, 0.65f);

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject post = RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, $"TorchPost_{i}", PrimitiveType.Cylinder, positions[i], new Vector3(0.18f, 1.05f, 0.18f), postMaterial);
                GameObject flame = RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, $"TorchFlame_{i}", PrimitiveType.Sphere, positions[i] + Vector3.up * 1.15f, new Vector3(0.32f, 0.44f, 0.32f), flameMaterial);
                flame.transform.localScale = new Vector3(0.28f, 0.42f, 0.28f);

                Light light = flame.AddComponent<Light>();
                light.name = "TorchLight";
                light.type = LightType.Point;
                light.range = 8f;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.48f, 0.18f, 1f);
            }
        }

        private void BuildRoomDressing()
        {
            if (propsRoot == null || accentsRoot == null)
            {
                return;
            }

            UnityEngine.Material trimMaterial = RuntimeVisualUtility.CreateMaterial("Room_Brass_Trim", new Color(0.84f, 0.58f, 0.22f, 1f), 0.18f, 0.46f);
            UnityEngine.Material bannerMaterial = RuntimeVisualUtility.CreateMaterial("Room_Banner", GetRoomAccentColor(roomType) * 0.72f + Color.black * 0.05f, 0f, 0.34f);
            UnityEngine.Material hazardMaterial = RuntimeVisualUtility.CreateMaterial("Room_Hazard_Mark", new Color(0.95f, 0.2f, 0.16f, 1f), 0f, 0.62f);

            float halfX = roomSize.x * 0.5f;
            float halfZ = roomSize.z * 0.5f;

            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "NorthFloorTrim", PrimitiveType.Cube, new Vector3(0f, 0.08f, halfZ - 2.2f), new Vector3(roomSize.x * 0.66f, 0.06f, 0.22f), trimMaterial);
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "SouthFloorTrim", PrimitiveType.Cube, new Vector3(0f, 0.08f, -halfZ + 2.2f), new Vector3(roomSize.x * 0.66f, 0.06f, 0.22f), trimMaterial);
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "EastFloorTrim", PrimitiveType.Cube, new Vector3(halfX - 2.2f, 0.08f, 0f), new Vector3(0.22f, 0.06f, roomSize.z * 0.66f), trimMaterial);
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "WestFloorTrim", PrimitiveType.Cube, new Vector3(-halfX + 2.2f, 0.08f, 0f), new Vector3(0.22f, 0.06f, roomSize.z * 0.66f), trimMaterial);

            RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "NorthBanner", PrimitiveType.Cube, new Vector3(0f, 2.45f, halfZ - 0.7f), new Vector3(2.4f, 1.45f, 0.12f), bannerMaterial);
            RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "SouthBanner", PrimitiveType.Cube, new Vector3(0f, 2.45f, -halfZ + 0.7f), new Vector3(2.4f, 1.45f, 0.12f), bannerMaterial);

            if (roomType == RoomType.Shop)
            {
                BuildShopDressing(trimMaterial, bannerMaterial);
            }
            else if (roomType == RoomType.Boss)
            {
                RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "BossSigil", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(3.1f, 0.08f, 3.1f), hazardMaterial);
            }
            else if (roomType == RoomType.Battle)
            {
                RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "BattleWarningLineA", PrimitiveType.Cube, new Vector3(0f, 0.1f, 3.4f), new Vector3(6.4f, 0.05f, 0.16f), hazardMaterial);
                RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "BattleWarningLineB", PrimitiveType.Cube, new Vector3(0f, 0.1f, -3.4f), new Vector3(6.4f, 0.05f, 0.16f), hazardMaterial);
            }
        }

        private void BuildShopDressing(UnityEngine.Material counterMaterial, UnityEngine.Material accentMaterial)
        {
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "ShopCounter", PrimitiveType.Cube, new Vector3(0f, 0.65f, 3.8f), new Vector3(5.2f, 1.1f, 1.15f), counterMaterial, false);
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "ShopDisplayLeft", PrimitiveType.Cube, new Vector3(-1.45f, 1.35f, 3.75f), new Vector3(0.7f, 0.22f, 0.7f), accentMaterial);
            RuntimeVisualUtility.CreatePrimitiveChild(propsRoot, "ShopDisplayRight", PrimitiveType.Cube, new Vector3(1.45f, 1.35f, 3.75f), new Vector3(0.7f, 0.22f, 0.7f), accentMaterial);
        }

        private void CreateRib(string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rib.name = name;
            rib.transform.SetParent(propsRoot, false);
            rib.transform.localPosition = localPosition;
            rib.transform.localScale = localScale;
            ApplyDungeonVisual(rib);
        }

        private void ApplyRoomAccents()
        {
            Color accent = GetRoomAccentColor(roomType);

            if (accentsRoot != null)
            {
                Light[] lights = accentsRoot.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                    {
                        lights[i].color = accent;
                    }
                }

                Renderer[] renderers = accentsRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null && renderer.name.Contains("TorchFlame"))
                    {
                        renderer.sharedMaterial = RuntimeVisualUtility.CreateMaterial("Torch_Flame_Accent", accent, 0f, 0.65f);
                    }
                }
            }
        }

        private static Color GetRoomAccentColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Initial:
                    return new Color(0.35f, 0.66f, 1f, 1f);
                case RoomType.Shop:
                    return new Color(0.35f, 1f, 0.56f, 1f);
                case RoomType.Boss:
                    return new Color(1f, 0.18f, 0.12f, 1f);
                default:
                    return new Color(1f, 0.48f, 0.18f, 1f);
            }
        }

        private void SpawnClearReward()
        {
            if (rewardSpawned || roomType == RoomType.Initial)
            {
                return;
            }

            rewardSpawned = true;

            if (LootManager.Instance != null)
            {
                int minLoot = roomType == RoomType.Boss ? 4 : 2;
                int maxLoot = roomType == RoomType.Boss ? 6 : 3;
                LootManager.Instance.SpawnLoot(RoomCenter + Vector3.up * 0.1f, minLoot, maxLoot);
            }

            if (accentsRoot == null)
            {
                return;
            }

            Color accent = GetRoomAccentColor(roomType);
            UnityEngine.Material rewardMaterial = RuntimeVisualUtility.CreateMaterial("ClearReward_Beacon", accent, 0f, 0.8f);
            GameObject beacon = RuntimeVisualUtility.CreatePrimitiveChild(accentsRoot, "ClearRewardBeacon", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(1.25f, 0.1f, 1.25f), rewardMaterial);
            Light light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = accent;
            light.range = 9f;
            light.intensity = 1.6f;
        }

        private IEnumerator AnimateDoors(bool closeDoors)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, doorAnimationDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (!closeDoors)
                {
                    t = 1f - t;
                }

                for (int i = 0; i < gates.Count; i++)
                {
                    GameObject gate = gates[i];
                    if (gate == null)
                    {
                        continue;
                    }

                    gate.transform.localPosition = Vector3.Lerp(openGatePositions[i], closedGatePositions[i], t);
                }

                yield return null;
            }

            for (int i = 0; i < gates.Count; i++)
            {
                GameObject gate = gates[i];
                if (gate == null)
                {
                    continue;
                }

                gate.transform.localPosition = closeDoors ? closedGatePositions[i] : openGatePositions[i];
                Collider gateCollider = gate.GetComponent<Collider>();
                if (gateCollider != null)
                {
                    gateCollider.enabled = closeDoors;
                }
            }

            doorAnimationRoutine = null;
        }

        private void PlayDoorTone(bool closeDoors)
        {
            if (roomAudioSource == null)
            {
                return;
            }

            AudioClip clip = CreateDoorToneClip(closeDoors ? 220f : 392f, closeDoors ? 0.16f : 0.12f);
            if (clip == null)
            {
                return;
            }

            roomAudioSource.pitch = doorAudioPitch;
            roomAudioSource.PlayOneShot(clip, closeDoors ? 0.9f : 0.7f);
        }

        private static AudioClip CreateDoorToneClip(float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(i / (float)sampleCount));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
            }

            AudioClip clip = AudioClip.Create($"DoorTone_{frequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void ShowRoomClearedMessage()
        {
            ToolTip toolTip = Object.FindObjectOfType<ToolTip>(true);
            if (toolTip == null)
            {
                return;
            }

            toolTip.Show("房间已清理");
            StartCoroutine(HideRoomClearedMessage(toolTip));
        }

        private IEnumerator HideRoomClearedMessage(ToolTip toolTip)
        {
            yield return new WaitForSeconds(1.5f);

            if (toolTip != null)
            {
                toolTip.Hide();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.6f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 1.5f, 0f), new Vector3(roomSize.x, 3f, roomSize.z));
        }
    }
}
