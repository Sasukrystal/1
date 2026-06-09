using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    internal static class RuntimeUIVisuals
    {
        private static Sprite solidSprite;

        public static Sprite SolidSprite
        {
            get
            {
                if (solidSprite != null)
                {
                    return solidSprite;
                }

                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);
                solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                solidSprite.hideFlags = HideFlags.HideAndDontSave;
                return solidSprite;
            }
        }

        public static void StyleImage(Image image, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = SolidSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
        }

        public static void StyleChrome(Image image, Color color)
        {
            StyleImage(image, color);
        }

        public static void StyleAccent(Image image, Color color)
        {
            StyleImage(image, color);
        }

        public static void CreateBlock(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            StyleImage(obj.GetComponent<Image>(), color);
        }

        public static void AddFrame(Transform parent, string name, Color color, float thickness)
        {
            CreateBlock(parent, name + "_Top", new Vector2(0f, 1f - thickness), Vector2.one, color);
            CreateBlock(parent, name + "_Bottom", Vector2.zero, new Vector2(1f, thickness), color);
            CreateBlock(parent, name + "_Left", Vector2.zero, new Vector2(thickness, 1f), color);
            CreateBlock(parent, name + "_Right", new Vector2(1f - thickness, 0f), Vector2.one, color);
        }
    }
}
