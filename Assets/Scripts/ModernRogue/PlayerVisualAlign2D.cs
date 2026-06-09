using UnityEngine;

namespace ModernRogue
{
    internal static class PlayerVisualAlign2D
    {
        public static void AlignVisualRootFeet(Transform playerRoot, Transform visualRoot, float zOffset)
        {
            if (playerRoot == null || visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = new Vector3(0f, 0f, zOffset);
            if (!TryFindFeetRenderer(playerRoot, visualRoot, out SpriteRenderer feetRenderer))
            {
                return;
            }

            float correctionY = playerRoot.position.y - feetRenderer.bounds.min.y;
            visualRoot.localPosition = new Vector3(0f, correctionY, zOffset);
        }

        public static bool TryFindFeetRenderer(Transform playerRoot, Transform visualRoot, out SpriteRenderer result)
        {
            result = null;
            if (visualRoot != null && TryFindBestRendererUnder(visualRoot, out result))
            {
                return true;
            }

            return playerRoot != null && PlayerAim2D.TryGetPrimaryVisualRenderer(playerRoot, out result);
        }

        private static bool TryFindBestRendererUnder(Transform root, out SpriteRenderer result)
        {
            result = null;
            if (root == null)
            {
                return false;
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            int bestOrder = int.MinValue;
            float bestArea = -1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate == null
                    || candidate.sprite == null
                    || !candidate.enabled
                    || candidate.color.a < 0.05f)
                {
                    continue;
                }

                string lowerName = candidate.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("shadow") || lowerName.Contains("charge") || lowerName.Contains("overlay")
                    || lowerName.Contains("frame") || lowerName.Contains("aura") || lowerName.Contains("shield")
                    || lowerName.Contains("bubble") || lowerName.Contains("vfx") || lowerName.Contains("health")
                    || lowerName.Contains("muzzle") || lowerName.Contains("trail") || lowerName.Contains("weapon")
                    || lowerName.Contains("effect"))
                {
                    continue;
                }

                Bounds bounds = candidate.bounds;
                float area = bounds.size.x * bounds.size.y;
                if (candidate.sortingOrder > bestOrder || (candidate.sortingOrder == bestOrder && area > bestArea))
                {
                    result = candidate;
                    bestOrder = candidate.sortingOrder;
                    bestArea = area;
                }
            }

            return result != null;
        }
    }
}
