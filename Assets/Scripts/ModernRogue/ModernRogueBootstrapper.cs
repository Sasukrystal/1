using UnityEngine;
using Bagsys.RogueLike;

namespace ModernRogue
{
    [DefaultExecutionOrder(-900)]
    public class ModernRogueBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MarkModernRogueFlow()
        {
            GameBootstrapper.SuppressLegacyRunLoop = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            TinySwordsExternalSpriteLibrary.WarmArcherClipsAtStartup();
            EnsureRuntime();
        }

        private void Awake()
        {
            EnsureRuntime();
        }

        private void Start()
        {
            EnsureRuntime();
        }

        private void LateUpdate()
        {
            EnsureRuntime();
        }

        public static void EnsureRuntime()
        {
            DisableLegacyRuntimeObjects();

            if (FindObjectOfType<NewInventorySystem>() == null)
            {
                new GameObject("NewInventorySystem").AddComponent<NewInventorySystem>();
            }

            if (FindObjectOfType<RunProgressionSystem>() == null)
            {
                new GameObject("RunProgressionSystem").AddComponent<RunProgressionSystem>();
            }

            if (FindObjectOfType<ElementalCoreCombatSystem>() == null)
            {
                new GameObject("ElementalCoreCombatSystem").AddComponent<ElementalCoreCombatSystem>();
            }

            if (FindObjectOfType<UIManager>() == null)
            {
                new GameObject("ModernUIManager").AddComponent<UIManager>();
            }

            Bagsys.RogueLike.DungeonDirector oldDirector = FindObjectOfType<Bagsys.RogueLike.DungeonDirector>();
            if (oldDirector != null)
            {
                oldDirector.enabled = false;
            }

            Bagsys.RogueLike.DungeonGenerator oldGenerator = FindObjectOfType<Bagsys.RogueLike.DungeonGenerator>();
            if (oldGenerator != null)
            {
                oldGenerator.enabled = false;
            }

            DungeonLoopDirector oldLoop = FindObjectOfType<DungeonLoopDirector>();
            if (oldLoop != null)
            {
                Destroy(oldLoop.gameObject);
            }

            if (FindObjectOfType<SoulKnightDirector>() == null)
            {
                new GameObject("SoulKnightDirector").AddComponent<SoulKnightDirector>();
            }

            if (FindObjectOfType<Meat_CheatTool>() == null)
            {
                new GameObject("Meat_CheatTool").AddComponent<Meat_CheatTool>();
            }

            if (FindObjectOfType<GameAudioService>() == null)
            {
                GameAudioService.Ensure();
            }

            ModernRogueLegacyUiSuppressor.DisableConflictingLegacySystems();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                ConfigureTopDown2DPlayer(player);
            }
        }

        private static void ConfigureTopDown2DPlayer(GameObject player)
        {
            Bagsys.RogueLike.PlayerController oldController = player.GetComponent<Bagsys.RogueLike.PlayerController>();
            if (oldController != null)
            {
                DestroyImmediate(oldController);
            }

            Bagsys.RogueLike.PlayerAttack oldAttack = player.GetComponent<Bagsys.RogueLike.PlayerAttack>();
            if (oldAttack != null)
            {
                DestroyImmediate(oldAttack);
            }

            Rigidbody oldBody = player.GetComponent<Rigidbody>();
            if (oldBody != null)
            {
                DestroyImmediate(oldBody);
            }

            Collider[] oldColliders = player.GetComponents<Collider>();
            for (int i = 0; i < oldColliders.Length; i++)
            {
                if (oldColliders[i] != null)
                {
                    DestroyImmediate(oldColliders[i]);
                }
            }

            MeshRenderer meshRenderer = player.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                DestroyImmediate(meshRenderer);
            }

            MeshFilter meshFilter = player.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                DestroyImmediate(meshFilter);
            }

            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = player.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = Art2DUtility.LoadSprite("Player_Sprite");
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            spriteRenderer.sortingOrder = 0;
            HideLegacyPlayerVisualChildren(player);
            DisableLegacyPlayerVisualComponents(player);

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = player.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = false;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = player.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = player.AddComponent<CircleCollider2D>();
            }

            collider.radius = 0.45f;
            collider.isTrigger = false;

            if (player.GetComponent<PlayerController2D>() == null)
            {
                player.AddComponent<PlayerController2D>();
            }

            if (player.GetComponent<PlayerAttack2D>() == null)
            {
                player.AddComponent<PlayerAttack2D>();
            }

            SKCharacterVisual2D skVisual = player.GetComponent<SKCharacterVisual2D>();
            if (skVisual == null)
            {
                player.AddComponent<SKCharacterVisual2D>();
            }

            if (player.GetComponent<TinySwordsPlayerVisual2D>() == null)
            {
                player.AddComponent<TinySwordsPlayerVisual2D>();
            }

            if (player.GetComponent<PlayerClassVfx2D>() == null)
            {
                player.AddComponent<PlayerClassVfx2D>();
            }

            if (player.GetComponent<PlayerVisualHub2D>() == null)
            {
                player.AddComponent<PlayerVisualHub2D>();
            }

            if (player.GetComponent<PlayerLifeState2D>() == null)
            {
                player.AddComponent<PlayerLifeState2D>();
            }

            SmoothWorldHealthBar smoothHealthBar = player.GetComponent<SmoothWorldHealthBar>();
            if (smoothHealthBar != null)
            {
                DestroyImmediate(smoothHealthBar);
            }

            Bagsys.RogueLike.FloatingHealthBar floatingHealthBar = player.GetComponent<Bagsys.RogueLike.FloatingHealthBar>();
            if (floatingHealthBar != null)
            {
                DestroyImmediate(floatingHealthBar);
            }

            Transform smoothBar = player.transform.Find("SmoothHealthBar");
            if (smoothBar != null)
            {
                DestroyImmediate(smoothBar.gameObject);
            }

            Transform floatingBar = player.transform.Find("FloatingHealthBar");
            if (floatingBar != null)
            {
                DestroyImmediate(floatingBar.gameObject);
            }
        }

        private static void HideLegacyPlayerVisualChildren(GameObject player)
        {
            for (int i = 0; i < player.transform.childCount; i++)
            {
                Transform child = player.transform.GetChild(i);
                if (child == null || child.name == "SKPlayerVisual" || child.name == "TinySwordsWarriorRuntimeVisual")
                {
                    continue;
                }

                if (child.GetComponentInChildren<Renderer>(true) != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void DisableLegacyPlayerVisualComponents(GameObject player)
        {
            Bagsys.RogueLike.RuntimeCharacterVisual runtimeVisual = player.GetComponent<Bagsys.RogueLike.RuntimeCharacterVisual>();
            if (runtimeVisual != null)
            {
                DestroyImmediate(runtimeVisual);
            }

            Bagsys.RogueLike.ProceduralCharacterAnimator proceduralAnimator = player.GetComponent<Bagsys.RogueLike.ProceduralCharacterAnimator>();
            if (proceduralAnimator != null)
            {
                DestroyImmediate(proceduralAnimator);
            }

            Bagsys.RogueLike.RuntimeArtBinder artBinder = player.GetComponent<Bagsys.RogueLike.RuntimeArtBinder>();
            if (artBinder != null)
            {
                DestroyImmediate(artBinder);
            }

            Transform runtimeStylizedVisual = player.transform.Find("RuntimeStylizedVisual");
            if (runtimeStylizedVisual != null)
            {
                runtimeStylizedVisual.gameObject.SetActive(false);
            }
        }

        private static void DisableLegacyRuntimeObjects()
        {
            DisableObject("RoguelikeScenePreview");
            DisableObject("BackpackRuntime");
            DisableObject("EnhancedBackpackPanelHost");
            DisableObject("CharacterProgressionPanelHost");
            DisableObject("CharacterPanelController");

            Bagsys.RogueLike.GameFeelDirector feelDirector = FindObjectOfType<Bagsys.RogueLike.GameFeelDirector>();
            if (feelDirector != null)
            {
                feelDirector.enabled = false;
            }

            RuntimeBackpackPanel oldBackpack = FindObjectOfType<RuntimeBackpackPanel>();
            if (oldBackpack != null)
            {
                oldBackpack.enabled = false;
            }

            Bagsys.RogueLike.EnhancedBackpackPanel enhancedBackpack = FindObjectOfType<Bagsys.RogueLike.EnhancedBackpackPanel>();
            if (enhancedBackpack != null)
            {
                enhancedBackpack.enabled = false;
            }

            Bagsys.RogueLike.CharacterProgressionPanel progressionPanel = FindObjectOfType<Bagsys.RogueLike.CharacterProgressionPanel>();
            if (progressionPanel != null)
            {
                progressionPanel.enabled = false;
            }

            Bagsys.RogueLike.CharacterPanelController characterPanel = FindObjectOfType<Bagsys.RogueLike.CharacterPanelController>();
            if (characterPanel != null)
            {
                characterPanel.enabled = false;
            }
        }

        private static void DisableObject(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}
