using UnityEngine;

namespace Bagsys.RogueLike
{
    public static class RuntimeVisualUtility
    {
        public static UnityEngine.Material CreateMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.35f)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            UnityEngine.Material material = new UnityEngine.Material(shader);
            material.name = name;
            material.color = color;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            return material;
        }

        public static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, UnityEngine.Material material, bool removeCollider = true)
        {
            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            Collider collider = child.GetComponent<Collider>();
            if (collider != null && removeCollider)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return child;
        }

        public static void SetRenderersEnabled(GameObject target, bool enabled)
        {
            if (target == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponents<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = enabled;
            }
        }
    }
}
