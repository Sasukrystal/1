using UnityEngine;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    public class RuntimeArtBinder : MonoBehaviour
    {
        [SerializeField] private string artName;
        [SerializeField] private Color fallbackTint = Color.white;
        [SerializeField] private bool billboardToCamera;

        private Renderer[] cachedRenderers;

        public static T LoadAssetFromResources<T>(string assetName) where T : Object
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            string normalized = assetName.StartsWith("Art/") ? assetName : "Art/" + assetName;
            normalized = normalized.Replace(".png", string.Empty).Replace(".PNG", string.Empty);
            return Resources.Load<T>(normalized);
        }

        public void SetSprite(string path)
        {
            artName = path;
            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (!billboardToCamera || Camera.main == null)
            {
                return;
            }

            transform.forward = Camera.main.transform.forward;
        }

        public void Apply()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            Texture2D texture = LoadAssetFromResources<Texture2D>(artName);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                UnityEngine.Material material = renderer.material;
                if (texture != null)
                {
                    material.mainTexture = texture;
                    material.color = Color.white;
                }
                else
                {
                    material.color = fallbackTint;
                }
            }
        }
    }
}
