using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModernRogue
{
    public static class Art2DUtility
    {
        private static Sprite fallbackSprite;
        private static Sprite circleSprite;
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, string[]> aliases = new Dictionary<string, string[]>
        {
            { "Player_Sprite", new[] { "Player_Sprite", "Player_Warrior", "AnimationSprites/Player/Warrior/Warrior_Idle_01" } },
            { "Player_Warrior", new[] { "Player_Warrior", "AnimationSprites/Player/Warrior/Warrior_Idle_01" } },
            { "Player_Mage", new[] { "Player_Mage", "AnimationSprites/Player/Mage/Mage_Idle_01" } },
            { "Slime_Sprite", new[] { "Enemy_Slime", "AnimationSprites/Enemies/Slime/Slime_Idle_01" } },
            { "Enemy_Slime", new[] { "Enemy_Slime", "AnimationSprites/Enemies/Slime/Slime_Idle_01" } },
            { "Enemy_Slime_Elite", new[] { "Enemy_Slime_Elite", "AnimationSprites/Enemies/EliteSlime/EliteSlime_Idle_01" } },
            { "Enemy_SkeletonArcher", new[] { "Enemy_SkeletonArcher", "AnimationSprites/Enemies/SkeletonArcher/SkeletonArcher_Idle_01" } },
            { "Boss_Sprite", new[] { "Boss_Titan", "AnimationSprites/Bosses/Titan/Titan_Idle_01" } },
            { "Boss_Titan", new[] { "Boss_Titan", "AnimationSprites/Bosses/Titan/Titan_Idle_01" } },
            { "Boss_EmberMage", new[] { "Boss_EmberMage", "AnimationSprites/Bosses/EmberMage/EmberMage_Idle_01" } },
            { "Boss_StormGuard", new[] { "Boss_StormGuard", "AnimationSprites/Bosses/StormGuard/StormGuard_Idle_01" } },
            { "Boss_BroodQueen", new[] { "Boss_BroodQueen", "AnimationSprites/Bosses/BroodQueen/BroodQueen_Idle_01" } },
            { "Projectile_Wand", new[] { "Projectile_MageOrb", "EnhancedBossBullet" } },
            { "Projectile_Arrow", new[] { "Projectile_Arrow" } },
            { "Projectile_ChargedArrow", new[] { "Projectile_ChargedArrow", "Projectile_Arrow" } },
            { "Projectile_SkeletonArrow", new[] { "Projectile_SkeletonArrow", "Projectile_Arrow" } },
            { "Floor_Sprite", new[] { "Environment/Tiles/Floor_CombatRoom", "Environment/Tiles/Floor_Lobby" } },
            { "Wall_Sprite", new[] { "Environment/Walls/Wall_Stone_Horizontal", "Environment/Walls/Wall_Lobby_Horizontal" } },
            { "Weapon_Dagger", new[] { "Weapon_WarriorSword", "Weapon_ArcherBow", "Weapon_MageStaff" } }
        };

        public static Sprite LoadSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return GetFallbackSprite();
            }

            string[] candidates = aliases.TryGetValue(assetName, out string[] mapped) ? mapped : new[] { assetName };
            for (int i = 0; i < candidates.Length; i++)
            {
                Sprite sprite = LoadSpriteDirect(candidates[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return GetFallbackSprite();
        }

        public static Sprite LoadProjectileSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return GetFallbackSprite();
            }

            if (assetName == "Projectile_Arrow" || assetName == "Projectile_ChargedArrow")
            {
                if (TinySwordsExternalSpriteLibrary.TryGetArcherProjectileSprite(
                    assetName == "Projectile_ChargedArrow", out Sprite tinySprite) && tinySprite != null)
                {
                    return tinySprite;
                }
            }

            string[] candidates = aliases.TryGetValue(assetName, out string[] mapped)
                ? mapped
                : new[] { assetName };
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                string cacheKey = "Projectile|" + candidate;
                if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                {
                    return cached;
                }

                Texture2D texture = LoadArt2DTexture("Art2D/" + candidate);
                if (texture == null)
                {
                    continue;
                }

                StripBorderBackdrop(texture);
                Rect rect = ResolveOpaqueRect(texture);
                Vector2 pivot = candidate.Contains("Skeleton")
                    ? new Vector2(0.2f, 0.5f)
                    : new Vector2(0.15f, 0.5f);
                Vector2 clampedPivot = new Vector2(
                    Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                    Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
                float pixelsPerUnit = ResolveProjectilePixelsPerUnit(rect);
                Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
                sprite.name = candidate;
                spriteCache[cacheKey] = sprite;
                spriteCache["Projectile|" + assetName] = sprite;
                return sprite;
            }

            return GetFallbackSprite();
        }

        public static Sprite LoadBossVfxSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            string cacheKey = "BossVfx|" + assetName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = LoadArt2DTexture("Art2D/" + assetName);
            if (texture == null)
            {
                return null;
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            float pixelsPerUnit = Mathf.Clamp(Mathf.Max(rect.width, rect.height), 32f, 128f);
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
            sprite.name = assetName;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static Sprite LoadStrippedResourceSprite(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return null;
            }

            resourceName = resourceName.Replace("\\", "/").Trim('/');
            string cacheKey = "ResourceRoot|" + resourceName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = LoadArt2DTexture(resourceName);
            if (texture == null)
            {
                texture = LoadArt2DTexture("Art2D/" + resourceName);
            }

            if (texture == null)
            {
                return null;
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            Vector2 pivot = new Vector2(0.5f, 0f);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            float pixelsPerUnit = Mathf.Clamp(Mathf.Max(rect.width, rect.height), 48f, 192f);
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
            sprite.name = resourceName;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static Sprite LoadCoreSprite(CoreElement element, CoreQuality quality)
        {
            string folder = ResolveCoreFolder(element);
            string qualitySuffix = quality == CoreQuality.Legendary
                ? "Legendary"
                : quality == CoreQuality.Rare ? "Rare" : "Common";
            string assetPath = "Cores/" + folder + "/Core_" + folder + "_" + qualitySuffix;
            string cacheKey = "CoreSprite|" + assetPath;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string artPath = "Art2D/" + assetPath;
            Texture2D texture = LoadTextureFromDisk(artPath);
            if (texture == null)
            {
                Texture2D resourceTexture = LoadResourceTexture(artPath);
                if (resourceTexture != null)
                {
                    texture = MakeReadableCopy(resourceTexture);
                }
            }

            if (texture == null)
            {
                return null;
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            float pixelsPerUnit = Mathf.Clamp(Mathf.Max(rect.width, rect.height), 48f, 160f);
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
            sprite.name = assetPath;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static Sprite LoadCoreSprite(int templateId, CoreQuality quality)
        {
            CoreData core = GameDataModel.GetCore(templateId);
            return core != null ? LoadCoreSprite(core.element, quality) : null;
        }

        public static Sprite LoadTreasureSprite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            assetPath = assetPath.Replace("\\", "/").Trim('/');
            string cacheKey = "TreasureSprite|" + assetPath;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string artPath = "Art2D/" + assetPath;
            Texture2D texture = LoadTextureFromDisk(artPath);
            if (texture == null)
            {
                Texture2D resourceTexture = LoadResourceTexture(artPath);
                if (resourceTexture != null)
                {
                    texture = MakeReadableCopy(resourceTexture);
                }
            }

            if (texture == null)
            {
                return null;
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            float pixelsPerUnit = Mathf.Clamp(Mathf.Max(rect.width, rect.height), 48f, 160f);
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
            sprite.name = assetPath;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static Sprite LoadItemSprite(ItemData item)
        {
            return item != null ? LoadItemSprite(GameDataModel.ResolveItemIconKey(item)) : null;
        }

        public static Sprite LoadItemSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            resourcePath = resourcePath.Replace("\\", "/").Trim('/');
            string cacheKey = "ItemSprite|" + resourcePath;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                spriteCache[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite LoadTreasureSprite(TreasureData treasure)
        {
            return treasure != null ? LoadTreasureSprite(treasure.iconKey) : null;
        }

        public static Sprite LoadPlayerPortraitSprite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            assetPath = assetPath.Replace("\\", "/").Trim('/');
            string cacheKey = "PlayerPortrait|" + assetPath;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = LoadArt2DTexture("Art2D/" + assetPath);
            if (texture == null)
            {
                texture = LoadArt2DTexture(assetPath);
            }

            if (texture == null)
            {
                return null;
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            Vector2 pivot = new Vector2(0.5f, 0f);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            float pixelsPerUnit = Mathf.Clamp(Mathf.Max(rect.width, rect.height), 48f, 192f);
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, pixelsPerUnit);
            sprite.name = assetPath;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static string ResolveCoreFolder(CoreElement element)
        {
            switch (element)
            {
                case CoreElement.Fire:
                    return "Fire";
                case CoreElement.Water:
                    return "Water";
                case CoreElement.Thunder:
                    return "Thunder";
                case CoreElement.Metal:
                    return "Metal";
                default:
                    return "Wind";
            }
        }

        private static float ResolveProjectilePixelsPerUnit(Rect rect)
        {
            float height = Mathf.Max(1f, rect.height);
            float width = Mathf.Max(1f, rect.width);
            if (width > height * 2.5f)
            {
                return Mathf.Clamp(height * 3.5f, 24f, 96f);
            }

            return Mathf.Clamp(Mathf.Max(width, height), 32f, 128f);
        }

        public static Sprite LoadSpriteAny(params string[] assetNames)
        {
            if (assetNames == null)
            {
                return GetFallbackSprite();
            }

            for (int i = 0; i < assetNames.Length; i++)
            {
                Sprite sprite = LoadSprite(assetNames[i]);
                if (sprite != fallbackSprite)
                {
                    return sprite;
                }
            }

            return GetFallbackSprite();
        }

        public static Sprite LoadFromDiskTransparent(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return GetFallbackSprite();
            }

            if (spriteCache.TryGetValue(filePath, out Sprite cached))
            {
                return cached;
            }

            if (!File.Exists(filePath))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = Path.GetFileNameWithoutExtension(filePath);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Object.Destroy(texture);
                return GetFallbackSprite();
            }

            StripBorderBackdrop(texture);
            Rect rect = ResolveOpaqueRect(texture);
            float pixelsPerUnit = Mathf.Max(rect.width, rect.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = texture.name;
            spriteCache[filePath] = sprite;
            return sprite;
        }

        public static Sprite[] LoadSequence(string folder, params string[] frameNames)
        {
            if (frameNames == null || frameNames.Length == 0)
            {
                return new[] { GetFallbackSprite() };
            }

            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < frameNames.Length; i++)
            {
                Sprite sprite = LoadSpriteDirect(folder + "/" + frameNames[i]);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites.Count > 0 ? sprites.ToArray() : new[] { GetFallbackSprite() };
        }

        public static void WarmCombatSprites()
        {
            WarmEnemyActor("AnimationSprites/Enemies/Slime", "Slime_Idle_01", "Slime_Idle_02", "Slime_Move_01", "Slime_Move_02", "Slime_Attack_Windup", "Slime_Attack_Lunge", "Slime_Hit_01");
            WarmEnemyActor("AnimationSprites/Enemies/EliteSlime", "EliteSlime_Idle_01", "EliteSlime_Idle_02", "EliteSlime_Move_01", "EliteSlime_Attack_Windup", "EliteSlime_Attack_Lunge", "EliteSlime_Hit_01");
            WarmEnemyActor("AnimationSprites/Enemies/SkeletonArcher", "SkeletonArcher_Idle_01", "SkeletonArcher_Idle_02", "SkeletonArcher_Move_01", "SkeletonArcher_Move_02", "SkeletonArcher_Aim_01", "SkeletonArcher_Aim_02", "SkeletonArcher_Shoot_01", "SkeletonArcher_Recover_01", "SkeletonArcher_Hit_01");
            LoadProjectileSprite("Projectile_SkeletonArrow");
            LoadProjectileSprite("Projectile_Arrow");
            WarmRoomSpritesSync();
        }

        public static IEnumerator WarmCombatSpritesAsync()
        {
            yield return WarmEnemyActorAsync("AnimationSprites/Enemies/Slime", "Slime_Idle_01", "Slime_Idle_02", "Slime_Move_01", "Slime_Move_02", "Slime_Attack_Windup", "Slime_Attack_Lunge", "Slime_Hit_01");
            yield return WarmEnemyActorAsync("AnimationSprites/Enemies/EliteSlime", "EliteSlime_Idle_01", "EliteSlime_Idle_02", "EliteSlime_Move_01", "EliteSlime_Attack_Windup", "EliteSlime_Attack_Lunge", "EliteSlime_Hit_01");
            yield return WarmEnemyActorAsync("AnimationSprites/Enemies/SkeletonArcher", "SkeletonArcher_Idle_01", "SkeletonArcher_Idle_02", "SkeletonArcher_Move_01", "SkeletonArcher_Move_02", "SkeletonArcher_Aim_01", "SkeletonArcher_Aim_02", "SkeletonArcher_Shoot_01", "SkeletonArcher_Recover_01", "SkeletonArcher_Hit_01");
            LoadProjectileSprite("Projectile_SkeletonArrow");
            LoadProjectileSprite("Projectile_Arrow");
            yield return WarmRoomSpritesAsync();
        }

        private static void WarmRoomSpritesSync()
        {
            string[] roomSprites =
            {
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/crypt_10",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/cobble_blood_1_new",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_dark_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_gray_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/torch_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/large_box",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/chest_2_closed"
            };

            for (int i = 0; i < roomSprites.Length; i++)
            {
                string fileName = Path.GetFileName(roomSprites[i]);
                LoadDungeonRoomSprite(fileName);
            }
        }

        private static IEnumerator WarmRoomSpritesAsync()
        {
            string[] roomSprites =
            {
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/crypt_10",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/cobble_blood_1_new",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_dark_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_gray_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/torch_0",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/large_box",
                "ArtIntegration/Environment/DungeonCrawlCombatRoom/chest_2_closed"
            };

            for (int i = 0; i < roomSprites.Length; i++)
            {
                string fileName = Path.GetFileName(roomSprites[i]);
                LoadDungeonRoomSprite(fileName);
                yield return null;
            }
        }

        private static void WarmEnemyActor(string folder, params string[] frameNames)
        {
            for (int i = 0; i < frameNames.Length; i++)
            {
                LoadSpriteDirect(folder + "/" + frameNames[i]);
            }
        }

        private static IEnumerator WarmEnemyActorAsync(string folder, params string[] frameNames)
        {
            for (int i = 0; i < frameNames.Length; i++)
            {
                LoadSpriteDirect(folder + "/" + frameNames[i]);
                yield return null;
            }
        }

        private static bool NeedsBackdropStrip(string assetName)
        {
            if (assetName.Contains("Environment/Tiles/")
                || assetName.Contains("ArtIntegration/Environment/DungeonCrawlCombatRoom/"))
            {
                return false;
            }

            return assetName.Contains("Enemies/") || assetName.Contains("Bosses/")
                || assetName.StartsWith("Enemy_") || assetName.StartsWith("Boss_")
                || assetName.Contains("Environment/Interactables/")
                || assetName.Contains("Environment/RoomDecor/")
                || assetName.Contains("Environment/GroundEffects/")
                || assetName.Contains("Environment/Lobby/")
                || assetName.Contains("ArtIntegration/UI/");
        }

        private static Sprite LoadSpriteDirect(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            assetName = assetName.Replace("\\", "/").Trim('/');
            if (assetName.StartsWith("Projectile_"))
            {
                return LoadProjectileSprite(assetName);
            }

            if (assetName.StartsWith("VFX_Boss_") || assetName == "EnhancedBossBullet")
            {
                return LoadBossVfxSprite(assetName);
            }

            if (spriteCache.TryGetValue(assetName, out Sprite cached))
            {
                return cached;
            }

            bool needsBackdropStrip = NeedsBackdropStrip(assetName);

            if (!needsBackdropStrip)
            {
                Sprite direct = LoadResourceSprite(assetName);
                if (direct != null)
                {
                    spriteCache[assetName] = direct;
                    return direct;
                }
            }

            Texture2D texture = LoadTextureFromDisk(assetName);
            if (texture == null)
            {
                Texture2D resourceTexture = LoadResourceTexture(assetName);
                if (resourceTexture != null)
                {
                    texture = MakeReadableCopy(resourceTexture);
                }
            }

            if (texture != null)
            {
                if (needsBackdropStrip)
                {
                    StripBorderBackdrop(texture);
                }

                Rect rect = ResolveOpaqueRect(texture);
                float pixelsPerUnit = Mathf.Max(rect.width, rect.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Sprite sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
                sprite.name = assetName;
                spriteCache[assetName] = sprite;
                return sprite;
            }

            if (!needsBackdropStrip)
            {
                Sprite imported = LoadResourceSprite(assetName);
                if (imported != null)
                {
                    spriteCache[assetName] = imported;
                    return imported;
                }
            }

            return null;
        }

        private static string[] BuildResourcePathCandidates(string assetName)
        {
            assetName = assetName.Replace("\\", "/").Trim('/');
            if (assetName.StartsWith("Art2D/"))
            {
                return new[] { assetName };
            }

            return new[] { "Art2D/" + assetName, assetName };
        }

        private static Sprite LoadResourceSprite(string assetName)
        {
            string[] candidates = BuildResourcePathCandidates(assetName);
            for (int i = 0; i < candidates.Length; i++)
            {
                Sprite sprite = Resources.Load<Sprite>(candidates[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Texture2D LoadArt2DTexture(string artPath)
        {
            Texture2D texture = LoadTextureFromDisk(artPath);
            if (texture == null)
            {
                Texture2D resourceTexture = LoadResourceTexture(artPath);
                if (resourceTexture != null)
                {
                    texture = MakeReadableCopy(resourceTexture);
                }
            }

            return texture;
        }

        private static Texture2D LoadResourceTexture(string assetName)
        {
            string[] candidates = BuildResourcePathCandidates(assetName);
            for (int i = 0; i < candidates.Length; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>(candidates[i]);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        private static Texture2D LoadTextureFromDisk(string assetName)
        {
            string[] candidates = BuildResourcePathCandidates(assetName);
            for (int i = 0; i < candidates.Length; i++)
            {
                Texture2D texture = LoadTextureFromDiskPath(candidates[i]);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        private static Texture2D LoadTextureFromDiskPath(string resourcePath)
        {
            string filePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".png");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".PNG");
                if (!File.Exists(filePath))
                {
                    return null;
                }
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = resourcePath + "_FromDisk";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        private static Rect ResolveOpaqueRect(Texture2D texture)
        {
            if (texture == null)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            Color32[] pixels;
            try
            {
                pixels = texture.GetPixels32();
            }
            catch (UnityException)
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }
            catch (System.Exception)
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }

            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;
            const byte alphaThreshold = 12;
            for (int y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[row + x].a <= alphaThreshold)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }

            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public static float ComputeSpriteFeetOffsetY(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return 0f;
            }

            Rect textureRect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            int minOpaqueY = int.MaxValue;
            try
            {
                Color32[] pixels = texture.GetPixels32();
                int textureWidth = texture.width;
                int startX = Mathf.FloorToInt(textureRect.x);
                int startY = Mathf.FloorToInt(textureRect.y);
                int endX = startX + Mathf.FloorToInt(textureRect.width);
                int endY = startY + Mathf.FloorToInt(textureRect.height);
                const byte alphaThreshold = 12;
                for (int y = startY; y < endY; y++)
                {
                    int row = y * textureWidth;
                    for (int x = startX; x < endX; x++)
                    {
                        if (pixels[row + x].a <= alphaThreshold)
                        {
                            continue;
                        }

                        if (y < minOpaqueY)
                        {
                            minOpaqueY = y;
                        }
                    }
                }
            }
            catch (UnityException)
            {
                return 0f;
            }
            catch (System.Exception)
            {
                return 0f;
            }

            if (minOpaqueY == int.MaxValue)
            {
                return 0f;
            }

            float opaqueBottomInRect = minOpaqueY - textureRect.y;
            float feetLocalY = (opaqueBottomInRect - sprite.pivot.y) / sprite.pixelsPerUnit;
            return -feetLocalY;
        }

        public static SpriteRenderer CreateSpriteObject(Transform parent, string name, Vector2 position, Vector2 size, string spriteName, Color color, int sortingOrder = 0)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spriteName);
            if (renderer.sprite == null)
            {
                renderer.sprite = GetFallbackSprite();
            }
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static bool IsFallbackSprite(Sprite sprite)
        {
            return sprite != null && sprite == GetFallbackSprite();
        }

        public static Sprite LoadDungeonRoomSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return GetFallbackSprite();
            }

            string fullPath = "ArtIntegration/Environment/DungeonCrawlCombatRoom/" + assetName.Replace("\\", "/").Trim('/');
            if (spriteCache.TryGetValue(fullPath, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = LoadResourceSprite(fullPath);
            if (sprite != null)
            {
                spriteCache[fullPath] = sprite;
                return sprite;
            }

            return GetFallbackSprite();
        }

        public static Sprite LoadLobbySprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            assetName = assetName.Replace("\\", "/").Trim('/');
            string[] candidates =
            {
                "Art2D/ArtIntegration/Environment/Lobby/" + assetName,
                "Art2D/Environment/Lobby/" + assetName,
                "ArtIntegration/Environment/Lobby/" + assetName,
                "Environment/Lobby/" + assetName
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Sprite sprite = LoadSpriteDirect(candidates[i]);
                if (sprite != null && !IsFallbackSprite(sprite))
                {
                    return sprite;
                }
            }

            return null;
        }

        public static SpriteRenderer CreateLobbySpriteObject(Transform parent, string name, Vector2 position, Vector2 size, string spriteName, Color color, int sortingOrder = 0)
        {
            Sprite sprite = LoadLobbySprite(spriteName);
            if (sprite == null)
            {
                return null;
            }

            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            Vector2 spriteSize = sprite.bounds.size;
            if (spriteSize.x > 0.001f && spriteSize.y > 0.001f)
            {
                obj.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            }
            else
            {
                obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static void CleanupTransientCombatVfx()
        {
            DestroyRootObjectsNamed(
                "SlimeAttackTell",
                "SkeletonAimTell",
                "BowChargeVfx",
                "BowChargePips",
                "BowChargeOverlay",
                "BossWarningLine",
                "BossDelayedCircle",
                "VerticalLightningTell",
                "HorizontalLightningTell",
                "ClassMotionLine",
                "ClassSpark",
                "WaterPuddle",
                "FallingGoldStrike");
        }

        public static void CleanupLegacyShopPlaceholderSprites()
        {
            DestroyObjectsNamed(
                "CounterBody",
                "CounterTop",
                "CounterCloth",
                "CounterShadow",
                "MerchantBody",
                "MerchantHood",
                "MerchantHead",
                "ShopCounterTop",
                "ShopCloth",
                "ShopCoinA",
                "ShopCoinB",
                "ShopBottle",
                "CoinA",
                "CoinB",
                "LampGlow",
                "LampCore");
        }

        private static void DestroyObjectsNamed(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return;
            }

            HashSet<string> targets = new HashSet<string>(names);
            GameObject[] matches = Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < matches.Length; i++)
            {
                GameObject candidate = matches[i];
                if (candidate != null && targets.Contains(candidate.name))
                {
                    Object.Destroy(candidate);
                }
            }
        }

        private static void DestroyRootObjectsNamed(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                GameObject[] matches = GameObject.FindObjectsOfType<GameObject>();
                for (int j = 0; j < matches.Length; j++)
                {
                    GameObject candidate = matches[j];
                    if (candidate != null && candidate.name == names[i] && candidate.transform.parent == null)
                    {
                        Object.Destroy(candidate);
                    }
                }
            }
        }

        public static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Generated2DDefaultSpriteTexture";
            texture.SetPixel(0, 0, new Color(1f, 0f, 1f, 0.35f));
            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            fallbackSprite.name = "Generated2DDefaultSprite";
            return fallbackSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Generated2DCircleSpriteTexture";
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            circleSprite.name = "Generated2DCircleSprite";
            return circleSprite;
        }

        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            if (source == null)
            {
                return source;
            }

            try
            {
                source.GetPixels32();
                return source;
            }
            catch
            {
            }

            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.name = source.name + "_Readable";
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static void StripBorderBackdrop(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            try
            {
                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();

                Color32 bgColor = SampleBackgroundColor(pixels, width, height);
                const int tolerance = 62;

                bool[] visited = new bool[pixels.Length];
                Queue<int> queue = new Queue<int>();

                for (int x = 0; x < width; x++)
                {
                    TryEnqueueIfBackground(pixels, visited, queue, x, 0, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x, height - 1, width, height, bgColor, tolerance);
                }

                for (int y = 0; y < height; y++)
                {
                    TryEnqueueIfBackground(pixels, visited, queue, 0, y, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, width - 1, y, width, height, bgColor, tolerance);
                }

                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    pixels[index] = new Color32(0, 0, 0, 0);
                    int x = index % width;
                    int y = index / width;
                    TryEnqueueIfBackground(pixels, visited, queue, x - 1, y, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x + 1, y, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x, y - 1, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x, y + 1, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x - 1, y - 1, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x + 1, y - 1, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x - 1, y + 1, width, height, bgColor, tolerance);
                    TryEnqueueIfBackground(pixels, visited, queue, x + 1, y + 1, width, height, bgColor, tolerance);
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
            }
            catch
            {
            }
        }

        private static Color32 SampleBackgroundColor(Color32[] pixels, int width, int height)
        {
            int rSum = 0, gSum = 0, bSum = 0;
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                AddSample(pixels[x], ref rSum, ref gSum, ref bSum, ref count);
                AddSample(pixels[(height - 1) * width + x], ref rSum, ref gSum, ref bSum, ref count);
            }

            for (int y = 1; y < height - 1; y++)
            {
                AddSample(pixels[y * width], ref rSum, ref gSum, ref bSum, ref count);
                AddSample(pixels[y * width + width - 1], ref rSum, ref gSum, ref bSum, ref count);
            }

            if (count == 0)
            {
                return new Color32(255, 255, 255, 255);
            }

            return new Color32((byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count), 255);
        }

        private static void AddSample(Color32 pixel, ref int rSum, ref int gSum, ref int bSum, ref int count)
        {
            if (pixel.a < 10)
            {
                return;
            }

            rSum += pixel.r;
            gSum += pixel.g;
            bSum += pixel.b;
            count++;
        }

        private static void TryEnqueueIfBackground(Color32[] pixels, bool[] visited, Queue<int> queue, int x, int y, int width, int height, Color32 bgColor, int tolerance)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            int index = y * width + x;
            if (visited[index])
            {
                return;
            }

            Color32 pixel = pixels[index];
            if (pixel.a < 30)
            {
                visited[index] = true;
                queue.Enqueue(index);
                return;
            }

            int dr = pixel.r - bgColor.r;
            int dg = pixel.g - bgColor.g;
            int db = pixel.b - bgColor.b;
            int distSq = dr * dr + dg * dg + db * db;
            if (distSq > tolerance * tolerance)
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(index);
        }
    }
}
