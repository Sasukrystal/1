using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModernRogue
{
    public static class TinySwordsExternalSpriteLibrary
    {
        private const int ArcherFrameHeight = 192;
        private const float ArcherPivotY = 0.08f;
        private const string ChargedArrowGeneratedFileName = "Projectile_ChargedArrow_Generated.png";
        private const string ArcherDodgeGeneratedFileName = "Archer_Dodge_Generated.png";

        private static readonly Dictionary<string, Sprite[]> clipCache = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static void ResetArcherClipCache()
        {
            clipCache.Clear();
            spriteCache.Clear();
        }

        public static void WarmArcherClipsAtStartup()
        {
            TryGetArcherClip("Idle", out _);
            TryGetArcherClip("Run", out _);
            TryGetArcherClip("Attack", out _);
        }

        public static bool TryGetArcherClip(string clipName, out Sprite[] frames)
        {
            frames = null;
            if (string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            if (clipCache.TryGetValue(clipName, out frames))
            {
                return frames != null && frames.Length > 0;
            }

            switch (clipName)
            {
                case "Idle":
                    frames = LoadSheetFrames("Archer_Idle", "Archer_Idle.png", 6);
                    break;
                case "Run":
                    frames = LoadSheetFrames("Archer_Run", "Archer_Run.png", 4);
                    break;
                case "Attack":
                    frames = LoadSheetFrames("Archer_Shoot", "Archer_Shoot.png", 8);
                    break;
                case "Special":
                    if (TryGetArcherClip("Attack", out Sprite[] attackFrames))
                    {
                        frames = SliceRange(attackFrames, 0, 4);
                    }

                    break;
                case "Dodge":
                    if (TryGetArcherClip("Run", out Sprite[] runFrames))
                    {
                        frames = Reverse(runFrames);
                    }

                    break;
                case "Hit":
                    if (TryGetArcherClip("Idle", out Sprite[] idleFrames))
                    {
                        frames = new[] { idleFrames[idleFrames.Length - 1] };
                    }

                    break;
            }

            if (frames == null || frames.Length == 0)
            {
                return false;
            }

            clipCache[clipName] = frames;
            return true;
        }

        public static bool HasTinySwordsArcherSheets()
        {
            return TryGetArcherClip("Idle", out Sprite[] idle) && idle != null && idle.Length > 0
                && TryGetArcherClip("Run", out Sprite[] run) && run != null && run.Length > 0
                && TryGetArcherClip("Attack", out Sprite[] attack) && attack != null && attack.Length > 0;
        }

        public static bool TryGetArcherProjectileSprite(bool charged, out Sprite sprite)
        {
            string key = charged ? "ArcherProjectileCharged" : "ArcherProjectile";
            if (spriteCache.TryGetValue(key, out sprite))
            {
                return sprite != null;
            }

            string primaryPath = charged
                ? GetProjectProcessedPath(ChargedArrowGeneratedFileName)
                : GetProjectProcessedPath("Projectile_Arrow_TransparentTest.png");
            string fallbackPath = GetExternalArcherPath("Arrow.png");
            string resolvedPath = File.Exists(primaryPath)
                ? primaryPath
                : File.Exists(fallbackPath)
                    ? fallbackPath
                    : "Resources://Art2D/External/TinySwords/Archer/Arrow";
            sprite = LoadSingleSprite(
                key,
                resolvedPath,
                charged ? new Vector2(0.12f, 0.5f) : new Vector2(0.2f, 0.5f),
                stripBackdrop: charged);
            spriteCache[key] = sprite;
            return sprite != null;
        }

        public static bool TryGetArcherDodgeEffectSprite(out Sprite sprite)
        {
            const string key = "ArcherDodgeUserEffect";
            if (spriteCache.TryGetValue(key, out sprite))
            {
                return sprite != null;
            }

            string path = GetProjectProcessedPath(ArcherDodgeGeneratedFileName);
            string resolvedPath = File.Exists(path)
                ? path
                : "Resources://Art2D/External/TinySwords/Archer/Archer_Run";
            sprite = LoadSingleSprite(key, resolvedPath, new Vector2(0.76f, 0.5f), stripBackdrop: true);
            spriteCache[key] = sprite;
            return sprite != null;
        }

        private static string GetExternalArcherPath(string fileName)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Tiny Swords", "Tiny Swords", "Tiny Swords02", "Units", "Blue Units", "Archer", fileName);
        }

        private static string GetBlueUnitArcherPath(string fileName)
        {
            return GetExternalArcherPath(fileName);
        }

        private static string GetProjectProcessedPath(string fileName)
        {
            return Path.Combine(Application.dataPath, "_Project", "ArtProcessed", "TransparentTest", fileName);
        }

        private static bool TryLoadTextureFromResolvedPath(string resolvedPath, string textureName, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return false;
            }

            if (resolvedPath.StartsWith("Resources://"))
            {
                string resourcePath = resolvedPath.Substring("Resources://".Length);
                Texture2D source = Resources.Load<Texture2D>(resourcePath);
                if (source == null)
                {
                    Debug.LogWarning("[TinySwords] Resources.Load<Texture2D> failed: " + resourcePath);
                    return false;
                }

                texture = MakeReadableCopy(source, textureName);
                return texture != null;
            }

            if (resolvedPath.StartsWith("Streaming://"))
            {
                string relativePath = resolvedPath.Substring("Streaming://".Length);
                string streamingPath = Path.Combine(Application.streamingAssetsPath, relativePath);
                return TryLoadTextureFromBytes(streamingPath, textureName, out texture);
            }

            return TryLoadTextureFromBytes(resolvedPath, textureName, out texture);
        }

        private static bool TryLoadTextureFromBytes(string filePath, string textureName, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Object.Destroy(texture);
                texture = null;
                return false;
            }

            return true;
        }

        private static Texture2D MakeReadableCopy(Texture2D source, string textureName)
        {
            if (source == null)
            {
                return null;
            }

            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.name = textureName;
            copy.filterMode = FilterMode.Point;
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static string ResolveReadableAssetPath(string bundledResourceName, string externalPath)
        {
            if (!string.IsNullOrEmpty(externalPath) && File.Exists(externalPath))
            {
                return externalPath;
            }

            return "Resources://Art2D/External/TinySwords/Archer/" + Path.GetFileNameWithoutExtension(bundledResourceName);
        }

        private static string ResolveArcherSheetPath(string fileName)
        {
            string externalPath = GetExternalArcherPath(fileName);
#if UNITY_EDITOR
            if (File.Exists(externalPath))
            {
                return externalPath;
            }
#endif

            string streamingRelative = Path.Combine("Art2D", "External", "TinySwords", "Archer", fileName);
            string streamingPath = Path.Combine(Application.streamingAssetsPath, streamingRelative);
            if (File.Exists(streamingPath))
            {
                return "Streaming://" + streamingRelative.Replace('\\', '/');
            }

            return "Resources://Art2D/External/TinySwords/Archer/" + Path.GetFileNameWithoutExtension(fileName);
        }

        private static Sprite[] LoadSheetFrames(string cacheKey, string fileName, int frameCount)
        {
            if (clipCache.TryGetValue(cacheKey, out Sprite[] cached))
            {
                return cached;
            }

            if (!TryLoadTextureFromResolvedPath(ResolveArcherSheetPath(fileName), cacheKey + "_Texture", out Texture2D texture))
            {
                Debug.LogWarning("[TinySwords] Failed to load archer sheet: " + fileName);
                return null;
            }

            if (frameCount <= 0 || texture.width % frameCount != 0)
            {
                Object.Destroy(texture);
                return null;
            }

            int frameWidth = texture.width / frameCount;
            int frameHeight = texture.height;
            List<Sprite> sprites = new List<Sprite>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                Rect rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight);
                Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, ArcherPivotY), frameWidth, 0, SpriteMeshType.FullRect);
                sprite.name = cacheKey + "_" + (i + 1).ToString("00");
                sprites.Add(sprite);
            }

            Sprite[] result = sprites.ToArray();
            clipCache[cacheKey] = result;
            return result;
        }

        private static Sprite[] SliceRange(Sprite[] source, int start, int length)
        {
            if (source == null || source.Length == 0 || length <= 0)
            {
                return null;
            }

            int clampedStart = Mathf.Clamp(start, 0, source.Length - 1);
            int clampedLength = Mathf.Clamp(length, 1, source.Length - clampedStart);
            Sprite[] result = new Sprite[clampedLength];
            for (int i = 0; i < clampedLength; i++)
            {
                result[i] = source[clampedStart + i];
            }

            return result;
        }

        private static Sprite[] Reverse(Sprite[] source)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }

            Sprite[] result = new Sprite[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[source.Length - 1 - i];
            }

            return result;
        }

        private static Sprite LoadSingleSprite(string cacheKey, string resolvedPath, Vector2 pivot, bool stripBackdrop = false)
        {
            if (!TryLoadTextureFromResolvedPath(resolvedPath, cacheKey + "_Texture", out Texture2D texture))
            {
                return null;
            }

            if (stripBackdrop)
            {
                StripBorderBackdrop(texture);
            }

            Rect rect = ResolveOpaqueRect(texture);
            Vector2 clampedPivot = new Vector2(
                Mathf.Clamp01((pivot.x * texture.width - rect.xMin) / Mathf.Max(1f, rect.width)),
                Mathf.Clamp01((pivot.y * texture.height - rect.yMin) / Mathf.Max(1f, rect.height)));
            Sprite sprite = Sprite.Create(texture, rect, clampedPivot, Mathf.Max(rect.width, rect.height), 0, SpriteMeshType.FullRect);
            sprite.name = cacheKey;
            return sprite;
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

                Color32 bgColor = SampleEdgeColor(pixels, width, height);
                const int tolerance = 62;
                int tolSq = tolerance * tolerance;

                bool[] visited = new bool[pixels.Length];
                Queue<int> queue = new Queue<int>();

                for (int x = 0; x < width; x++)
                {
                    TryEnqueueBg(pixels, visited, queue, x, 0, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x, height - 1, width, height, bgColor, tolSq);
                }

                for (int y = 0; y < height; y++)
                {
                    TryEnqueueBg(pixels, visited, queue, 0, y, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, width - 1, y, width, height, bgColor, tolSq);
                }

                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    pixels[index] = new Color32(0, 0, 0, 0);
                    int x = index % width;
                    int y = index / width;
                    TryEnqueueBg(pixels, visited, queue, x - 1, y, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x + 1, y, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x, y - 1, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x, y + 1, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x - 1, y - 1, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x + 1, y - 1, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x - 1, y + 1, width, height, bgColor, tolSq);
                    TryEnqueueBg(pixels, visited, queue, x + 1, y + 1, width, height, bgColor, tolSq);
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
            }
            catch
            {
            }
        }

        private static Color32 SampleEdgeColor(Color32[] pixels, int width, int height)
        {
            int rSum = 0, gSum = 0, bSum = 0, count = 0;
            for (int x = 0; x < width; x++)
            {
                AddEdgeSample(pixels[x], ref rSum, ref gSum, ref bSum, ref count);
                AddEdgeSample(pixels[(height - 1) * width + x], ref rSum, ref gSum, ref bSum, ref count);
            }

            for (int y = 1; y < height - 1; y++)
            {
                AddEdgeSample(pixels[y * width], ref rSum, ref gSum, ref bSum, ref count);
                AddEdgeSample(pixels[y * width + width - 1], ref rSum, ref gSum, ref bSum, ref count);
            }

            if (count == 0)
            {
                return new Color32(255, 255, 255, 255);
            }

            return new Color32((byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count), 255);
        }

        private static void AddEdgeSample(Color32 pixel, ref int rSum, ref int gSum, ref int bSum, ref int count)
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

        private static void TryEnqueueBg(Color32[] pixels, bool[] visited, Queue<int> queue, int x, int y, int width, int height, Color32 bgColor, int tolSq)
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
            if (dr * dr + dg * dg + db * db > tolSq)
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(index);
        }

        private static Rect ResolveOpaqueRect(Texture2D texture)
        {
            if (texture == null)
            {
                return new Rect(0f, 0f, 2f, 2f);
            }

            try
            {
                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;

                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a < 12)
                    {
                        continue;
                    }

                    int x = i % width;
                    int y = i / width;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }

                if (maxX < minX || maxY < minY)
                {
                    return new Rect(0f, 0f, width, height);
                }

                return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            catch
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }
        }
    }
}
