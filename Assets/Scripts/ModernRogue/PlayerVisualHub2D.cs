using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public class PlayerVisualHub2D : MonoBehaviour
    {
        public Vector2 BodyCenter { get; private set; }
        public Vector2 FeetWorld { get; private set; }
        public Transform CharacterSpriteTransform { get; private set; }

        public Vector2 ResolveOrbitCenter()
        {
            return BodyCenter;
        }

        public Vector2 ResolveFeetWorld()
        {
            return FeetWorld;
        }

        public static PlayerVisualHub2D Get(Transform playerTransform)
        {
            return playerTransform != null ? playerTransform.GetComponent<PlayerVisualHub2D>() : null;
        }

        private void LateUpdate()
        {
            HeroClass heroClass = RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.CurrentClass : HeroClass.Warrior;
            TinySwordsPlayerVisual2D warriorVisual = GetComponent<TinySwordsPlayerVisual2D>();
            if (heroClass == HeroClass.Warrior
                && warriorVisual != null
                && warriorVisual.TryGetVisualAnchors(out Transform feet, out Transform center))
            {
                if (center != null)
                {
                    BodyCenter = center.position;
                    CharacterSpriteTransform = center;
                }
                else if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer bodyRenderer))
                {
                    BodyCenter = bodyRenderer.bounds.center;
                    CharacterSpriteTransform = bodyRenderer.transform;
                }
                else
                {
                    BodyCenter = transform.position;
                    CharacterSpriteTransform = transform;
                }

                FeetWorld = feet != null ? (Vector2)feet.position : new Vector2(BodyCenter.x, BodyCenter.y);
                AlignHitboxCollider();
                return;
            }

            if (PlayerAim2D.TryGetPrimaryVisualRenderer(transform, out SpriteRenderer renderer))
            {
                Bounds bounds = renderer.bounds;
                BodyCenter = bounds.center;
                FeetWorld = new Vector2(bounds.center.x, bounds.min.y);
                CharacterSpriteTransform = renderer.transform;
                AlignHitboxCollider();
                return;
            }

            BodyCenter = transform.position;
            FeetWorld = transform.position;
            CharacterSpriteTransform = transform;
            AlignHitboxCollider();
        }

        private void AlignHitboxCollider()
        {
            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                return;
            }

            float offsetY = BodyCenter.y - transform.position.y;
            if (offsetY < 0.08f)
            {
                circle.offset = Vector2.zero;
                circle.radius = 0.45f;
                return;
            }

            float bodyHeight = Mathf.Max(0.35f, BodyCenter.y - FeetWorld.y);
            circle.offset = new Vector2(0f, offsetY * 0.92f);
            circle.radius = Mathf.Clamp(bodyHeight * 0.34f, 0.32f, 0.5f);
        }
    }
}
