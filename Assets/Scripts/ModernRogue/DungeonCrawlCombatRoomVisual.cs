using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    public static class DungeonCrawlCombatRoomVisual
    {
        public enum RoomVisualStyle
        {
            Start,
            Crossroad,
            Shop,
            Combat,
            Treasure,
            Boss
        }

        private const string ResourceRoot = "ArtIntegration/Environment/DungeonCrawlCombatRoom/";
        private const float RoomWidth = 30f;
        private const float RoomHeight = 20f;
        private const float HalfRoomWidth = RoomWidth * 0.5f;
        private const float HalfRoomHeight = RoomHeight * 0.5f;
        private const float DoorOpening = 7.6f;
        private const float TileStep = 1f;
        private const int FloorSorting = -11;
        private const int AccentSorting = -10;
        private const int RecessSorting = 1;
        private const int WallSorting = 2;
        private const int DoorFrameSorting = 5;
        private const int DecorSorting = 4;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static void Create(
            Transform root,
            Vector2 center,
            bool openNorth,
            bool openSouth,
            bool openWest,
            bool openEast,
            RoomVisualStyle style,
            SoulKnightStageProfile profile,
            bool signatureRoom)
        {
            GameObject visualRoot = new GameObject(ResolveRootName(style));
            visualRoot.transform.SetParent(root, false);
            visualRoot.transform.localPosition = new Vector3(center.x, center.y, 0f);
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.transform.localScale = Vector3.one;

            int seed = Mathf.RoundToInt(center.x * 11f + center.y * 17f + (profile != null ? profile.Stage * 101f : 0f) + (signatureRoom ? 53f : 0f));

            BuildFloor(visualRoot.transform, seed, style, profile, signatureRoom);
            BuildHorizontalWall(visualRoot.transform, "North", HalfRoomHeight - 0.5f, openNorth, false, profile);
            BuildHorizontalWall(visualRoot.transform, "South", -HalfRoomHeight + 0.5f, openSouth, true, profile);
            BuildVerticalWall(visualRoot.transform, "West", -HalfRoomWidth + 0.5f, openWest, false, profile);
            BuildVerticalWall(visualRoot.transform, "East", HalfRoomWidth - 0.5f, openEast, true, profile);
            AddDecor(visualRoot.transform, seed, style, profile, signatureRoom);
        }

        public static void CreateCorridorVisual(Transform root, Vector2 from, Vector2 to, float roomWidth, float roomHeight, SoulKnightStageProfile profile)
        {
            Vector2 center = (from + to) * 0.5f;
            bool vertical = Mathf.Abs(to.y - from.y) >= Mathf.Abs(to.x - from.x);
            if (vertical)
            {
                float length = Mathf.Abs(to.y - from.y) - roomHeight;
                BuildCorridorFloor(root, center, new Vector2(4f, length), profile);
                BuildCorridorSide(root, center + new Vector2(-2.35f, 0f), length, true, profile);
                BuildCorridorSide(root, center + new Vector2(2.35f, 0f), length, true, profile);
                return;
            }

            float horizontalLength = Mathf.Abs(to.x - from.x) - roomWidth;
            BuildCorridorFloor(root, center, new Vector2(horizontalLength, 4f), profile);
            BuildCorridorSide(root, center + new Vector2(0f, 2.35f), horizontalLength, false, profile);
            BuildCorridorSide(root, center + new Vector2(0f, -2.35f), horizontalLength, false, profile);
        }

        public static void CreateTransitionFrame(Transform root, Vector2 center, Vector2 side, SoulKnightStageProfile profile)
        {
            if (Mathf.Abs(side.x) > Mathf.Abs(side.y))
            {
                CreateHorizontalDoorwayAt(root, center, side.x < 0f, "Door", profile);
                return;
            }

            CreateVerticalDoorwayAt(root, center, side.y < 0f, "Door", profile);
        }

        private static string ResolveRootName(RoomVisualStyle style)
        {
            switch (style)
            {
                case RoomVisualStyle.Start:
                    return "DungeonCrawlStartRoom_Visual";
                case RoomVisualStyle.Crossroad:
                    return "DungeonCrawlCrossroadRoom_Visual";
                case RoomVisualStyle.Shop:
                    return "DungeonCrawlShopRoom_Visual";
                case RoomVisualStyle.Treasure:
                    return "DungeonCrawlTreasureRoom_Visual";
                case RoomVisualStyle.Boss:
                    return "DungeonCrawlBossRoom_Visual";
                default:
                    return "DungeonCrawlCombatRoom_Visual";
            }
        }

        private static void BuildFloor(Transform root, int seed, RoomVisualStyle style, SoulKnightStageProfile profile, bool signatureRoom)
        {
            Sprite floor = LoadSprite("crypt_10");
            Sprite accent = LoadSprite("cobble_blood_1_new");
            Color baseTint = ResolveBaseFloorTint(style, profile);

            int xCount = Mathf.RoundToInt(RoomWidth / TileStep);
            int yCount = Mathf.RoundToInt(RoomHeight / TileStep);
            for (int x = 0; x < xCount; x++)
            {
                float localX = -HalfRoomWidth + 0.5f + x * TileStep;
                for (int y = 0; y < yCount; y++)
                {
                    float localY = -HalfRoomHeight + 0.5f + y * TileStep;
                    Color tileTint = ResolveFloorTileTint(x, y, seed, style, baseTint);
                    CreateSprite(root, "FloorTile", new Vector2(localX, localY), Vector2.one, floor, FloorSorting, 0f, tileTint);

                    if (ShouldPlaceAccentTile(x, y, seed, style))
                    {
                        Color accentTint = ResolveAccentTint(style, x, y, seed, profile, signatureRoom);
                        CreateSprite(root, "FloorAccent", new Vector2(localX, localY), Vector2.one, accent, AccentSorting, 0f, accentTint);
                    }
                }
            }
        }

        private static Color ResolveBaseFloorTint(RoomVisualStyle style, SoulKnightStageProfile profile)
        {
            Color roomTint;
            switch (style)
            {
                case RoomVisualStyle.Start:
                    roomTint = new Color(0.88f, 0.9f, 0.95f, 1f);
                    break;
                case RoomVisualStyle.Crossroad:
                    roomTint = new Color(0.84f, 0.9f, 0.92f, 1f);
                    break;
                case RoomVisualStyle.Shop:
                    roomTint = new Color(0.95f, 0.9f, 0.82f, 1f);
                    break;
                case RoomVisualStyle.Treasure:
                    roomTint = new Color(0.96f, 0.92f, 0.82f, 1f);
                    break;
                case RoomVisualStyle.Boss:
                    roomTint = new Color(0.82f, 0.82f, 0.9f, 1f);
                    break;
                default:
                    roomTint = new Color(0.92f, 0.92f, 0.92f, 1f);
                    break;
            }

            return profile != null ? MultiplyColor(roomTint, profile.BaseFloorTint) : roomTint;
        }

        private static Color ResolveFloorTileTint(int x, int y, int seed, RoomVisualStyle style, Color baseTint)
        {
            float noise = ((x * 17 + y * 13 + seed) % 9) * 0.022f;
            float diagonal = ((x - y + seed) % 7) == 0 ? -0.06f : 0f;
            float edgeDarken =
                Mathf.InverseLerp(0f, 3f, Mathf.Min(x, Mathf.Abs(x - 29))) * 0.03f +
                Mathf.InverseLerp(0f, 3f, Mathf.Min(y, Mathf.Abs(y - 19))) * 0.03f;
            float modifier = 0.88f + noise + diagonal - edgeDarken;

            if (style == RoomVisualStyle.Crossroad && (x == 14 || y == 9))
            {
                modifier += 0.05f;
            }

            if (style == RoomVisualStyle.Start && (x + y + seed) % 11 == 0)
            {
                modifier += 0.04f;
            }

            if (style == RoomVisualStyle.Boss)
            {
                modifier -= 0.06f;
            }

            return new Color(baseTint.r * modifier, baseTint.g * modifier, baseTint.b * modifier, 1f);
        }

        private static Color ResolveAccentTint(RoomVisualStyle style, int x, int y, int seed, SoulKnightStageProfile profile, bool signatureRoom)
        {
            Color baseColor;
            if (style == RoomVisualStyle.Boss)
            {
                baseColor = new Color(0.78f, 0.58f, 0.66f, 0.52f);
            }
            else if (style == RoomVisualStyle.Shop || style == RoomVisualStyle.Treasure)
            {
                baseColor = new Color(0.95f, 0.86f, 0.66f, ((x + y + seed) & 1) == 0 ? 0.42f : 0.32f);
            }
            else
            {
                baseColor = new Color(0.9f, 0.88f, 0.84f, ((x + y + seed) & 1) == 0 ? 0.32f : 0.22f);
            }

            if (profile != null)
            {
                Color profileTint = profile.AccentTint;
                profileTint.a = baseColor.a;
                baseColor = Color.Lerp(baseColor, profileTint, signatureRoom ? 0.72f : 0.4f);
            }

            return baseColor;
        }

        private static bool ShouldPlaceAccentTile(int x, int y, int seed, RoomVisualStyle style)
        {
            int value = x * 31 + y * 17 + seed;
            if (style == RoomVisualStyle.Boss)
            {
                return value % 13 == 0 || (x > 11 && x < 18 && y > 6 && y < 13 && value % 5 == 0);
            }

            if (style == RoomVisualStyle.Crossroad)
            {
                return value % 19 == 0 || x == 14 || y == 9;
            }

            if (style == RoomVisualStyle.Start)
            {
                return value % 21 == 0 || (x > 12 && x < 17 && y > 5 && y < 10 && value % 3 == 0);
            }

            if (style == RoomVisualStyle.Shop || style == RoomVisualStyle.Treasure)
            {
                return value % 17 == 0 || (x + y + seed) % 23 == 0;
            }

            return value % 23 == 0 || (x + y + seed) % 41 == 0;
        }

        private static void BuildFloorEdgeShadow(Transform root, SoulKnightStageProfile profile)
        {
            Color edgeShadow = profile != null
                ? new Color(profile.CorridorShadowTint.r, profile.CorridorShadowTint.g, profile.CorridorShadowTint.b, Mathf.Clamp01(profile.CorridorShadowAlpha * 0.48f))
                : new Color(0f, 0f, 0f, 0.18f);
            CreateSprite(root, "NorthEdgeShadow", new Vector2(0f, HalfRoomHeight - 1.15f), new Vector2(RoomWidth - 2f, 1.55f), Art2DUtility.GetCircleSprite(), FloorSorting + 1, 0f, edgeShadow);
            CreateSprite(root, "SouthEdgeShadow", new Vector2(0f, -HalfRoomHeight + 1.15f), new Vector2(RoomWidth - 2f, 1.55f), Art2DUtility.GetCircleSprite(), FloorSorting + 1, 0f, edgeShadow);
            CreateSprite(root, "WestEdgeShadow", new Vector2(-HalfRoomWidth + 1.15f, 0f), new Vector2(1.55f, RoomHeight - 2f), Art2DUtility.GetCircleSprite(), FloorSorting + 1, 0f, edgeShadow);
            CreateSprite(root, "EastEdgeShadow", new Vector2(HalfRoomWidth - 1.15f, 0f), new Vector2(1.55f, RoomHeight - 2f), Art2DUtility.GetCircleSprite(), FloorSorting + 1, 0f, edgeShadow);
        }

        private static void BuildHorizontalWall(Transform root, string sideName, float y, bool hasDoor, bool flipDoor, SoulKnightStageProfile profile)
        {
            for (int x = 0; x < Mathf.RoundToInt(RoomWidth); x++)
            {
                float localX = -HalfRoomWidth + 0.5f + x * TileStep;
                if (hasDoor && Mathf.Abs(localX) < DoorOpening * 0.5f)
                {
                    continue;
                }

                Sprite wall = ((x + (y > 0f ? 1 : 0)) & 1) == 0 ? LoadSprite("brick_dark_0") : LoadSprite("brick_gray_0");
                CreateSprite(root, sideName + "WallTile", new Vector2(localX, y), Vector2.one, wall, WallSorting, 0f, ResolveWallTint(profile));
            }

            if (hasDoor)
            {
                CreateHorizontalDoorwayAt(root, new Vector2(0f, y), flipDoor, sideName, profile);
            }
        }

        private static void BuildVerticalWall(Transform root, string sideName, float x, bool hasDoor, bool flipDoor, SoulKnightStageProfile profile)
        {
            for (int y = 0; y < Mathf.RoundToInt(RoomHeight); y++)
            {
                float localY = -HalfRoomHeight + 0.5f + y * TileStep;
                if (hasDoor && Mathf.Abs(localY) < DoorOpening * 0.5f)
                {
                    continue;
                }

                Sprite wall = ((y + (x > 0f ? 1 : 0)) & 1) == 0 ? LoadSprite("brick_dark_0") : LoadSprite("brick_gray_0");
                CreateSprite(root, sideName + "WallTile", new Vector2(x, localY), Vector2.one, wall, WallSorting, 90f, ResolveWallTint(profile));
            }

            if (hasDoor)
            {
                CreateVerticalDoorwayAt(root, new Vector2(x, 0f), flipDoor, sideName, profile);
            }
        }

        private static void CreateHorizontalDoorwayAt(Transform root, Vector2 position, bool flipFrame, string sideName, SoulKnightStageProfile profile)
        {
            Sprite floorTile = LoadSprite("crypt_10");
            Color passageColor = ResolveDoorPassageColor(profile);

            for (int i = 0; i < 6; i++)
            {
                float tx = position.x - 2.5f + i;
                CreateSprite(root, sideName + "PassFloor", new Vector2(tx, position.y), Vector2.one, floorTile, RecessSorting, 0f, passageColor);
            }
        }

        private static void CreateVerticalDoorwayAt(Transform root, Vector2 position, bool flipFrame, string sideName, SoulKnightStageProfile profile)
        {
            Sprite floorTile = LoadSprite("crypt_10");
            Color passageColor = ResolveDoorPassageColor(profile);

            for (int i = 0; i < 6; i++)
            {
                float ty = position.y - 2.5f + i;
                CreateSprite(root, sideName + "PassFloor", new Vector2(position.x, ty), Vector2.one, floorTile, RecessSorting, 0f, passageColor);
            }
        }

        private static void BuildCorridorFloor(Transform root, Vector2 center, Vector2 size, SoulKnightStageProfile profile)
        {
            Sprite floor = LoadSprite("crypt_10");
            Sprite accent = LoadSprite("cobble_blood_1_new");
            int xCount = Mathf.Max(1, Mathf.RoundToInt(size.x / TileStep));
            int yCount = Mathf.Max(1, Mathf.RoundToInt(size.y / TileStep));
            for (int x = 0; x < xCount; x++)
            {
                float localX = center.x - size.x * 0.5f + 0.5f + x * TileStep;
                for (int y = 0; y < yCount; y++)
                {
                    float localY = center.y - size.y * 0.5f + 0.5f + y * TileStep;
                    float modifier = 0.72f + (((x * 5 + y * 3) % 5) * 0.035f);
                    Color corridorBase = profile != null ? MultiplyColor(new Color(modifier, modifier, modifier, 1f), profile.BaseFloorTint) : new Color(modifier, modifier, modifier, 1f);
                    CreateSprite(root, "CorridorFloorTile", new Vector2(localX, localY), Vector2.one, floor, FloorSorting + 1, 0f, corridorBase);
                    if (((x * 13) + (y * 7)) % 9 == 0)
                    {
                        Color accentColor = profile != null ? new Color(profile.AccentTint.r, profile.AccentTint.g, profile.AccentTint.b, 0.26f) : new Color(0.7f, 0.66f, 0.62f, 0.22f);
                        CreateSprite(root, "CorridorFloorAccent", new Vector2(localX, localY), Vector2.one, accent, AccentSorting + 1, 0f, accentColor);
                    }
                }
            }
        }

        private static void BuildCorridorSide(Transform root, Vector2 center, float length, bool vertical, SoulKnightStageProfile profile)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(length / TileStep));
            for (int i = 0; i < count; i++)
            {
                float offset = -length * 0.5f + 0.5f + i * TileStep;
                Vector2 pos = vertical ? center + new Vector2(0f, offset) : center + new Vector2(offset, 0f);
                float rotation = vertical ? 90f : 0f;
                Sprite wall = (i & 1) == 0 ? LoadSprite("brick_dark_0") : LoadSprite("brick_gray_0");
                CreateSprite(root, "CorridorWallTile", pos, new Vector2(1.25f, 1.25f), wall, WallSorting, rotation, ResolveWallTint(profile));
            }
        }




        private static void AddDecor(Transform root, int seed, RoomVisualStyle style, SoulKnightStageProfile profile, bool signatureRoom)
        {
            AddWallTorch(root, new Vector2(-7.5f, HalfRoomHeight - 1.75f));
            AddWallTorch(root, new Vector2(7.3f, HalfRoomHeight - 1.45f));
            AddWallTorch(root, new Vector2(-6.9f, -HalfRoomHeight + 1.45f));
            AddWallTorch(root, new Vector2(7.6f, -HalfRoomHeight + 1.8f));

            AddCrateCluster(root, new Vector2(-HalfRoomWidth + 2.35f, -HalfRoomHeight + 1.7f), seed % 2 == 0, style == RoomVisualStyle.Start || style == RoomVisualStyle.Crossroad);
            AddCrateCluster(root, new Vector2(HalfRoomWidth - 2.65f, -HalfRoomHeight + 1.82f), seed % 3 == 0, true);

            AddRoomDecorArt(root, seed, style, profile);

            if (style == RoomVisualStyle.Crossroad)
            {
                AddWallTorch(root, new Vector2(-HalfRoomWidth + 2.3f, 0f));
                AddWallTorch(root, new Vector2(HalfRoomWidth - 2.3f, 0f));
            }

            if (style == RoomVisualStyle.Shop)
            {
                AddCrateCluster(root, new Vector2(HalfRoomWidth - 2.8f, HalfRoomHeight - 2.25f), true, true);
                AddCrateCluster(root, new Vector2(-HalfRoomWidth + 2.5f, HalfRoomHeight - 2.05f), false, false);
            }

            if (style == RoomVisualStyle.Treasure)
            {
                AddCrateCluster(root, new Vector2(HalfRoomWidth - 2.8f, HalfRoomHeight - 2.1f), true, true);
                AddCrateCluster(root, new Vector2(HalfRoomWidth - 1.95f, HalfRoomHeight - 2.9f), false, false);
            }

            if (style == RoomVisualStyle.Boss)
            {
                CreateSprite(root, "BossAltarShadow", new Vector2(0f, HalfRoomHeight - 2.35f), new Vector2(2.1f, 0.92f), Art2DUtility.GetCircleSprite(), DecorSorting, 0f, new Color(0f, 0f, 0f, 0.28f));
                CreateSprite(root, "BossAltar", new Vector2(0f, HalfRoomHeight - 2.1f), new Vector2(1.72f, 1.72f), LoadSprite("altar_base"), DecorSorting + 1);
                AddWallTorch(root, new Vector2(-3.4f, HalfRoomHeight - 1.5f));
                AddWallTorch(root, new Vector2(3.4f, HalfRoomHeight - 1.5f));
                AddCrateCluster(root, new Vector2(-HalfRoomWidth + 2.55f, HalfRoomHeight - 2.35f), false, true);
                AddCrateCluster(root, new Vector2(HalfRoomWidth - 2.55f, HalfRoomHeight - 2.35f), true, true);
                AddArtDecor(root, "BossRuneCircle", new Vector2(0f, -1f), new Vector2(3.5f, 3.5f), "Environment/RoomDecor/BossRuneCircle", new Color(1f, 1f, 1f, 0.6f));
            }

            if (profile != null && profile.DenseDecor)
            {
                AddCrateCluster(root, new Vector2(-HalfRoomWidth + 2.65f, 0f), seed % 2 == 0, false);
                AddCrateCluster(root, new Vector2(HalfRoomWidth - 2.65f, 0.2f), seed % 3 == 0, false);
            }
        }

        private static void AddRoomDecorArt(Transform root, int seed, RoomVisualStyle style, SoulKnightStageProfile profile)
        {
            int hash = seed * 7;

            if (style == RoomVisualStyle.Combat || style == RoomVisualStyle.Crossroad)
            {
                if (hash % 3 == 0)
                {
                    AddArtDecor(root, "BarrelDecor", new Vector2(-HalfRoomWidth + 2.2f, HalfRoomHeight - 2.1f), new Vector2(1.4f, 1.4f), "Environment/RoomDecor/Barrel_Wood", Color.white);
                }

                if (hash % 5 == 0)
                {
                    AddArtDecor(root, "CrateDecor", new Vector2(HalfRoomWidth - 2.4f, HalfRoomHeight - 2.2f), new Vector2(1.5f, 1.5f), "Environment/RoomDecor/Crate_Wood", Color.white);
                }

                if (hash % 7 == 0)
                {
                    AddArtDecor(root, "BonePileDecor", new Vector2(-HalfRoomWidth + 3.5f, -HalfRoomHeight + 2.4f), new Vector2(1.3f, 1.3f), "Environment/RoomDecor/BonePile", Color.white);
                }

                if (hash % 11 == 0)
                {
                    AddArtDecor(root, "PillarDecor", new Vector2(HalfRoomWidth - 3.2f, 0f), new Vector2(1.2f, 2.2f), "Environment/RoomDecor/Pillar_Stone", Color.white);
                }
            }

            if (style == RoomVisualStyle.Boss)
            {
                AddArtDecor(root, "PillarLeft", new Vector2(-4.5f, 2f), new Vector2(1.2f, 2.2f), "Environment/RoomDecor/Pillar_Stone", Color.white);
                AddArtDecor(root, "PillarRight", new Vector2(4.5f, 2f), new Vector2(1.2f, 2.2f), "Environment/RoomDecor/Pillar_Stone", Color.white);
            }

            if (style == RoomVisualStyle.Treasure)
            {
                AddArtDecor(root, "BookshelfDecor", new Vector2(-HalfRoomWidth + 2.5f, HalfRoomHeight - 2.2f), new Vector2(1.5f, 1.8f), "Environment/RoomDecor/Bookshelf_Broken", Color.white);
            }

            if (style == RoomVisualStyle.Shop)
            {
                AddArtDecor(root, "ShopTableDecor", new Vector2(-3f, -2f), new Vector2(2.2f, 1.6f), "Environment/RoomDecor/ShopDisplayTable", Color.white);
            }

            if (hash % 4 == 0 && style != RoomVisualStyle.Start)
            {
                AddArtDecor(root, "RubbleDecor", new Vector2(HalfRoomWidth - 3f, -HalfRoomHeight + 2.5f), new Vector2(1.4f, 1.2f), "Environment/RoomDecor/Rubble_Stone", Color.white);
            }
        }

        private static void AddArtDecor(Transform root, string name, Vector2 position, Vector2 scale, string spriteName, Color color)
        {
            Sprite sprite = Art2DUtility.LoadSprite(spriteName);
            if (sprite == null || Art2DUtility.IsFallbackSprite(sprite))
            {
                return;
            }

            CreateSprite(root, name, position, scale, sprite, DecorSorting, 0f, color);
        }

        private static Color ResolveWallTint(SoulKnightStageProfile profile)
        {
            return profile != null ? MultiplyColor(new Color(0.9f, 0.9f, 0.9f, 1f), profile.BaseFloorTint) : Color.white;
        }

        private static Color ResolveDoorPassageColor(SoulKnightStageProfile profile)
        {
            return profile != null ? MultiplyColor(new Color(0.83f, 0.83f, 0.83f, 1f), profile.BaseFloorTint) : new Color(0.83f, 0.83f, 0.83f, 1f);
        }

        private static Color ResolveCorridorShadow(SoulKnightStageProfile profile, float multiplier)
        {
            if (profile == null)
            {
                return new Color(0f, 0f, 0f, 0.34f * multiplier / 0.38f);
            }

            return new Color(
                profile.CorridorShadowTint.r,
                profile.CorridorShadowTint.g,
                profile.CorridorShadowTint.b,
                Mathf.Clamp01(profile.CorridorShadowAlpha * multiplier));
        }

        private static Color MultiplyColor(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }

        private static void AddWallTorch(Transform root, Vector2 position)
        {
            CreateSprite(root, "Torch", position, new Vector2(1.18f, 1.18f), LoadSprite("torch_0"), DecorSorting);
        }

        private static void AddCrateCluster(Transform root, Vector2 position, bool mirrored, bool doubleStack)
        {
            Sprite boxSprite = Art2DUtility.LoadDungeonRoomSprite("large_box");
            Sprite chestSprite = Art2DUtility.LoadDungeonRoomSprite("chest_2_closed");
            if (Art2DUtility.IsFallbackSprite(boxSprite) && Art2DUtility.IsFallbackSprite(chestSprite))
            {
                return;
            }

            CreateSprite(root, "CrateShadow", position + new Vector2(0.08f, -0.14f), new Vector2(1.28f, 0.86f), Art2DUtility.GetCircleSprite(), DecorSorting - 2, 0f, new Color(0f, 0f, 0f, 0.28f));

            if (!Art2DUtility.IsFallbackSprite(boxSprite))
            {
                SpriteRenderer back = CreateSprite(
                    root,
                    "CrateBack",
                    position + new Vector2(mirrored ? -0.18f : 0.18f, 0.16f),
                    new Vector2(1.08f, 1.08f),
                    boxSprite,
                    DecorSorting - 1,
                    0f,
                    new Color(0.7f, 0.64f, 0.58f, 0.92f));
                ApplyMirror(back, mirrored);
            }

            if (!Art2DUtility.IsFallbackSprite(chestSprite))
            {
                SpriteRenderer front = CreateSprite(root, "CrateFront", position, new Vector2(1.2f, 1.2f), chestSprite, DecorSorting + 1);
                ApplyMirror(front, mirrored);
            }

            if (doubleStack && !Art2DUtility.IsFallbackSprite(boxSprite))
            {
                SpriteRenderer upper = CreateSprite(root, "CrateTop", position + new Vector2(mirrored ? -0.38f : 0.38f, 0.48f), new Vector2(0.96f, 0.96f), boxSprite, DecorSorting + 2, 0f, new Color(0.78f, 0.72f, 0.66f, 0.96f));
                ApplyMirror(upper, mirrored);
            }
        }

        private static void ApplyMirror(SpriteRenderer renderer, bool mirrored)
        {
            if (!mirrored)
            {
                return;
            }

            Vector3 scale = renderer.transform.localScale;
            renderer.transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 scale,
            Sprite sprite,
            int sortingOrder,
            float rotationZ = 0f,
            Color? color = null)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            obj.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color ?? Color.white;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite LoadSprite(string assetName)
        {
            if (SpriteCache.TryGetValue(assetName, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Art2DUtility.LoadDungeonRoomSprite(assetName);
            if (Art2DUtility.IsFallbackSprite(sprite))
            {
                Debug.LogWarning("DungeonCrawlCombatRoomVisual: Missing sprite at " + ResourceRoot + assetName);
            }

            SpriteCache[assetName] = sprite;
            return sprite;
        }
    }
}
