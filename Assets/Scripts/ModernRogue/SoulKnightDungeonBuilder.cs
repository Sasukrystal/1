using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public static class SoulKnightDungeonBuilder
    {
        private static readonly Vector2 RoomSize = new Vector2(30f, 20f);
        private const float RoomGap = 36f;
        private const float DoorOpening = 7.6f;

        public static void BuildLobby(Transform root, SoulKnightDirector director)
        {
            CreateRoomShell(root, Vector2.zero, new Color(0.18f, 0.22f, 0.2f), false, false, false, false);
            AddLobbyArchitecture(root);
            CreateLabel(root, new Vector2(0f, 5.1f), "安全大厅");
            CreateSign(root, new Vector2(0f, 0.95f), "初始宝箱");
            CreateSign(root, new Vector2(0f, 7.55f), "地牢入口");
            CreatePathArrow(root, new Vector2(0f, 2.7f), 90f, new Color(0.2f, 0.78f, 1f, 0.95f));

            SoulKnightChest chest = CreateChest(root, new Vector2(0f, -1.25f), SoulKnightChestMode.Starter);
            chest.Initialize(director, SoulKnightChestMode.Starter);

            GameObject portal = CreatePortal(root, "DungeonPortal", new Vector2(0f, 7.55f), false);
            CircleCollider2D portalCollider = portal.AddComponent<CircleCollider2D>();
            portalCollider.isTrigger = true;
            portalCollider.radius = 1.95f;
            BoxCollider2D portalPadCollider = portal.AddComponent<BoxCollider2D>();
            portalPadCollider.isTrigger = true;
            portalPadCollider.offset = new Vector2(0f, -0.75f);
            portalPadCollider.size = new Vector2(4.2f, 3.4f);
            SoulKnightPortal portalScript = portal.AddComponent<SoulKnightPortal>();
            portalScript.Initialize(director, SoulKnightPortalMode.StartRun);
        }

        private static void AddLobbyArchitecture(Transform root)
        {
            AddFloorTiles(root, Vector2.zero, RoomSize, 1.65f, new Color(1f, 1f, 1f, 0.03f));
            CreateSprite(root, "LobbyFloorArt", Vector2.zero, RoomSize, "Environment/Tiles/Floor_Lobby", Color.white, -11);
            CreateSprite(root, "LobbyMainCarpet", new Vector2(0f, -2.35f), new Vector2(6.4f, 11.4f), "Environment/Lobby/Lobby_Carpet", Color.white, -4);
            CreateSprite(root, "PortalDais", new Vector2(0f, 7.55f), new Vector2(6.8f, 3.8f), "Environment/Tiles/Floor_Lobby", new Color(1f, 1f, 1f, 0.84f), -3);
            CreateShopCounter(root, new Vector2(11.35f, -2.1f));
            CreateTrainingTarget(root, new Vector2(-11.65f, 5.0f));
            CreateBulletinBoard(root, new Vector2(11.7f, 5.1f));
            CreateClassSelectionStatue(root, new Vector2(0f, -6.65f));
            CreateWeaponForge(root, new Vector2(-11.5f, -2.1f));
            CreateSign(root, new Vector2(-11.5f, -0.75f), "武器铺");
            CreateSign(root, new Vector2(11.35f, -0.75f), "补给台");
            CreateSign(root, new Vector2(-11.65f, 6.25f), "训练靶");
            CreateSign(root, new Vector2(11.7f, 6.35f), "任务板");

            Vector2[] pillarPositions =
            {
                new Vector2(-13f, 7.3f),
                new Vector2(13f, 7.3f),
                new Vector2(-13f, -7.6f),
                new Vector2(13f, -7.6f)
            };

            for (int i = 0; i < pillarPositions.Length; i++)
            {
                CreatePillarTorch(root, pillarPositions[i]);
            }

            CreateSprite(root, "LeftAlcoveFloor", new Vector2(-18.1f, 0.4f), new Vector2(6.2f, 10.2f), "Environment/Tiles/Floor_Lobby", new Color(1f, 1f, 1f, 0.68f), -7);
            CreateSprite(root, "RightAlcoveFloor", new Vector2(18.1f, 0.4f), new Vector2(6.2f, 10.2f), "Environment/Tiles/Floor_Lobby", new Color(1f, 1f, 1f, 0.68f), -7);
        }

        public static void BuildStage(Transform root, SoulKnightDirector director, int stage, SoulKnightStageProfile profile)
        {
            profile = profile ?? SoulKnightStageProfiles.Resolve(stage);
            List<GeneratedRoom> rooms = GenerateFloorLayout(stage, profile);
            for (int i = 0; i < rooms.Count; i++)
            {
                BuildGeneratedRoom(root, director, stage, profile, rooms[i]);
            }

            BuildStageCorridors(root, rooms, profile);
        }

        public static IEnumerator BuildStageAsync(Transform root, SoulKnightDirector director, int stage, SoulKnightStageProfile profile, System.Action onStartRoomReady = null)
        {
            profile = profile ?? SoulKnightStageProfiles.Resolve(stage);
            List<GeneratedRoom> rooms = GenerateFloorLayout(stage, profile);
            int startRoomIndex = -1;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].kind == GeneratedRoomKind.Start)
                {
                    startRoomIndex = i;
                    break;
                }
            }

            if (startRoomIndex >= 0)
            {
                BuildGeneratedRoom(root, director, stage, profile, rooms[startRoomIndex]);
                onStartRoomReady?.Invoke();
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == startRoomIndex)
                {
                    continue;
                }

                BuildGeneratedRoom(root, director, stage, profile, rooms[i]);
                yield return null;
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                GeneratedRoom room = rooms[i];
                Vector2 from = GridToWorld(room.grid);
                for (int j = 0; j < room.neighbors.Count; j++)
                {
                    Vector2Int neighbor = room.neighbors[j];
                    if (neighbor.x < room.grid.x || neighbor.y < room.grid.y)
                    {
                        continue;
                    }

                    CreateCorridor(root, from, GridToWorld(neighbor), profile);
                    CreateDoorFrame(root, from, GridToWorld(neighbor), profile);
                    yield return null;
                }
            }
        }

        private static void BuildGeneratedRoom(Transform root, SoulKnightDirector director, int stage, SoulKnightStageProfile profile, GeneratedRoom room)
        {
            Vector2 center = GridToWorld(room.grid);
            CreateRoomCollisionShell(root, center, room.openNorth, room.openSouth, room.openWest, room.openEast);
            DungeonCrawlCombatRoomVisual.RoomVisualStyle roomVisualStyle = ResolveDungeonCrawlRoomVisualStyle(room.kind);
            DungeonCrawlCombatRoomVisual.Create(
                root,
                center,
                room.openNorth,
                room.openSouth,
                room.openWest,
                room.openEast,
                roomVisualStyle,
                profile,
                room.isSignatureRoom);

            switch (room.kind)
            {
                case GeneratedRoomKind.Boss:
                {
                    RoomTrigger2D trigger = CreateRoomTrigger(root, center, stage, true);
                    trigger.Initialize(director, stage, true, profile);
                    trigger.SetRewardType(RoomRewardType.Core);
                    break;
                }
                case GeneratedRoomKind.Shop:
                    CreateShopStand(root, center, director);
                    break;
                case GeneratedRoomKind.Treasure:
                {
                    RoomTrigger2D trigger = CreateRoomTrigger(root, center, stage, false);
                    trigger.Initialize(director, stage, false, profile);
                    trigger.SetRewardType(RoomRewardType.Treasure);
                    break;
                }
                case GeneratedRoomKind.Combat:
                {
                    RoomTrigger2D trigger = CreateRoomTrigger(root, center, stage, false);
                    trigger.Initialize(director, stage, false, profile);
                    trigger.SetRewardType(room.rewardType);
                    break;
                }
            }
        }

        private static void BuildStageCorridors(Transform root, List<GeneratedRoom> rooms, SoulKnightStageProfile profile)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                GeneratedRoom room = rooms[i];
                Vector2 from = GridToWorld(room.grid);
                for (int j = 0; j < room.neighbors.Count; j++)
                {
                    Vector2Int neighbor = room.neighbors[j];
                    if (neighbor.x < room.grid.x || neighbor.y < room.grid.y)
                    {
                        continue;
                    }

                    CreateCorridor(root, from, GridToWorld(neighbor), profile);
                    CreateDoorFrame(root, from, GridToWorld(neighbor), profile);
                }
            }
        }

        private static DungeonCrawlCombatRoomVisual.RoomVisualStyle ResolveDungeonCrawlRoomVisualStyle(GeneratedRoomKind roomKind)
        {
            switch (roomKind)
            {
                case GeneratedRoomKind.Start:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Start;
                case GeneratedRoomKind.Crossroad:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Crossroad;
                case GeneratedRoomKind.Shop:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Shop;
                case GeneratedRoomKind.Treasure:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Treasure;
                case GeneratedRoomKind.Boss:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Boss;
                default:
                    return DungeonCrawlCombatRoomVisual.RoomVisualStyle.Combat;
            }
        }

        private static void BuildCombatRoom(Transform root, SoulKnightDirector director, Vector2 center, int stage, int index, RoomRewardType rewardType)
        {
            bool westRoom = center.x < -0.1f;
            bool eastRoom = center.x > 0.1f;
            CreateRoomShell(root, center, ResolveFloorColor(stage, false), false, false, eastRoom, westRoom);
            AddDungeonRoomDecor(root, center, stage, false, false);
            CreateRoomPlaque(root, center, ResolveRewardRoomName(rewardType));
            RoomTrigger2D trigger = CreateRoomTrigger(root, center, stage, false);
            trigger.Initialize(director, stage, false);
            trigger.SetRewardType(rewardType);
        }

        private static void BuildBossRoom(Transform root, SoulKnightDirector director, Vector2 center, int stage)
        {
            CreateRoomShell(root, center, new Color(0.18f, 0.12f, 0.16f), false, true, false, false);
            AddDungeonRoomDecor(root, center, stage, false, true);
            CreateRoomPlaque(root, center, "Boss 房");
            CreateLabel(root, center + new Vector2(0f, 3.9f), "警告：Boss 房\n进入后会锁门，胜利后开启下一层传送阵");
            RoomTrigger2D trigger = CreateRoomTrigger(root, center, stage, true);
            trigger.Initialize(director, stage, true);
            trigger.SetRewardType(RoomRewardType.Core);
        }

        private static List<GeneratedRoom> GenerateFloorLayout(int stage, SoulKnightStageProfile profile)
        {
            Dictionary<Vector2Int, GeneratedRoom> map = new Dictionary<Vector2Int, GeneratedRoom>();
            Vector2Int start = new Vector2Int(0, -1);
            AddGeneratedRoom(map, start);
            Vector2Int current = start;
            int targetCount = Random.Range(profile.RoomCountMin, profile.RoomCountMax + 1);
            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            int safety = 0;
            while (map.Count < targetCount && safety++ < 200)
            {
                Vector2Int direction = directions[Random.Range(0, directions.Length)];
                Vector2Int next = current + direction;
                if (next.y < -1 || Mathf.Abs(next.x) > 3 || next.y > 3)
                {
                    continue;
                }

                AddGeneratedRoom(map, next);
                LinkGeneratedRooms(map[current], map[next]);
                current = Random.value > 0.34f ? next : PickRandomKey(map);
            }

            List<GeneratedRoom> rooms = new List<GeneratedRoom>(map.Values);
            GeneratedRoom startRoom = map[start];
            startRoom.kind = GeneratedRoomKind.Start;

            GeneratedRoom bossRoom = null;
            int bestDistance = -1;
            for (int i = 0; i < rooms.Count; i++)
            {
                GeneratedRoom room = rooms[i];
                int distance = Mathf.Abs(room.grid.x - start.x) + Mathf.Abs(room.grid.y - start.y);
                if (room != startRoom && distance > bestDistance)
                {
                    bestDistance = distance;
                    bossRoom = room;
                }
            }

            if (bossRoom != null)
            {
                bossRoom.kind = GeneratedRoomKind.Boss;
            }

            bool shopAssigned = false;
            bool treasureAssigned = false;
            for (int i = 0; i < rooms.Count; i++)
            {
                GeneratedRoom room = rooms[i];
                ResolveOpenings(room);
                if (room.kind == GeneratedRoomKind.Start || room.kind == GeneratedRoomKind.Boss)
                {
                    continue;
                }

                if (!shopAssigned && Random.value < profile.ShopChance)
                {
                    room.kind = GeneratedRoomKind.Shop;
                    shopAssigned = true;
                    continue;
                }

                if (!treasureAssigned && Random.value < profile.TreasureChance)
                {
                    room.kind = GeneratedRoomKind.Treasure;
                    treasureAssigned = true;
                    continue;
                }

                room.kind = room.neighbors.Count >= 3 && Random.value < profile.CrossroadChance ? GeneratedRoomKind.Crossroad : GeneratedRoomKind.Combat;
                room.rewardType = ResolveRewardType(stage, Mathf.Abs(room.grid.x * 13 + room.grid.y * 7), profile);
            }

            if (!treasureAssigned)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].kind == GeneratedRoomKind.Combat)
                    {
                        rooms[i].kind = GeneratedRoomKind.Treasure;
                        break;
                    }
                }
            }

            AssignSignatureRoom(rooms, profile);
            return rooms;
        }

        private static void AssignSignatureRoom(List<GeneratedRoom> rooms, SoulKnightStageProfile profile)
        {
            if (rooms == null || profile == null)
            {
                return;
            }

            GeneratedRoomKind desiredKind = ResolveGeneratedRoomKind(profile.SignatureKind);
            GeneratedRoom signatureRoom = null;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].kind == desiredKind)
                {
                    signatureRoom = rooms[i];
                    break;
                }
            }

            if (signatureRoom == null)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].kind == GeneratedRoomKind.Combat)
                    {
                        rooms[i].kind = desiredKind;
                        signatureRoom = rooms[i];
                        break;
                    }
                }
            }

            if (signatureRoom != null)
            {
                signatureRoom.isSignatureRoom = true;
                if (signatureRoom.kind == GeneratedRoomKind.Combat)
                {
                    signatureRoom.rewardType = ResolveRewardType(profile.Stage, Mathf.Abs(signatureRoom.grid.x * 13 + signatureRoom.grid.y * 7) + 99, profile);
                }
            }
        }

        private static GeneratedRoomKind ResolveGeneratedRoomKind(StageSignatureKind signatureKind)
        {
            switch (signatureKind)
            {
                case StageSignatureKind.Crossroad:
                    return GeneratedRoomKind.Crossroad;
                case StageSignatureKind.Treasure:
                    return GeneratedRoomKind.Treasure;
                case StageSignatureKind.Shop:
                    return GeneratedRoomKind.Shop;
                default:
                    return GeneratedRoomKind.Combat;
            }
        }

        private static GeneratedRoom AddGeneratedRoom(Dictionary<Vector2Int, GeneratedRoom> map, Vector2Int grid)
        {
            if (map.TryGetValue(grid, out GeneratedRoom existing))
            {
                return existing;
            }

            GeneratedRoom room = new GeneratedRoom { grid = grid };
            map.Add(grid, room);
            return room;
        }

        private static void LinkGeneratedRooms(GeneratedRoom a, GeneratedRoom b)
        {
            if (!a.neighbors.Contains(b.grid))
            {
                a.neighbors.Add(b.grid);
            }

            if (!b.neighbors.Contains(a.grid))
            {
                b.neighbors.Add(a.grid);
            }
        }

        private static Vector2Int PickRandomKey(Dictionary<Vector2Int, GeneratedRoom> map)
        {
            int index = Random.Range(0, map.Count);
            foreach (Vector2Int key in map.Keys)
            {
                if (index-- <= 0)
                {
                    return key;
                }
            }

            return new Vector2Int(0, -1);
        }

        private static void ResolveOpenings(GeneratedRoom room)
        {
            room.openNorth = room.neighbors.Contains(room.grid + Vector2Int.up);
            room.openSouth = room.neighbors.Contains(room.grid + Vector2Int.down);
            room.openWest = room.neighbors.Contains(room.grid + Vector2Int.left);
            room.openEast = room.neighbors.Contains(room.grid + Vector2Int.right);
        }

        private static Vector2 GridToWorld(Vector2Int grid)
        {
            return new Vector2(grid.x * RoomGap, grid.y * RoomGap);
        }

        private enum GeneratedRoomKind
        {
            Start,
            Combat,
            Crossroad,
            Treasure,
            Shop,
            Boss
        }

        private sealed class GeneratedRoom
        {
            public Vector2Int grid;
            public GeneratedRoomKind kind = GeneratedRoomKind.Combat;
            public RoomRewardType rewardType = RoomRewardType.Equipment;
            public bool isSignatureRoom;
            public bool openNorth;
            public bool openSouth;
            public bool openWest;
            public bool openEast;
            public readonly List<Vector2Int> neighbors = new List<Vector2Int>();
        }

        public static SoulKnightChest CreateChest(Transform root, Vector2 position, SoulKnightChestMode mode)
        {
            GameObject obj = CreateChestVisual(root, position, mode);
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.45f, 1.05f);
            return obj.AddComponent<SoulKnightChest>();
        }

        public static void CreateShopStand(Transform root, Vector2 position, SoulKnightDirector director)
        {
            GameObject npc = new GameObject("牢骚商人");
            npc.transform.SetParent(root, false);
            npc.transform.localPosition = position;
            Art2DUtility.CreateLobbySpriteObject(npc.transform, "ShopMerchantArt", Vector2.zero, new Vector2(2.4f, 2.4f), "Lobby_MerchantNPC", Color.white, 8);

            CircleCollider2D collider = npc.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.6f;
            SoulKnightShopStand shop = npc.AddComponent<SoulKnightShopStand>();
            shop.Initialize(director);
        }

        public static GameObject CreateFloorPortal(Transform root, Vector2 position, SoulKnightDirector director, SoulKnightPortalMode mode, bool gold)
        {
            GameObject portal = CreatePortal(root, mode == SoulKnightPortalMode.Victory ? "VictoryPortal" : "NextFloorPortal", position, gold);
            CircleCollider2D portalCollider = portal.AddComponent<CircleCollider2D>();
            portalCollider.isTrigger = true;
            portalCollider.radius = 1.9f;
            portal.AddComponent<SoulKnightPortal>().Initialize(director, mode);
            return portal;
        }

        public static SpriteRenderer CreateSprite(Transform root, string name, Vector2 pos, Vector2 scale, string spriteName, Color color, int sorting)
        {
            return Art2DUtility.CreateSpriteObject(root, name, pos, scale, spriteName, color, sorting);
        }

        private static RoomTrigger2D CreateRoomTrigger(Transform root, Vector2 center, int stage, bool finalRoom)
        {
            GameObject triggerObject = new GameObject("CombatRoomTrigger_1-" + stage, typeof(BoxCollider2D), typeof(RoomTrigger2D));
            triggerObject.transform.SetParent(root, false);
            triggerObject.transform.localPosition = new Vector3(center.x, center.y, 0f);
            BoxCollider2D collider = triggerObject.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = RoomSize - new Vector2(10f, 10f);

            return triggerObject.GetComponent<RoomTrigger2D>();
        }

        private static void CreateRoomCollisionShell(Transform root, Vector2 center, bool openNorth, bool openSouth, bool openWest, bool openEast)
        {
            CreateHorizontalBoundaryColliderOnly(root, "NorthWall", center, RoomSize.y * 0.5f, openNorth);
            CreateHorizontalBoundaryColliderOnly(root, "SouthWall", center, -RoomSize.y * 0.5f, openSouth);
            CreateVerticalBoundaryColliderOnly(root, "WestWall", center, -RoomSize.x * 0.5f, openWest);
            CreateVerticalBoundaryColliderOnly(root, "EastWall", center, RoomSize.x * 0.5f, openEast);
        }

        private static void CreateRoomShell(Transform root, Vector2 center, Color floorColor, bool openNorth, bool openSouth, bool openWest, bool openEast)
        {
            CreateSprite(root, "Floor", center, RoomSize, "Floor_Sprite", floorColor, -10);
            AddFloorTiles(root, center, RoomSize, 1.6f, new Color(1f, 1f, 1f, 0.025f));
            Color wallColor = new Color(0.05f, 0.055f, 0.065f);
            CreateHorizontalBoundary(root, "NorthWall", center, RoomSize.y * 0.5f, openNorth, wallColor);
            CreateHorizontalBoundary(root, "SouthWall", center, -RoomSize.y * 0.5f, openSouth, wallColor);
            CreateVerticalBoundary(root, "WestWall", center, -RoomSize.x * 0.5f, openWest, wallColor);
            CreateVerticalBoundary(root, "EastWall", center, RoomSize.x * 0.5f, openEast, wallColor);
        }

        private static void AddDungeonRoomDecor(Transform root, Vector2 center, int stage, bool isStart, bool isFinal)
        {
            Color accent = isFinal ? new Color(0.22f, 0.6f, 0.75f) : new Color(0.35f, 0.22f, 0.16f);
            if (isStart)
            {
                CreateSprite(root, "StageSpawnRune", center + new Vector2(0f, -2.3f), new Vector2(1.25f, 1.25f), "Projectile_Wand", new Color(0.35f, 0.82f, 1f), -2);
                CreateSign(root, center + new Vector2(0f, -4.55f), "起始安全房");
                CreatePathArrow(root, center + new Vector2(0f, 1.6f), 90f, new Color(0.35f, 0.82f, 1f, 0.9f));
                CreateSprite(root, "HealingFountainArt", center + new Vector2(6f, 2.5f), new Vector2(2f, 2f), "Environment/Interactables/HealingFountain", Color.white, 3);
            }
            else if (isFinal)
            {
                CreateCircle(root, "RewardRoomRune", center, new Vector2(2.3f, 2.3f), new Color(0.1f, 0.8f, 0.45f, 0.18f), -2);
                CreateSign(root, center + new Vector2(0f, -2.25f), stage < 4 ? "开箱后进下一关" : "通往 Boss");
                CreateSprite(root, "CoreAltarArt", center + new Vector2(0f, 3.5f), new Vector2(2.2f, 2.2f), "Environment/Interactables/CoreAltar", Color.white, 3);
            }
            else
            {
                CreateCircle(root, "CombatWarningRune", center, new Vector2(2.4f, 2.4f), new Color(1f, 0.18f, 0.12f, 0.14f), -2);
                CreateSprite(root, "CombatCrossA", center, new Vector2(2.1f, 0.2f), "Floor_Sprite", new Color(0.92f, 0.12f, 0.1f, 0.72f), -1).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                CreateSprite(root, "CombatCrossB", center, new Vector2(2.1f, 0.2f), "Floor_Sprite", new Color(0.92f, 0.12f, 0.1f, 0.72f), -1).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                CreateSign(root, center + new Vector2(0f, -4.55f), "进入后锁门战斗");
            }

            CreateRuinedPillar(root, center + new Vector2(-8.5f, 4.6f), accent);
            CreateRuinedPillar(root, center + new Vector2(8.5f, -4.6f), accent);
            CreateCratePile(root, center + new Vector2(-8.3f, -4.45f));
            if (stage >= 3)
            {
                CreateBanner(root, center + new Vector2(8.7f, 4.55f));
            }
        }

        private static void AddCrossroadDecor(Transform root, Vector2 center)
        {
            CreateCircle(root, "CrossroadRune", center, new Vector2(2.1f, 2.1f), new Color(0.25f, 0.7f, 1f, 0.18f), -2);
            CreatePathArrow(root, center + new Vector2(0f, 2.0f), 90f, new Color(0.35f, 0.82f, 1f, 0.9f));
            CreatePathArrow(root, center + new Vector2(-2.0f, 0f), 180f, new Color(0.35f, 0.82f, 1f, 0.9f));
            CreatePathArrow(root, center + new Vector2(2.0f, 0f), 0f, new Color(0.35f, 0.82f, 1f, 0.9f));
            CreateSign(root, center + new Vector2(0f, -3.1f), "选路前进");
        }

        private static void AddFloorTiles(Transform root, Vector2 center, Vector2 size, float spacing, Color color)
        {
            int xCount = Mathf.FloorToInt(size.x / spacing);
            int yCount = Mathf.FloorToInt(size.y / spacing);
            for (int x = -xCount / 2; x <= xCount / 2; x++)
            {
                CreateSprite(root, "FloorTileLineV", center + new Vector2(x * spacing, 0f), new Vector2(0.035f, size.y - 0.7f), "Floor_Sprite", color, -8);
            }

            for (int y = -yCount / 2; y <= yCount / 2; y++)
            {
                CreateSprite(root, "FloorTileLineH", center + new Vector2(0f, y * spacing), new Vector2(size.x - 0.7f, 0.035f), "Floor_Sprite", color, -8);
            }
        }

        private static void CreateCorridor(Transform root, Vector2 from, Vector2 to, SoulKnightStageProfile profile)
        {
            if (Mathf.Abs(to.y - from.y) >= Mathf.Abs(to.x - from.x))
            {
                Vector2 center = (from + to) * 0.5f;
                float length = Mathf.Abs(to.y - from.y) - RoomSize.y;
                CreateCollisionWall(root, "CorridorWestWall", center + new Vector2(-2.25f, 0f), new Vector2(0.4f, length));
                CreateCollisionWall(root, "CorridorEastWall", center + new Vector2(2.25f, 0f), new Vector2(0.4f, length));
                DungeonCrawlCombatRoomVisual.CreateCorridorVisual(root, from, to, RoomSize.x, RoomSize.y, profile);
            }
            else
            {
                Vector2 center = (from + to) * 0.5f;
                float length = Mathf.Abs(to.x - from.x) - RoomSize.x;
                CreateCollisionWall(root, "CorridorNorthWall", center + new Vector2(0f, 2.25f), new Vector2(length, 0.4f));
                CreateCollisionWall(root, "CorridorSouthWall", center + new Vector2(0f, -2.25f), new Vector2(length, 0.4f));
                DungeonCrawlCombatRoomVisual.CreateCorridorVisual(root, from, to, RoomSize.x, RoomSize.y, profile);
            }
        }

        private static void CreateDoorFrame(Transform root, Vector2 from, Vector2 to, SoulKnightStageProfile profile)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 fromDoor = from + direction * (RoomSize.y * 0.5f);
            Vector2 toDoor = to - direction * (RoomSize.y * 0.5f);
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                fromDoor = from + direction * (RoomSize.x * 0.5f);
                toDoor = to - direction * (RoomSize.x * 0.5f);
            }

            DungeonCrawlCombatRoomVisual.CreateTransitionFrame(root, fromDoor, side, profile);
            DungeonCrawlCombatRoomVisual.CreateTransitionFrame(root, toDoor, side, profile);
        }

        private static void CreateWall(Transform root, string name, Vector2 pos, Vector2 scale, Color color)
        {
            GameObject wall = CreateSprite(root, name, pos, scale, "Wall_Sprite", color, 1).gameObject;
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void CreateCollisionWall(Transform root, string name, Vector2 pos, Vector2 scale)
        {
            GameObject wall = new GameObject(name, typeof(BoxCollider2D));
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            wall.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void CreateHorizontalBoundary(Transform root, string name, Vector2 center, float yOffset, bool hasDoor, Color color)
        {
            if (!hasDoor)
            {
                CreateWall(root, name, center + new Vector2(0f, yOffset), new Vector2(RoomSize.x, 0.48f), color);
                return;
            }

            float segment = (RoomSize.x - DoorOpening) * 0.5f;
            float offset = DoorOpening * 0.5f + segment * 0.5f;
            CreateWall(root, name + "Left", center + new Vector2(-offset, yOffset), new Vector2(segment, 0.48f), color);
            CreateWall(root, name + "Right", center + new Vector2(offset, yOffset), new Vector2(segment, 0.48f), color);
        }

        private static void CreateHorizontalBoundaryColliderOnly(Transform root, string name, Vector2 center, float yOffset, bool hasDoor)
        {
            if (!hasDoor)
            {
                CreateCollisionWall(root, name, center + new Vector2(0f, yOffset), new Vector2(RoomSize.x, 0.48f));
                return;
            }

            float segment = (RoomSize.x - DoorOpening) * 0.5f;
            float offset = DoorOpening * 0.5f + segment * 0.5f;
            CreateCollisionWall(root, name + "Left", center + new Vector2(-offset, yOffset), new Vector2(segment, 0.48f));
            CreateCollisionWall(root, name + "Right", center + new Vector2(offset, yOffset), new Vector2(segment, 0.48f));
        }

        private static void CreateVerticalBoundary(Transform root, string name, Vector2 center, float xOffset, bool hasDoor, Color color)
        {
            if (!hasDoor)
            {
                CreateWall(root, name, center + new Vector2(xOffset, 0f), new Vector2(0.48f, RoomSize.y), color);
                return;
            }

            float segment = (RoomSize.y - DoorOpening) * 0.5f;
            float offset = DoorOpening * 0.5f + segment * 0.5f;
            CreateWall(root, name + "Top", center + new Vector2(xOffset, offset), new Vector2(0.48f, segment), color);
            CreateWall(root, name + "Bottom", center + new Vector2(xOffset, -offset), new Vector2(0.48f, segment), color);
        }

        private static void CreateVerticalBoundaryColliderOnly(Transform root, string name, Vector2 center, float xOffset, bool hasDoor)
        {
            if (!hasDoor)
            {
                CreateCollisionWall(root, name, center + new Vector2(xOffset, 0f), new Vector2(0.48f, RoomSize.y));
                return;
            }

            float segment = (RoomSize.y - DoorOpening) * 0.5f;
            float offset = DoorOpening * 0.5f + segment * 0.5f;
            CreateCollisionWall(root, name + "Top", center + new Vector2(xOffset, offset), new Vector2(0.48f, segment));
            CreateCollisionWall(root, name + "Bottom", center + new Vector2(xOffset, -offset), new Vector2(0.48f, segment));
        }

        private static void CreateRoomPlaque(Transform root, Vector2 center, string value)
        {
            CreateSprite(root, "RoomPlaque", center + new Vector2(0f, RoomSize.y * 0.5f - 0.8f), new Vector2(3.8f, 0.58f), "Wall_Sprite", new Color(0.08f, 0.09f, 0.1f, 0.88f), 6);
            CreateSmallLabel(root, center + new Vector2(0f, RoomSize.y * 0.5f - 0.78f), value, 210f, 42f, 16);
        }

        private static string ResolveRoomName(int index, int count, int stage)
        {
            if (index == 0)
            {
                return "1-" + stage + " 起始房";
            }

            if (index == count - 1)
            {
                return stage < 4 ? "奖励传送房" : "Boss 入口";
            }

            switch (ResolveRewardType(stage, index))
            {
                case RoomRewardType.Gold:
                    return "金币遭遇房";
                case RoomRewardType.Vitality:
                    return "生命强化房";
                case RoomRewardType.Core:
                    return "虫核遭遇房";
                case RoomRewardType.Treasure:
                    return "宝物遭遇房";
                default:
                    return "装备遭遇房";
            }
        }

        private static string ResolveRewardRoomName(RoomRewardType rewardType)
        {
            switch (rewardType)
            {
                case RoomRewardType.Gold:
                    return "金币遭遇房";
                case RoomRewardType.Vitality:
                    return "生命强化房";
                case RoomRewardType.Core:
                    return "虫核遭遇房";
                case RoomRewardType.Treasure:
                    return "宝物遭遇房";
                default:
                    return "装备遭遇房";
            }
        }

        private static RoomRewardType ResolveRewardType(int stage, int roomIndex)
        {
            return ResolveRewardType(stage, roomIndex, SoulKnightStageProfiles.Resolve(stage));
        }

        private static RoomRewardType ResolveRewardType(int stage, int roomIndex, SoulKnightStageProfile profile)
        {
            int totalWeight = profile.GoldWeight + profile.VitalityWeight + profile.CoreWeight + profile.TreasureWeight + profile.EquipmentWeight;
            if (totalWeight <= 0)
            {
                return RoomRewardType.Equipment;
            }

            int value = Mathf.Abs(stage * 97 + roomIndex * 31) % totalWeight;
            if ((value -= profile.GoldWeight) < 0)
            {
                return RoomRewardType.Gold;
            }

            if ((value -= profile.VitalityWeight) < 0)
            {
                return RoomRewardType.Vitality;
            }

            if ((value -= profile.CoreWeight) < 0)
            {
                return RoomRewardType.Core;
            }

            if ((value -= profile.TreasureWeight) < 0)
            {
                return RoomRewardType.Treasure;
            }

            return RoomRewardType.Equipment;
        }

        private static void CreateSign(Transform root, Vector2 pos, string value)
        {
            CreateSprite(root, "SmallSignBoard", pos, new Vector2(3.15f, 0.72f), "Wall_Sprite", new Color(0.08f, 0.075f, 0.06f, 0.94f), 12);
            CreateSmallLabel(root, pos + new Vector2(0f, 0.03f), value, 260f, 48f, 18);
        }

        private static void CreatePathArrow(Transform root, Vector2 pos, float angle, Color color)
        {
            GameObject arrow = new GameObject("PathArrow");
            arrow.transform.SetParent(root, false);
            arrow.transform.localPosition = pos;
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            CreateSprite(arrow.transform, "ArrowStem", Vector2.zero, new Vector2(1.25f, 0.24f), "Floor_Sprite", color, 3);
            CreateSprite(arrow.transform, "ArrowHeadA", new Vector2(0.58f, 0.22f), new Vector2(0.68f, 0.22f), "Floor_Sprite", color, 3).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            CreateSprite(arrow.transform, "ArrowHeadB", new Vector2(0.58f, -0.22f), new Vector2(0.68f, 0.22f), "Floor_Sprite", color, 3).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        private static GameObject CreateChestVisual(Transform root, Vector2 pos, SoulKnightChestMode mode)
        {
            string chestName;
            string spriteName;
            switch (mode)
            {
                case SoulKnightChestMode.BossReward:
                    chestName = "BossChest";
                    spriteName = "Environment/Interactables/Chest_Boss_Closed";
                    break;
                case SoulKnightChestMode.Starter:
                    chestName = "StarterChest";
                    spriteName = "Environment/Interactables/Chest_Common_Closed";
                    break;
                default:
                    chestName = "TreasureChest";
                    spriteName = "Environment/Interactables/Chest_Treasure_Closed";
                    break;
            }

            GameObject chest = new GameObject(chestName);
            chest.transform.SetParent(root, false);
            chest.transform.localPosition = pos;
            CreateSprite(chest.transform, "ChestShadow", new Vector2(0f, -0.18f), new Vector2(1.85f, 0.42f), "Floor_Sprite", new Color(0f, 0f, 0f, 0.24f), 7);
            CreateSprite(chest.transform, "ChestArt", Vector2.zero, new Vector2(1.8f, 1.8f), spriteName, Color.white, 8);
            return chest;
        }

        private static GameObject CreatePortal(Transform root, string name, Vector2 pos, bool gold)
        {
            GameObject portal = new GameObject(name);
            portal.transform.SetParent(root, false);
            portal.transform.localPosition = pos;
            CreateSprite(portal.transform, "PortalArt", Vector2.zero, Vector2.one, "Environment/Lobby/Lobby_Portal", Color.white, 8);
            return portal;
        }

        private static void CreateClassStatue(Transform root, Vector2 pos, HeroClass heroClass, Color color)
        {
            string className = RunProgressionSystem.GetClassName(heroClass);
            GameObject statue = new GameObject(className + "职业雕像");
            statue.transform.SetParent(root, false);
            statue.transform.localPosition = pos;
            CreateSprite(statue.transform, "StatueBase", Vector2.zero, new Vector2(1.75f, 0.5f), "Wall_Sprite", new Color(0.38f, 0.4f, 0.42f), 6);
            CreateSprite(statue.transform, "StatueBody", new Vector2(0f, 0.55f), new Vector2(1.05f, 1.45f), "Player_Sprite", color, 7);
            CreateCircle(statue.transform, "ClassGem", new Vector2(0f, 1.45f), new Vector2(0.36f, 0.36f), color * 1.35f, 8);
            CircleCollider2D collider = statue.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.45f;
            statue.AddComponent<LobbyClassStatueInteractable>();
            CreateSign(root, pos + new Vector2(0f, 2.25f), "右键选择" + className);
        }

        private static void CreateClassSelectionStatue(Transform root, Vector2 pos)
        {
            GameObject statue = new GameObject("职业雕像");
            statue.transform.SetParent(root, false);
            statue.transform.localPosition = pos;
            SpriteRenderer art = Art2DUtility.CreateLobbySpriteObject(
                statue.transform,
                "ClassStatueArt",
                Vector2.zero,
                new Vector2(3.2f, 3.2f),
                "Lobby_ClassStatue",
                Color.white,
                7);
            if (art == null)
            {
                CreateSprite(statue.transform, "ClassStatueFallback", Vector2.zero, new Vector2(2.4f, 2.4f), "Art2D/ArtIntegration/Environment/Lobby/Lobby_ClassStatue", Color.white, 7);
            }

            CircleCollider2D collider = statue.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 2.5f;
            statue.AddComponent<LobbyClassStatueInteractable>();
            CreateSign(root, pos + new Vector2(0f, 4.5f), "职业雕像");
        }

        private static void CreateWeaponForge(Transform root, Vector2 pos)
        {
            GameObject forge = new GameObject("职业武器铺");
            forge.transform.SetParent(root, false);
            forge.transform.localPosition = pos;
            CreateSprite(forge.transform, "WeaponForgeArt", Vector2.zero, Vector2.one, "Environment/Lobby/Lobby_WeaponForge", Color.white, 8);
            SpriteRenderer forgeArt = forge.transform.Find("WeaponForgeArt")?.GetComponent<SpriteRenderer>();
            if (forgeArt != null)
            {
                Sprite forgeSprite = Art2DUtility.LoadLobbySprite("Lobby_WeaponForge");
                if (forgeSprite != null)
                {
                    forgeArt.sprite = forgeSprite;
                }
            }
            CircleCollider2D collider = forge.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 2.5f;
            forge.AddComponent<LobbyWeaponForgeInteractable>();
        }

        private static void CreateWeaponRack(Transform root, Vector2 pos)
        {
            CreateSprite(root, "WeaponRackBack", pos, new Vector2(2.7f, 0.95f), "Wall_Sprite", new Color(0.32f, 0.19f, 0.09f), 4);
            CreateSprite(root, "WeaponRackBladeA", pos + new Vector2(-0.55f, 0.24f), new Vector2(0.16f, 1.15f), "Floor_Sprite", new Color(0.85f, 0.88f, 0.92f), 6).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            CreateSprite(root, "WeaponRackBladeB", pos + new Vector2(0.35f, 0.25f), new Vector2(0.16f, 1.15f), "Floor_Sprite", new Color(0.85f, 0.88f, 0.92f), 6).transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            CreateSprite(root, "WeaponRackHandle", pos + new Vector2(0f, -0.17f), new Vector2(2.25f, 0.16f), "Floor_Sprite", new Color(0.12f, 0.08f, 0.04f), 7);
        }

        private static void CreateShopCounter(Transform root, Vector2 pos)
        {
            Art2DUtility.CreateLobbySpriteObject(root, "LobbyShopCounter", pos, new Vector2(3.2f, 2.2f), "Lobby_ShopCounter", Color.white, 4);
        }

        private static void CreateTrainingTarget(Transform root, Vector2 pos)
        {
            CreateCircle(root, "TargetOuter", pos, new Vector2(1.08f, 1.08f), new Color(0.94f, 0.18f, 0.14f), 5);
            CreateCircle(root, "TargetMid", pos, new Vector2(0.72f, 0.72f), Color.white, 6);
            CreateCircle(root, "TargetCore", pos, new Vector2(0.34f, 0.34f), new Color(0.94f, 0.18f, 0.14f), 7);
            CreateSprite(root, "TargetStand", pos + new Vector2(0f, -0.8f), new Vector2(0.18f, 0.96f), "Floor_Sprite", new Color(0.32f, 0.22f, 0.12f), 4);
        }

        private static void CreateBulletinBoard(Transform root, Vector2 pos)
        {
            CreateSprite(root, "BulletinBoard", pos, new Vector2(1.75f, 1.1f), "Wall_Sprite", new Color(0.26f, 0.17f, 0.1f), 4);
            CreateSprite(root, "PinnedNoteA", pos + new Vector2(-0.38f, 0.15f), new Vector2(0.42f, 0.5f), "Floor_Sprite", new Color(0.9f, 0.82f, 0.56f), 6);
            CreateSprite(root, "PinnedNoteB", pos + new Vector2(0.42f, -0.05f), new Vector2(0.46f, 0.55f), "Floor_Sprite", new Color(0.84f, 0.72f, 0.48f), 6);
        }

        private static void CreatePillarTorch(Transform root, Vector2 pos)
        {
            CreateSprite(root, "StonePillar", pos, new Vector2(0.72f, 0.72f), "Wall_Sprite", new Color(0.42f, 0.45f, 0.43f), 4);
            CreateCircle(root, "TorchGlow", pos + new Vector2(0f, 0.52f), new Vector2(0.38f, 0.38f), new Color(1f, 0.72f, 0.24f, 0.9f), 7);
            CreateCircle(root, "TorchCore", pos + new Vector2(0f, 0.52f), new Vector2(0.18f, 0.18f), new Color(1f, 0.95f, 0.55f), 8);
        }

        private static void CreateRuinedPillar(Transform root, Vector2 pos, Color color)
        {
            CreateSprite(root, "RuinedPillarBase", pos, new Vector2(0.78f, 0.95f), "Wall_Sprite", color, 3);
            CreateSprite(root, "RuinedPillarCrack", pos + new Vector2(0.12f, 0.08f), new Vector2(0.08f, 0.74f), "Floor_Sprite", new Color(0.04f, 0.04f, 0.05f), 4).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        }

        private static void CreateCratePile(Transform root, Vector2 pos)
        {
            CreateSprite(root, "CrateA", pos + new Vector2(-0.28f, 0f), new Vector2(0.72f, 0.72f), "Wall_Sprite", new Color(0.38f, 0.24f, 0.12f), 3);
            CreateSprite(root, "CrateB", pos + new Vector2(0.35f, 0.14f), new Vector2(0.64f, 0.64f), "Wall_Sprite", new Color(0.44f, 0.28f, 0.14f), 4);
            CreateSprite(root, "CrateBand", pos + new Vector2(0.02f, 0.02f), new Vector2(1.35f, 0.09f), "Floor_Sprite", new Color(0.18f, 0.1f, 0.04f), 5);
        }

        private static void CreateBanner(Transform root, Vector2 pos)
        {
            CreateSprite(root, "ArcaneBannerPole", pos + new Vector2(-0.35f, 0f), new Vector2(0.12f, 1.5f), "Wall_Sprite", new Color(0.18f, 0.12f, 0.08f), 4);
            CreateSprite(root, "ArcaneBannerCloth", pos + new Vector2(0f, 0.12f), new Vector2(0.7f, 1.15f), "Floor_Sprite", new Color(0.42f, 0.12f, 0.55f), 5);
            CreateCircle(root, "BannerMark", pos + new Vector2(0f, 0.28f), new Vector2(0.22f, 0.22f), new Color(0.9f, 0.7f, 1f), 6);
        }

        private static SpriteRenderer CreateCircle(Transform root, string name, Vector2 pos, Vector2 scale, Color color, int sorting)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(root, false);
            obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Art2DUtility.GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = sorting;
            return renderer;
        }

        private static Color ResolveFloorColor(int stage, bool finalRoom)
        {
            if (finalRoom)
            {
                return new Color(0.17f, 0.18f, 0.23f);
            }

            switch (stage)
            {
                case 2:
                    return new Color(0.18f, 0.17f, 0.22f);
                case 3:
                    return new Color(0.19f, 0.16f, 0.16f);
                case 4:
                    return new Color(0.15f, 0.18f, 0.22f);
                default:
                    return new Color(0.16f, 0.19f, 0.17f);
            }
        }

        private static void CreateLabel(Transform root, Vector2 pos, string value)
        {
            GameObject canvasObject = new GameObject("RoomLabel", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root, false);
            canvasObject.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.018f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 110f);
            Text text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(canvasObject.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 23;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
        }

        private static void CreateSmallLabel(Transform root, Vector2 pos, string value, float width, float height, int fontSize)
        {
            GameObject canvasObject = new GameObject("SmallWorldLabel", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root, false);
            canvasObject.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.018f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 24;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            Text text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(canvasObject.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(18, fontSize);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
        }
    }
}
