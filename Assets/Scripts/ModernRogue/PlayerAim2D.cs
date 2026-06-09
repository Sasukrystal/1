using UnityEngine;

namespace ModernRogue
{
    internal static class PlayerAim2D
    {
        public static Vector2 ResolveMouseWorld(Transform player)
        {
            Camera camera = Camera.main;
            if (camera == null || player == null)
            {
                return player != null ? (Vector2)player.position + Vector2.right : Vector2.right;
            }

            Vector3 screen = Input.mousePosition;
            screen.z = player.position.z - camera.transform.position.z;
            return camera.ScreenToWorldPoint(screen);
        }

        public static Vector2 ResolveVisualAimOrigin(Transform player)
        {
            if (player == null)
            {
                return Vector2.zero;
            }

            if (TryGetPrimaryVisualRenderer(player, out SpriteRenderer renderer))
            {
                return renderer.bounds.center;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            return body != null ? body.position : (Vector2)player.position;
        }

        public static Vector2 ResolveAimDirection(Transform player)
        {
            Vector2 origin = ResolveVisualAimOrigin(player);
            Vector2 mouseWorld = ResolveMouseWorld(player);
            Vector2 direction = mouseWorld - origin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector2.right;
            }

            return direction.normalized;
        }

        public static bool TryGetPrimaryVisualRenderer(Transform player, out SpriteRenderer result)
        {
            result = null;
            if (player == null)
            {
                return false;
            }

            SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            int bestOrder = int.MinValue;
            float bestArea = -1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate == null
                    || candidate.sprite == null
                    || !candidate.enabled
                    || candidate.color.a < 0.05f
                    || candidate.sortingOrder < 7)
                {
                    continue;
                }

                string lowerName = candidate.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("shadow") || lowerName.Contains("charge") || lowerName.Contains("overlay")
                    || lowerName.Contains("frame") || lowerName.Contains("aura") || lowerName.Contains("shield")
                    || lowerName.Contains("bubble") || lowerName.Contains("vfx") || lowerName.Contains("health")
                    || lowerName.Contains("muzzle") || lowerName.Contains("trail") || lowerName.Contains("bowcharge")
                    || IsUnderNamedRoot(candidate.transform, "ArcherBowChargeBar"))
                {
                    continue;
                }

                HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
                if (heroClass != HeroClass.Warrior && IsUnderNamedRoot(candidate.transform, "TinySwordsWarriorRuntimeVisual"))
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

        private static bool IsUnderNamedRoot(Transform node, string rootName)
        {
            Transform walk = node;
            while (walk != null)
            {
                if (walk.name == rootName)
                {
                    return true;
                }

                walk = walk.parent;
            }

            return false;
        }
    }
}
