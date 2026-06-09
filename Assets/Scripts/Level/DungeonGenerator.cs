using System.Collections.Generic;
using UnityEngine;

namespace Bagsys.RogueLike
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Rooms")]
        [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();
        [SerializeField] private int minRoomCount = 3;
        [SerializeField] private int maxRoomCount = 5;
        [SerializeField] private float roomSpacing = 30f;
        [SerializeField] private Vector3 generationOffset = Vector3.zero;

        private readonly List<GameObject> spawnedRooms = new List<GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> roomsByCell = new Dictionary<Vector3Int, GameObject>();
        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        private readonly Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        private Room startingRoom;
        private Transform corridorsRoot;

        private static readonly Vector3Int[] CardinalDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        private void Start()
        {
            GenerateDungeon();
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon()
        {
            ClearDungeon();
            startingRoom = null;

            int targetRoomCount = Random.Range(minRoomCount, maxRoomCount + 1);
            Vector3Int originCell = Vector3Int.zero;
            SpawnRoom(originCell, RoomType.Initial);
            frontier.Enqueue(originCell);

            while (spawnedRooms.Count < targetRoomCount && frontier.Count > 0)
            {
                Vector3Int currentCell = frontier.Dequeue();
                List<Vector3Int> shuffledDirections = GetShuffledDirections();
                bool spawnedFromCurrent = false;

                for (int i = 0; i < shuffledDirections.Count; i++)
                {
                    Vector3Int nextCell = currentCell + shuffledDirections[i];
                    if (occupiedCells.Contains(nextCell))
                    {
                        continue;
                    }

                    RoomType roomType = spawnedRooms.Count == targetRoomCount - 1
                        ? RoomType.Boss
                        : (Random.value < 0.2f ? RoomType.Shop : RoomType.Battle);

                    SpawnRoom(nextCell, roomType);
                    frontier.Enqueue(nextCell);
                    spawnedFromCurrent = true;
                    break;
                }

                if (!spawnedFromCurrent)
                {
                    continue;
                }
            }

            PlacePlayerAtStartRoom();
            BuildCorridors();
        }

        private void SpawnRoom(Vector3Int cellPosition, RoomType roomType)
        {
            Vector3 worldPosition = generationOffset + new Vector3(cellPosition.x * roomSpacing, 0f, cellPosition.z * roomSpacing);
            GameObject roomInstance = CreateRoomInstance(worldPosition);
            ApplyDungeonVisual(roomInstance);

            Room room = roomInstance.GetComponent<Room>();
            if (room != null)
            {
                bool isStartRoom = roomType == RoomType.Initial;
                room.Initialize(roomType, isStartRoom);
                room.isCleared = isStartRoom;

                if (isStartRoom)
                {
                    startingRoom = room;
                }
            }

            spawnedRooms.Add(roomInstance);
            roomsByCell[cellPosition] = roomInstance;
            occupiedCells.Add(cellPosition);
        }

        private GameObject CreateRoomInstance(Vector3 worldPosition)
        {
            if (roomPrefabs != null && roomPrefabs.Count > 0)
            {
                GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
                if (prefab != null)
                {
                    return Instantiate(prefab, worldPosition, Quaternion.identity, transform);
                }
            }

            GameObject roomObject = new GameObject("RuntimeRoom");
            roomObject.name = "RuntimeRoom";
            roomObject.transform.SetParent(transform, false);
            roomObject.transform.position = worldPosition;

            Room room = roomObject.GetComponent<Room>();
            if (room == null)
            {
                room = roomObject.AddComponent<Room>();
            }

            return roomObject;
        }

        private static void ApplyDungeonVisual(GameObject roomInstance)
        {
            if (roomInstance == null)
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

            Renderer[] renderers = roomInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = dungeonMaterial;
            }
        }

        private List<Vector3Int> GetShuffledDirections()
        {
            List<Vector3Int> directions = new List<Vector3Int>(CardinalDirections);

            for (int i = 0; i < directions.Count; i++)
            {
                Vector3Int temp = directions[i];
                int randomIndex = Random.Range(i, directions.Count);
                directions[i] = directions[randomIndex];
                directions[randomIndex] = temp;
            }

            return directions;
        }

        private void ClearDungeon()
        {
            for (int i = 0; i < spawnedRooms.Count; i++)
            {
                if (spawnedRooms[i] != null)
                {
                    Destroy(spawnedRooms[i]);
                }
            }

            spawnedRooms.Clear();
            roomsByCell.Clear();
            occupiedCells.Clear();
            frontier.Clear();

            if (corridorsRoot != null)
            {
                Destroy(corridorsRoot.gameObject);
                corridorsRoot = null;
            }
        }

        private void PlacePlayerAtStartRoom()
        {
            if (startingRoom == null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            Vector3 startPosition = startingRoom.RoomCenter + Vector3.up;
            playerObject.transform.position = startPosition;

            Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.position = startPosition;
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void BuildCorridors()
        {
            if (roomsByCell.Count <= 1)
            {
                return;
            }

            corridorsRoot = new GameObject("RuntimeCorridors").transform;
            corridorsRoot.SetParent(transform, false);

            UnityEngine.Material floorMaterial = RuntimeVisualUtility.CreateMaterial("Corridor_Floor", new Color(0.22f, 0.23f, 0.27f, 1f), 0f, 0.28f);
            UnityEngine.Material wallMaterial = RuntimeVisualUtility.CreateMaterial("Corridor_Edge", new Color(0.1f, 0.105f, 0.13f, 1f), 0f, 0.22f);
            UnityEngine.Material runeMaterial = RuntimeVisualUtility.CreateMaterial("Corridor_Rune", new Color(0.34f, 0.62f, 1f, 1f), 0f, 0.62f);

            foreach (KeyValuePair<Vector3Int, GameObject> pair in roomsByCell)
            {
                Vector3Int cell = pair.Key;
                TryCreateCorridor(cell, cell + Vector3Int.right, floorMaterial, wallMaterial, runeMaterial);
                TryCreateCorridor(cell, cell + new Vector3Int(0, 0, 1), floorMaterial, wallMaterial, runeMaterial);
            }
        }

        private void TryCreateCorridor(Vector3Int fromCell, Vector3Int toCell, UnityEngine.Material floorMaterial, UnityEngine.Material wallMaterial, UnityEngine.Material runeMaterial)
        {
            if (!roomsByCell.ContainsKey(fromCell) || !roomsByCell.ContainsKey(toCell))
            {
                return;
            }

            Vector3 from = generationOffset + new Vector3(fromCell.x * roomSpacing, 0f, fromCell.z * roomSpacing);
            Vector3 to = generationOffset + new Vector3(toCell.x * roomSpacing, 0f, toCell.z * roomSpacing);
            Vector3 center = (from + to) * 0.5f;
            Vector3 direction = (to - from).normalized;
            bool horizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

            Vector3 floorScale = horizontal
                ? new Vector3(Mathf.Max(5f, roomSpacing - 24f), 0.28f, 5.2f)
                : new Vector3(5.2f, 0.28f, Mathf.Max(5f, roomSpacing - 24f));

            GameObject floor = RuntimeVisualUtility.CreatePrimitiveChild(corridorsRoot, $"Corridor_{fromCell}_{toCell}", PrimitiveType.Cube, center + Vector3.down * 0.36f, floorScale, floorMaterial, false);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            float sideOffset = 2.85f;
            Vector3 sideA = horizontal ? Vector3.forward * sideOffset : Vector3.right * sideOffset;
            Vector3 sideB = -sideA;
            Vector3 wallScale = horizontal
                ? new Vector3(floorScale.x, 1.35f, 0.35f)
                : new Vector3(0.35f, 1.35f, floorScale.z);

            RuntimeVisualUtility.CreatePrimitiveChild(corridorsRoot, "CorridorEdgeA", PrimitiveType.Cube, center + sideA + Vector3.up * 0.28f, wallScale, wallMaterial, false);
            RuntimeVisualUtility.CreatePrimitiveChild(corridorsRoot, "CorridorEdgeB", PrimitiveType.Cube, center + sideB + Vector3.up * 0.28f, wallScale, wallMaterial, false);

            GameObject rune = RuntimeVisualUtility.CreatePrimitiveChild(corridorsRoot, "CorridorRune", PrimitiveType.Cube, center + Vector3.up * 0.02f, horizontal ? new Vector3(1.1f, 0.04f, 4.2f) : new Vector3(4.2f, 0.04f, 1.1f), runeMaterial);
            Light runeLight = rune.AddComponent<Light>();
            runeLight.type = LightType.Point;
            runeLight.color = new Color(0.28f, 0.55f, 1f, 1f);
            runeLight.range = 6f;
            runeLight.intensity = 0.55f;
        }
    }
}
