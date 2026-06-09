using UnityEngine;

namespace Bagsys.RogueLike
{
    public enum RuntimeCharacterVisualStyle
    {
        Player,
        Enemy
    }

    [DisallowMultipleComponent]
    public class RuntimeCharacterVisual : MonoBehaviour
    {
        [SerializeField] private RuntimeCharacterVisualStyle style = RuntimeCharacterVisualStyle.Player;
        [SerializeField] private bool rebuildOnStart;

        private const string VisualRootName = "RuntimeStylizedVisual";

        public void Configure(RuntimeCharacterVisualStyle newStyle)
        {
            style = newStyle;
            RebuildVisual();
        }

        private void Awake()
        {
            RebuildVisual();
        }

        private void Start()
        {
            if (rebuildOnStart)
            {
                RebuildVisual();
            }
        }

        [ContextMenu("Rebuild Visual")]
        public void RebuildVisual()
        {
            Transform oldVisual = transform.Find(VisualRootName);
            if (oldVisual != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldVisual.gameObject);
                }
                else
                {
                    DestroyImmediate(oldVisual.gameObject);
                }
            }

            RuntimeVisualUtility.SetRenderersEnabled(gameObject, false);

            GameObject root = new GameObject(VisualRootName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            if (style == RuntimeCharacterVisualStyle.Player)
            {
                BuildPlayer(root.transform);
            }
            else
            {
                BuildEnemy(root.transform);
            }
        }

        private static void BuildPlayer(Transform root)
        {
            UnityEngine.Material cloak = RuntimeVisualUtility.CreateMaterial("Player_Cobalt_Cloak", new Color(0.12f, 0.35f, 0.78f, 1f), 0f, 0.42f);
            UnityEngine.Material armor = RuntimeVisualUtility.CreateMaterial("Player_Steel_Armor", new Color(0.66f, 0.76f, 0.86f, 1f), 0.25f, 0.48f);
            UnityEngine.Material leather = RuntimeVisualUtility.CreateMaterial("Player_Leather", new Color(0.2f, 0.12f, 0.08f, 1f), 0f, 0.28f);
            UnityEngine.Material glow = RuntimeVisualUtility.CreateMaterial("Player_Gold_Glow", new Color(1f, 0.82f, 0.22f, 1f), 0f, 0.65f);
            UnityEngine.Material shadow = RuntimeVisualUtility.CreateMaterial("Ground_Shadow", new Color(0.03f, 0.025f, 0.02f, 1f), 0f, 0.1f);

            RuntimeVisualUtility.CreatePrimitiveChild(root, "Shadow", PrimitiveType.Cylinder, new Vector3(0f, -0.48f, 0f), new Vector3(0.92f, 0.018f, 0.92f), shadow);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.08f, 0f), new Vector3(0.62f, 0.68f, 0.62f), cloak);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "ChestPlate", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.18f), new Vector3(0.48f, 0.42f, 0.12f), armor);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.88f, 0.02f), new Vector3(0.38f, 0.38f, 0.38f), armor);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "LeftArm", PrimitiveType.Cube, new Vector3(-0.43f, 0.24f, 0.02f), new Vector3(0.16f, 0.52f, 0.16f), leather);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "RightArm", PrimitiveType.Cube, new Vector3(0.43f, 0.24f, 0.02f), new Vector3(0.16f, 0.52f, 0.16f), leather);

            GameObject weapon = RuntimeVisualUtility.CreatePrimitiveChild(root, "BladeMarker", PrimitiveType.Cube, new Vector3(0.48f, 0.28f, 0.48f), new Vector3(0.08f, 0.08f, 0.72f), glow);
            weapon.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

            GameObject crown = RuntimeVisualUtility.CreatePrimitiveChild(root, "DirectionCrest", PrimitiveType.Cube, new Vector3(0f, 1.08f, 0.24f), new Vector3(0.16f, 0.08f, 0.22f), glow);
            crown.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

            Light light = root.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.62f, 1f, 1f);
            light.range = 3f;
            light.intensity = 0.55f;
        }

        private static void BuildEnemy(Transform root)
        {
            UnityEngine.Material hide = RuntimeVisualUtility.CreateMaterial("Enemy_Crimson_Hide", new Color(0.72f, 0.12f, 0.14f, 1f), 0f, 0.32f);
            UnityEngine.Material belly = RuntimeVisualUtility.CreateMaterial("Enemy_Dark_Belly", new Color(0.18f, 0.04f, 0.05f, 1f), 0f, 0.22f);
            UnityEngine.Material eye = RuntimeVisualUtility.CreateMaterial("Enemy_Eye_Glow", new Color(1f, 0.95f, 0.36f, 1f), 0f, 0.75f);
            UnityEngine.Material shadow = RuntimeVisualUtility.CreateMaterial("Ground_Shadow", new Color(0.03f, 0.02f, 0.018f, 1f), 0f, 0.1f);

            RuntimeVisualUtility.CreatePrimitiveChild(root, "Shadow", PrimitiveType.Cylinder, new Vector3(0f, -0.5f, 0f), new Vector3(0.82f, 0.018f, 0.82f), shadow);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0f), new Vector3(0.72f, 0.58f, 0.72f), hide);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "Belly", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.32f), new Vector3(0.42f, 0.34f, 0.08f), belly);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "LeftEye", PrimitiveType.Sphere, new Vector3(-0.18f, 0.24f, 0.48f), new Vector3(0.09f, 0.09f, 0.09f), eye);
            RuntimeVisualUtility.CreatePrimitiveChild(root, "RightEye", PrimitiveType.Sphere, new Vector3(0.18f, 0.24f, 0.48f), new Vector3(0.09f, 0.09f, 0.09f), eye);

            GameObject leftHorn = RuntimeVisualUtility.CreatePrimitiveChild(root, "LeftHorn", PrimitiveType.Cube, new Vector3(-0.32f, 0.48f, 0.05f), new Vector3(0.12f, 0.32f, 0.12f), belly);
            leftHorn.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);

            GameObject rightHorn = RuntimeVisualUtility.CreatePrimitiveChild(root, "RightHorn", PrimitiveType.Cube, new Vector3(0.32f, 0.48f, 0.05f), new Vector3(0.12f, 0.32f, 0.12f), belly);
            rightHorn.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            Light light = root.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.18f, 0.12f, 1f);
            light.range = 2.3f;
            light.intensity = 0.45f;
        }
    }
}
