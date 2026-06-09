using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public enum LoopRoomKind
    {
        BasicCombat,
        EventReward,
        EliteCombat,
        Boss
    }

    [DefaultExecutionOrder(-450)]
    [DisallowMultipleComponent]
    public class DungeonLoopDirector : MonoBehaviour
    {
        public static DungeonLoopDirector Instance { get; private set; }

        private const int FinalStage = 5;
        private readonly List<GameObject> stageRoots = new List<GameObject>();
        private GameObject currentStageRoot;
        private NewRoomController currentRoom;
        private Transform player;
        private Text stageText;
        private Text victoryText;
        private bool playerPlacedForStage;

        public int CurrentStage { get; private set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            DisableOldDirectors();
            EnsureHud();
            BuildStage(1);
        }

        private void Update()
        {
            TryPlacePlayerForStage();
        }

        public void AdvanceStage()
        {
            GoToNextRoom();
        }

        public void GoToNextRoom()
        {
            if (CurrentStage >= FinalStage)
            {
                ShowVictory();
                return;
            }

            BuildStage(CurrentStage + 1);
        }

        public void GiveStageReward(bool bossReward)
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            inventory.EarnCoins(bossReward ? 180 : 70);
            if (bossReward)
            {
                inventory.AddItem(6, 1);
                inventory.AddItem(7, 1);
            }
            else
            {
                inventory.AddItem(Random.value > 0.5f ? 4 : 5, 1);
            }
        }

        public void GiveEventReward()
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null || !inventory.SpendCoins(20))
            {
                return;
            }

            inventory.AddItem(Random.value > 0.55f ? 7 : 6, 1);
        }

        public void HealPlayerToFull()
        {
            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory != null)
            {
                inventory.Heal(9999);
            }

            Bagsys.RogueLike.PlayerStats stats = FindObjectOfType<Bagsys.RogueLike.PlayerStats>();
            if (stats != null)
            {
                stats.Heal(9999);
            }
        }

        public void DebugBuildStage(int stage)
        {
            BuildStage(stage);
        }

        public void DebugStartCurrentChallenge()
        {
            if (currentRoom != null)
            {
                currentRoom.StartRoomChallenge();
            }
        }

        public void DebugCompleteCurrentRoom()
        {
            if (currentRoom != null)
            {
                currentRoom.DebugCompleteRoom();
            }
        }

        private void BuildStage(int stage)
        {
            CurrentStage = Mathf.Clamp(stage, 1, FinalStage);
            CleanupStages();
            playerPlacedForStage = false;

            currentStageRoot = new GameObject("DungeonLoop_Stage_" + CurrentStage);
            stageRoots.Add(currentStageRoot);

            LoopRoomKind kind = ResolveRoomKind(CurrentStage);
            NewRoomController room = currentStageRoot.AddComponent<NewRoomController>();
            room.Initialize(this, CurrentStage, kind);
            currentRoom = room;

            CachePlayer();
            TryPlacePlayerForStage();

            RefreshHud();
        }

        private void TryPlacePlayerForStage()
        {
            if (playerPlacedForStage)
            {
                return;
            }

            CachePlayer();
            if (player == null)
            {
                return;
            }

            player.position = new Vector3(0f, -5.5f, 0f);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = new Vector2(0f, -5.5f);
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            SetupCamera(player);
            playerPlacedForStage = true;
        }

        private void SetupCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 9.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10f);

            LoopCameraFollow follow = camera.GetComponent<LoopCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<LoopCameraFollow>();
            }

            follow.Configure(target);
        }

        private LoopRoomKind ResolveRoomKind(int stage)
        {
            if (stage <= 2)
            {
                return LoopRoomKind.BasicCombat;
            }

            if (stage == 3)
            {
                return LoopRoomKind.EventReward;
            }

            return stage == 4 ? LoopRoomKind.EliteCombat : LoopRoomKind.Boss;
        }

        private void CleanupStages()
        {
            for (int i = stageRoots.Count - 1; i >= 0; i--)
            {
                if (stageRoots[i] != null)
                {
                    Destroy(stageRoots[i]);
                }
            }

            stageRoots.Clear();
            CleanupStageUi();
        }

        private static void CleanupStageUi()
        {
            BossTopHealthBar2D.DestroySharedBar();
        }

        private void CachePlayer()
        {
            if (player != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        private void EnsureHud()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject root = new GameObject("DungeonLoopHud", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(620f, 86f);
            rect.anchoredPosition = new Vector2(0f, -12f);

            stageText = CreateHudText(root.transform, "StageText", 20, FontStyle.Bold);
            stageText.rectTransform.anchorMin = Vector2.zero;
            stageText.rectTransform.anchorMax = Vector2.one;
            stageText.rectTransform.offsetMin = Vector2.zero;
            stageText.rectTransform.offsetMax = Vector2.zero;

            victoryText = CreateHudText(root.transform, "VictoryText", 38, FontStyle.Bold);
            victoryText.rectTransform.anchorMin = new Vector2(-0.4f, -4.4f);
            victoryText.rectTransform.anchorMax = new Vector2(1.4f, -2.4f);
            victoryText.rectTransform.offsetMin = Vector2.zero;
            victoryText.rectTransform.offsetMax = Vector2.zero;
            victoryText.color = new Color(1f, 0.82f, 0.18f, 1f);
            victoryText.text = string.Empty;
        }

        private Text CreateHudText(Transform parent, string name, int size, FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private void RefreshHud()
        {
            if (stageText != null)
            {
                stageText.text = "Top-Down 2D    Stage " + CurrentStage + " / " + FinalStage + "    B Backpack    E Character    P/O Gear    I/L/K Flow";
            }
        }

        private void ShowVictory()
        {
            if (victoryText != null)
            {
                victoryText.text = "CONGRATULATIONS! VICTORY!";
            }
        }

        private static void DisableOldDirectors()
        {
            Bagsys.RogueLike.DungeonDirector oldDirector = FindObjectOfType<Bagsys.RogueLike.DungeonDirector>();
            if (oldDirector != null)
            {
                oldDirector.enabled = false;
            }

            Bagsys.RogueLike.DungeonGenerator oldGenerator = FindObjectOfType<Bagsys.RogueLike.DungeonGenerator>();
            if (oldGenerator != null)
            {
                oldGenerator.enabled = false;
            }
        }
    }

    [DisallowMultipleComponent]
    public class NewRoomController : MonoBehaviour
    {
        private DungeonLoopDirector director;
        private LoopRoomKind roomKind;
        private bool started;
        private bool cleared;
        private GameObject activePortal;
        private readonly List<GameObject> gates = new List<GameObject>();
        private readonly List<Bagsys.RogueLike.EnemyStats> enemies = new List<Bagsys.RogueLike.EnemyStats>();

        public void Initialize(DungeonLoopDirector owner, int stageIndex, LoopRoomKind kind)
        {
            director = owner;
            roomKind = kind;
            BuildRoom();

            if (roomKind == LoopRoomKind.EventReward)
            {
                cleared = true;
                SpawnEventInteractable();
            }
        }

        private void Update()
        {
            if (!started || cleared)
            {
                return;
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null || !enemies[i].gameObject.activeInHierarchy)
                {
                    enemies.RemoveAt(i);
                }
            }

            if (enemies.Count == 0)
            {
                CompleteChallenge();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player") || started || cleared)
            {
                return;
            }

            StartRoomChallenge();
        }

        public void StartRoomChallenge()
        {
            if (started || cleared)
            {
                return;
            }

            started = true;
            SetGates(true);
            SpawnEnemiesForRoom();
        }

        public void DebugCompleteRoom()
        {
            if (cleared)
            {
                SpawnNextPortal();
                return;
            }

            started = true;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] != null)
                {
                    Destroy(enemies[i].gameObject);
                }
            }

            enemies.Clear();
            CompleteChallenge();
        }

        public void SpawnNextPortal()
        {
            if (activePortal != null)
            {
                return;
            }

            bool victoryPortal = roomKind == LoopRoomKind.Boss;
            Color portalColor = victoryPortal ? new Color(1f, 0.77f, 0.08f) : new Color(0.2f, 0.62f, 1f);
            SpriteRenderer renderer = CreateSpriteBox(victoryPortal ? "VictoryPortal" : "NextStagePortal", new Vector2(0f, 6.6f), new Vector2(1.4f, 1.4f), "Projectile_Wand", portalColor, false, 5);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            CircleCollider2D collider = renderer.gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.7f;
            renderer.gameObject.AddComponent<LevelPortal>().Initialize(director, victoryPortal);
            activePortal = renderer.gameObject;
        }

        private void CompleteChallenge()
        {
            if (cleared)
            {
                return;
            }

            cleared = true;
            SetGates(false);
            SpawnRewardChest();
        }

        private void BuildRoom()
        {
            CreateSpriteBox("Floor", Vector2.zero, new Vector2(28f, 18f), "Floor_Sprite", new Color(0.17f, 0.19f, 0.22f), false, -10);
            CreateSpriteBox("NorthWall", new Vector2(0f, 9.2f), new Vector2(28f, 0.75f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);
            CreateSpriteBox("SouthWall", new Vector2(0f, -9.2f), new Vector2(28f, 0.75f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);
            CreateSpriteBox("EastWallTop", new Vector2(14.2f, 4.6f), new Vector2(0.75f, 9.2f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);
            CreateSpriteBox("EastWallBottom", new Vector2(14.2f, -4.6f), new Vector2(0.75f, 7.2f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);
            CreateSpriteBox("WestWallTop", new Vector2(-14.2f, 4.6f), new Vector2(0.75f, 9.2f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);
            CreateSpriteBox("WestWallBottom", new Vector2(-14.2f, -4.6f), new Vector2(0.75f, 7.2f), "Wall_Sprite", new Color(0.07f, 0.08f, 0.1f), true, 0);

            gates.Add(CreateSpriteBox("NorthGate", new Vector2(0f, 8.85f), new Vector2(4.3f, 0.95f), "Wall_Sprite", new Color(0.38f, 0.39f, 0.43f), true, 2).gameObject);
            gates.Add(CreateSpriteBox("SouthGate", new Vector2(0f, -8.85f), new Vector2(4.3f, 0.95f), "Wall_Sprite", new Color(0.38f, 0.39f, 0.43f), true, 2).gameObject);
            gates.Add(CreateSpriteBox("EastGate", new Vector2(13.85f, 0f), new Vector2(0.95f, 4.3f), "Wall_Sprite", new Color(0.38f, 0.39f, 0.43f), true, 2).gameObject);
            gates.Add(CreateSpriteBox("WestGate", new Vector2(-13.85f, 0f), new Vector2(0.95f, 4.3f), "Wall_Sprite", new Color(0.38f, 0.39f, 0.43f), true, 2).gameObject);
            SetGates(false);

            CreateSpriteBox("RoomCenterSigil", Vector2.zero, new Vector2(2.3f, 2.3f), "Floor_Sprite", GetAccentColor(), false, -2);

            BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(7.8f, 7.8f);
        }

        private void SpawnEnemiesForRoom()
        {
            if (roomKind == LoopRoomKind.BasicCombat)
            {
                int count = Random.Range(3, 6);
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = new Vector2(Random.Range(-5f, 5f), Random.Range(-2.5f, 4.5f));
                    enemies.Add(SpawnEnemy("Slime", pos, "Slime_Sprite", new Color(0.9f, 0.1f, 0.1f), 30, 5, 3.9f, typeof(SlimeAI)));
                }
            }
            else if (roomKind == LoopRoomKind.EliteCombat)
            {
                enemies.Add(SpawnEnemy("Elite_Archer_A", new Vector2(-4f, 2f), "Slime_Sprite", new Color(0.92f, 0.82f, 0.28f), 80, 12, 2.6f, typeof(SkeletonArcherAI)));
                enemies.Add(SpawnEnemy("Elite_Archer_B", new Vector2(4f, 2f), "Slime_Sprite", new Color(0.92f, 0.82f, 0.28f), 80, 12, 2.6f, typeof(SkeletonArcherAI)));
            }
            else if (roomKind == LoopRoomKind.Boss)
            {
                enemies.Add(SpawnEnemy("Dungeon_Boss", new Vector2(0f, 2.2f), "Boss_Sprite", new Color(0.1f, 0.35f, 1f), 300, 25, 2.1f, typeof(FloorBossAI), 3f));
            }
        }

        private Bagsys.RogueLike.EnemyStats SpawnEnemy(string name, Vector2 localPos, string spriteName, Color color, int hp, int atk, float speed, System.Type aiType, float scale = 1f)
        {
            SpriteRenderer renderer = CreateSpriteBox(name, localPos, Vector2.one * scale, spriteName, color, false, 3);
            GameObject enemy = renderer.gameObject;
            enemy.tag = "Enemy";

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;

            Bagsys.RogueLike.EnemyStats stats = enemy.AddComponent<Bagsys.RogueLike.EnemyStats>();
            stats.ConfigureBaseStats(hp, atk, 0);
            enemy.AddComponent<ModernRogue.SmoothWorldHealthBar>();
            EnemyLoopAgent agent = (EnemyLoopAgent)enemy.AddComponent(aiType);
            agent.Configure(atk, speed);
            return stats;
        }

        private void SpawnEventInteractable()
        {
            if (Random.value > 0.5f)
            {
                LoopEventInteractable.Create(transform, "HealingFountain", "治愈圣泉\n靠近按 E 回满 HP", true, GetAccentColor());
            }
            else
            {
                LoopEventInteractable.Create(transform, "DestinyStatue", "命运神像\n按 E 花费20金币换紫/橙装备", false, GetAccentColor());
            }
        }

        private void SpawnRewardChest()
        {
            SpriteRenderer chest = CreateSpriteBox("ClearRewardChest", Vector2.zero, new Vector2(1.25f, 1.25f), "Weapon_Dagger", new Color(1f, 0.67f, 0.16f), true, 4);
            LoopRewardChest reward = chest.gameObject.AddComponent<LoopRewardChest>();
            reward.Initialize(director, roomKind == LoopRoomKind.Boss);
        }

        private SpriteRenderer CreateSpriteBox(string name, Vector2 pos, Vector2 scale, string spriteName, Color color, bool solidCollider, int sortingOrder)
        {
            SpriteRenderer renderer = Art2DUtility.CreateSpriteObject(transform, name, pos, scale, spriteName, color, sortingOrder);
            if (solidCollider)
            {
                BoxCollider2D collider = renderer.gameObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
            }

            return renderer;
        }

        private void SetGates(bool active)
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].SetActive(active);
                }
            }
        }

        private Color GetAccentColor()
        {
            switch (roomKind)
            {
                case LoopRoomKind.EventReward:
                    return new Color(0.72f, 0.35f, 1f, 0.82f);
                case LoopRoomKind.EliteCombat:
                    return new Color(1f, 0.72f, 0.18f, 0.82f);
                case LoopRoomKind.Boss:
                    return new Color(0.15f, 0.35f, 1f, 0.82f);
                default:
                    return new Color(1f, 0.2f, 0.16f, 0.82f);
            }
        }
    }

    [DisallowMultipleComponent]
    public class LoopCameraFollow : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset;

        public void Configure(Transform followTarget)
        {
            Configure(followTarget, Vector3.zero);
        }

        public void Configure(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = new Vector3(target.position.x + offset.x, target.position.y + offset.y, -10f);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 8f);
            transform.rotation = Quaternion.identity;
        }
    }
}
