using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public sealed class DungeonCrawlBaseRoomRuntimeVisual : MonoBehaviour
    {
        public const string RootName = "DungeonCrawlBaseRoomRuntimeVisual";
        public const string ResourcePath = "ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual";

        private static readonly Vector3 RuntimePosition = new Vector3(0f, -1.3f, 0f);

        private Transform lobbyRoot;
        private GameObject visualInstance;
        private bool legacyVisualsHidden;

        public static DungeonCrawlBaseRoomRuntimeVisual EnsureForLobby(Transform lobbyRoot)
        {
            if (lobbyRoot == null)
            {
                return null;
            }

            Transform existing = lobbyRoot.Find(RootName);
            GameObject host = existing != null ? existing.gameObject : new GameObject(RootName);
            host.transform.SetParent(lobbyRoot, false);
            host.transform.localPosition = RuntimePosition;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;

            DungeonCrawlBaseRoomRuntimeVisual bridge = host.GetComponent<DungeonCrawlBaseRoomRuntimeVisual>();
            if (bridge == null)
            {
                bridge = host.AddComponent<DungeonCrawlBaseRoomRuntimeVisual>();
            }

            bridge.lobbyRoot = lobbyRoot;
            bridge.Apply();
            return bridge;
        }

        private void Start()
        {
            Apply();
        }

        private void LateUpdate()
        {
            EnsureModernUiManager();
            HideLegacySceneUi();
            HideLegacyGameFeelHud();
        }

        private void Apply()
        {
            if (lobbyRoot == null && transform.parent != null)
            {
                lobbyRoot = transform.parent;
            }

            HideLegacyLobbyVisuals();
            AlignLegacyGameplayTriggers();
            HideLegacySceneUi();
            HideLegacyGameFeelHud();
            EnsureModernUiManager();
            EnsureVisualInstance();
        }

        private static void EnsureModernUiManager()
        {
            if (FindObjectOfType<UIManager>() != null)
            {
                return;
            }

            new GameObject("ModernUIManager").AddComponent<UIManager>();
        }

        private void HideLegacyLobbyVisuals()
        {
            if (legacyVisualsHidden || lobbyRoot == null)
            {
                return;
            }

            Renderer[] renderers = lobbyRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && !renderer.transform.IsChildOf(transform))
                {
                    if (IsPlayerRenderer(renderer))
                    {
                        continue;
                    }

                    renderer.enabled = false;
                }
            }

            Graphic[] graphics = lobbyRoot.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null && !graphic.transform.IsChildOf(transform))
                {
                    graphic.enabled = false;
                }
            }

            legacyVisualsHidden = true;
        }

        private void AlignLegacyGameplayTriggers()
        {
            if (lobbyRoot == null)
            {
                return;
            }

            MoveChild("DungeonPortal", new Vector2(9.25f, 3.45f));
            MoveChild("职业雕像", new Vector2(0f, 2.6f));
            MoveChild("职业武器铺", new Vector2(-9.25f, 1.45f));
            DisablePortalBoxCollider();
        }

        private void MoveChild(string childName, Vector2 position)
        {
            Transform child = lobbyRoot.Find(childName);
            if (child != null)
            {
                child.localPosition = new Vector3(position.x, position.y, child.localPosition.z);
            }
        }

        private void DisablePortalBoxCollider()
        {
            Transform portal = lobbyRoot.Find("DungeonPortal");
            if (portal == null)
            {
                return;
            }

            BoxCollider2D box = portal.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.enabled = false;
            }
        }

        private void HideLegacySceneUi()
        {
            GameObject legacyCanvas = GameObject.Find("Canvas");
            if (legacyCanvas == null)
            {
                return;
            }

            Canvas canvas = legacyCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            GraphicRaycaster raycaster = legacyCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            Graphic[] graphics = legacyCanvas.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].enabled = false;
                }
            }

            Renderer[] renderers = legacyCanvas.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        private static void HideLegacyGameFeelHud()
        {
            Bagsys.RogueLike.GameFeelDirector feelDirector = FindObjectOfType<Bagsys.RogueLike.GameFeelDirector>();
            if (feelDirector != null)
            {
                feelDirector.enabled = false;
            }

            GameObject runHud = GameObject.Find("RunHud");
            if (runHud == null)
            {
                return;
            }

            Graphic[] graphics = runHud.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].enabled = false;
                }
            }
        }

        private void EnsureVisualInstance()
        {
            if (visualInstance != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("DungeonCrawlBaseRoomRuntimeVisual: Missing Resources prefab at " + ResourcePath);
                return;
            }

            visualInstance = Instantiate(prefab, transform);
            visualInstance.name = "BaseRoom_Visual_RuntimeInstance";
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;
            EnableRuntimeRenderers();
            DisableRuntimePhysics();
            ApplyClassChangeStatueVisual();
            ApplyWeaponShopVisual();
        }

        private void ApplyWeaponShopVisual()
        {
            if (visualInstance == null)
            {
                return;
            }

            Transform shop = FindChildRecursive(visualInstance.transform, "WeaponShop_Visual");
            if (shop == null)
            {
                return;
            }

            Sprite sprite = Art2DUtility.LoadLobbySprite("Lobby_WeaponForge");
            if (sprite == null)
            {
                return;
            }

            SpriteRenderer renderer = shop.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = sprite;
            shop.localPosition = new Vector3(-9.25f, 3.69f, -0.06f);
            shop.localScale = new Vector3(3.2f, 4f, 2f);

            HideVisualNode("WeaponShopSword_Visual");
            HideVisualNode("WeaponShopGoldenSword_Visual");
        }

        private void HideVisualNode(string nodeName)
        {
            Transform node = FindChildRecursive(visualInstance.transform, nodeName);
            if (node != null)
            {
                node.gameObject.SetActive(false);
            }
        }

        private void ApplyClassChangeStatueVisual()
        {
            if (visualInstance == null)
            {
                return;
            }

            Transform statue = FindChildRecursive(visualInstance.transform, "ClassChangeStatue_Visual");
            if (statue == null)
            {
                return;
            }

            Sprite sprite = Art2DUtility.LoadLobbySprite("Lobby_ClassStatue");
            if (sprite == null)
            {
                Debug.LogWarning("DungeonCrawlBaseRoomRuntimeVisual: Missing Lobby_ClassStatue sprite.");
                return;
            }

            SpriteRenderer renderer = statue.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = sprite;
            statue.localPosition = new Vector3(0f, 4.35f, -0.06f);
            statue.localRotation = Quaternion.identity;
            statue.localScale = new Vector3(3.933116f, 4.5f, 1f);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void EnableRuntimeRenderers()
        {
            Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = true;
                }
            }
        }

        private void DisableRuntimePhysics()
        {
            Collider2D[] colliders = visualInstance.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            Rigidbody2D[] bodies = visualInstance.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null)
                {
                    bodies[i].simulated = false;
                }
            }
        }

        private static bool IsPlayerRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
