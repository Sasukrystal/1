using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        private const string BootstrapRootName = "GameBootstrapper";

        public static bool SuppressLegacyRunLoop { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrapperExistsBeforeSceneLoad()
        {
            if (Object.FindObjectOfType<GameBootstrapper>() != null)
            {
                return;
            }

            GameObject root = new GameObject(BootstrapRootName);
            root.AddComponent<GameBootstrapper>();
            Object.DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            Object.DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SuppressLegacyRunLoop)
            {
                EnsureEventSystem();
                EnsureLootManager();
                CleanupDuplicateEventSystems();
                CleanupDuplicateAudioListeners();
                return;
            }

            EnsureCoreSceneObjects();
            StartCoroutine(CleanupLegacyUiOverFrames());
        }

        private IEnumerator CleanupLegacyUiOverFrames()
        {
            HideLegacyUiPanels();
            yield return null;
            HideLegacyUiPanels();
            yield return null;
            HideLegacyUiPanels();
        }

        private void EnsureCoreSceneObjects()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureTooltip();
            EnsurePickedItem();
            EnsureRuntimeLegacyKnapsack();
            EnsureBackpackPanel();
            EnsureEnhancedPanels();
            EnsureGameFeelDirector();
            EnsurePlayer();
            EnsureCharacterPanelController();
            EnsureCamera();
            EnsureLootManager();
            EnsureDungeonLoopDirector();
            DisablePreviewColliders();
            HideLegacyUiPanels();
            CleanupDuplicateEventSystems();
            CleanupDuplicateAudioListeners();
        }

        private static void EnsureEventSystem()
        {
            EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            if (eventSystems.Length > 0)
            {
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    Object.Destroy(eventSystems[i].gameObject);
                }

                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureTooltip()
        {
            if (Object.FindObjectOfType<ToolTip>() != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas();
            GameObject tooltipRoot = new GameObject("ToolTip", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(ToolTip));
            tooltipRoot.transform.SetParent(canvas.transform, false);

            Text rootText = tooltipRoot.GetComponent<Text>();
            rootText.text = string.Empty;
            rootText.color = Color.white;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(Text));
            contentObject.transform.SetParent(tooltipRoot.transform, false);
            Text contentText = contentObject.GetComponent<Text>();
            contentText.text = string.Empty;
            contentText.color = Color.white;
            contentText.alignment = TextAnchor.UpperLeft;
        }

        private static void EnsurePickedItem()
        {
            GameObject pickedItem = GameObject.Find("PickedItem");
            if (pickedItem != null && pickedItem.GetComponent<ItemUI>() != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas();
            if (pickedItem == null)
            {
                pickedItem = new GameObject("PickedItem", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ItemUI));
                pickedItem.transform.SetParent(canvas.transform, false);
            }

            if (pickedItem.GetComponent<Image>() == null)
            {
                pickedItem.AddComponent<Image>();
            }

            if (pickedItem.GetComponent<ItemUI>() == null)
            {
                pickedItem.AddComponent<ItemUI>();
            }

            if (pickedItem.transform.Find("AmountText") == null)
            {
                GameObject amountObject = new GameObject("AmountText", typeof(RectTransform), typeof(Text));
                amountObject.transform.SetParent(pickedItem.transform, false);
                Text amountText = amountObject.GetComponent<Text>();
                amountText.text = string.Empty;
                amountText.alignment = TextAnchor.LowerRight;
                amountText.color = Color.white;
            }

            pickedItem.SetActive(false);
        }

        private static void EnsureRuntimeLegacyKnapsack()
        {
            if (global::Knapsack.Instance != null || Object.FindObjectOfType<global::Knapsack>(true) != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas();
            GameObject knapsackObject = new GameObject("RuntimeLegacyKnapsack", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(global::Knapsack));
            knapsackObject.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = knapsackObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.sizeDelta = new Vector2(360f, 260f);
            rootRect.anchoredPosition = new Vector2(-10000f, -10000f);

            CanvasGroup canvasGroup = knapsackObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image background = knapsackObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);
            background.raycastTarget = false;

            GridLayoutGroup grid = knapsackObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(48f, 48f);
            grid.spacing = new Vector2(4f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            for (int i = 0; i < 24; i++)
            {
                GameObject slotObject = new GameObject("RuntimeSlot_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(global::Slot));
                slotObject.transform.SetParent(knapsackObject.transform, false);
                Image slotImage = slotObject.GetComponent<Image>();
                slotImage.color = new Color(0.12f, 0.13f, 0.16f, 0.85f);
                slotImage.raycastTarget = true;
            }
        }

        private static void EnsurePlayer()
        {
            if (SuppressLegacyRunLoop)
            {
                return;
            }

            global::Player legacyPlayer = Object.FindObjectOfType<global::Player>();
            GameObject oldPlayerObject = legacyPlayer != null ? legacyPlayer.gameObject : GameObject.Find("Player");
            if (oldPlayerObject != null)
            {
                Object.Destroy(oldPlayerObject);
            }

            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.position = new Vector3(0f, 1f, 0f);

            Rigidbody rigidbody = playerObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerObject.AddComponent<Rigidbody>();
            }

            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            if (playerObject.GetComponent<global::Player>() == null)
            {
                playerObject.AddComponent<global::Player>();
            }

            if (playerObject.GetComponent<PlayerController>() == null)
            {
                playerObject.AddComponent<PlayerController>();
            }

            if (playerObject.GetComponent<PlayerStats>() == null)
            {
                playerObject.AddComponent<PlayerStats>();
            }

            if (playerObject.GetComponent<PlayerAttack>() == null)
            {
                playerObject.AddComponent<PlayerAttack>();
            }

            ApplyPrototypeMaterial(playerObject, "Materials/PlayerBlue", Color.blue);
            EnsurePlayerEye(playerObject);

            RuntimeCharacterVisual playerVisual = playerObject.GetComponent<RuntimeCharacterVisual>();
            if (playerVisual == null)
            {
                playerVisual = playerObject.AddComponent<RuntimeCharacterVisual>();
            }

            playerVisual.Configure(RuntimeCharacterVisualStyle.Player);

            ProceduralCharacterAnimator playerAnimator = playerObject.GetComponent<ProceduralCharacterAnimator>();
            if (playerAnimator == null)
            {
                playerAnimator = playerObject.AddComponent<ProceduralCharacterAnimator>();
            }

            playerAnimator.Configure(RuntimeCharacterVisualStyle.Player);

            if (playerObject.GetComponent<FloatingHealthBar>() == null)
            {
                playerObject.AddComponent<FloatingHealthBar>();
            }

            if (playerObject.GetComponent<ModernRogue.SmoothWorldHealthBar>() == null)
            {
                playerObject.AddComponent<ModernRogue.SmoothWorldHealthBar>();
            }

            RuntimeArtBinder playerArt = playerObject.GetComponent<RuntimeArtBinder>();
            if (playerArt == null)
            {
                playerArt = playerObject.AddComponent<RuntimeArtBinder>();
            }

            playerArt.SetSprite("Player_Front");

            if (playerObject.GetComponent<CharacterController>() != null)
            {
                Object.Destroy(playerObject.GetComponent<CharacterController>());
            }

            if (playerObject.tag != "Player")
            {
                try
                {
                    playerObject.tag = "Player";
                }
                catch
                {
                    Debug.LogWarning("GameBootstrapper: Player tag is missing, please create it in Tag Manager if pickup detection fails.");
                }
            }
        }

        private static void EnsureTestEnemy()
        {
            GameObject enemyObject = GameObject.Find("TestEnemy");
            if (enemyObject == null)
            {
                enemyObject = Object.FindObjectOfType<EnemyStats>() != null ? Object.FindObjectOfType<EnemyStats>().gameObject : null;
            }

            if (enemyObject == null)
            {
                enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = "TestEnemy";
                enemyObject.transform.position = new Vector3(3f, 1f, 0f);
            }
            else
            {
                enemyObject.transform.position = new Vector3(enemyObject.transform.position.x, 1f, enemyObject.transform.position.z);
            }

            Rigidbody rigidbody = enemyObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = enemyObject.AddComponent<Rigidbody>();
            }

            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            if (enemyObject.GetComponent<EnemyAI>() == null)
            {
                enemyObject.AddComponent<EnemyAI>();
            }

            if (enemyObject.GetComponent<EnemyStats>() == null)
            {
                enemyObject.AddComponent<EnemyStats>();
            }

            enemyObject.transform.localScale = Vector3.one * 0.85f;
            ApplyPrototypeMaterial(enemyObject, "Materials/EnemyRed", new Color(0.85f, 0.2f, 0.2f));

            RuntimeCharacterVisual enemyVisual = enemyObject.GetComponent<RuntimeCharacterVisual>();
            if (enemyVisual == null)
            {
                enemyVisual = enemyObject.AddComponent<RuntimeCharacterVisual>();
            }

            enemyVisual.Configure(RuntimeCharacterVisualStyle.Enemy);

            ProceduralCharacterAnimator enemyAnimator = enemyObject.GetComponent<ProceduralCharacterAnimator>();
            if (enemyAnimator == null)
            {
                enemyAnimator = enemyObject.AddComponent<ProceduralCharacterAnimator>();
            }

            enemyAnimator.Configure(RuntimeCharacterVisualStyle.Enemy);

            try
            {
                enemyObject.tag = "Enemy";
            }
            catch
            {
                Debug.LogWarning("GameBootstrapper: Enemy tag is missing, please create it in Tag Manager if combat targeting fails.");
            }
        }

        private static void HideLegacyUiPanels()
        {
            if (SuppressLegacyRunLoop)
            {
                return;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (canvas.name == "GameStartMenuCanvas")
            {
                return;
            }

            Transform[] uiNodes = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < uiNodes.Length; i++)
            {
                Transform uiNode = uiNodes[i];
                if (uiNode == null)
                {
                    continue;
                }

                if (ShouldPreserveUiNode(uiNode.name) || IsUnderRuntimeLegacyKnapsack(uiNode))
                {
                    continue;
                }

                if (uiNode != canvas.transform)
                {
                    uiNode.gameObject.SetActive(false);
                }
            }
        }

        private static bool ShouldPreserveUiNode(string objectName)
        {
            return objectName == "Canvas"
                || objectName == "ToolTip"
                || objectName == "PickedItem"
                || objectName == "RuntimeLegacyKnapsack"
                || objectName == "BackpackRuntime"
                || objectName == "CharacterPanelRuntime"
                || objectName == "EnhancedBackpackPanel"
                || objectName == "CharacterProgressionPanel"
                || objectName == "RunHud"
                || objectName == "GameStartMenuCanvas"
                || objectName == "GameStartMenuPanel";
        }

        private static bool IsUnderRuntimeLegacyKnapsack(Transform node)
        {
            Transform current = node;
            while (current != null)
            {
                if (current.name == "RuntimeLegacyKnapsack")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void EnsureBackpackPanel()
        {
            if (Object.FindObjectOfType<RuntimeBackpackPanel>(true) != null)
            {
                return;
            }

            GameObject backpackObject = new GameObject("BackpackRuntime");
            backpackObject.AddComponent<RuntimeBackpackPanel>();
        }

        private static void EnsureEnhancedPanels()
        {
            if (Object.FindObjectOfType<EnhancedBackpackPanel>(true) == null)
            {
                GameObject backpackObject = new GameObject("EnhancedBackpackPanelHost");
                backpackObject.AddComponent<EnhancedBackpackPanel>();
            }

            if (Object.FindObjectOfType<CharacterProgressionPanel>(true) == null)
            {
                GameObject progressionObject = new GameObject("CharacterProgressionPanelHost");
                progressionObject.AddComponent<CharacterProgressionPanel>();
            }
        }

        private static void EnsureGameFeelDirector()
        {
            if (SuppressLegacyRunLoop || Object.FindObjectOfType<ModernRogue.SoulKnightDirector>() != null)
            {
                return;
            }

            if (Object.FindObjectOfType<GameFeelDirector>(true) != null)
            {
                return;
            }

            GameObject directorObject = new GameObject("GameFeelDirector");
            directorObject.AddComponent<GameFeelDirector>();
        }

        private static void EnsureCamera()
        {
            Camera camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            GameObject cameraObject = camera != null ? camera.gameObject : null;

            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera = cameraObject.GetComponent<Camera>();
            }

            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 1f);
            camera.orthographic = false;
            camera.transform.position = new Vector3(0f, 12f, -12f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        private static void EnsureDungeonGenerator()
        {
            if (Object.FindObjectOfType<DungeonGenerator>() != null)
            {
                return;
            }

            GameObject dungeonObject = new GameObject("DungeonGenerator");
            dungeonObject.AddComponent<DungeonGenerator>();
        }

        private static void EnsureDungeonLoopDirector()
        {
            if (SuppressLegacyRunLoop || Object.FindObjectOfType<ModernRogue.SoulKnightDirector>() != null)
            {
                return;
            }

            DungeonDirector oldDirector = Object.FindObjectOfType<DungeonDirector>();
            if (oldDirector != null)
            {
                oldDirector.enabled = false;
            }

            DungeonGenerator oldGenerator = Object.FindObjectOfType<DungeonGenerator>();
            if (oldGenerator != null)
            {
                oldGenerator.enabled = false;
            }

            if (Object.FindObjectOfType<ModernRogue.DungeonLoopDirector>() != null)
            {
                return;
            }

            GameObject directorObject = new GameObject("DungeonLoopDirector");
            directorObject.AddComponent<ModernRogue.DungeonLoopDirector>();
        }

        private static void EnsureLootManager()
        {
            if (Object.FindObjectOfType<LootManager>() != null)
            {
                return;
            }

            GameObject lootObject = new GameObject("LootManager");
            lootObject.AddComponent<LootManager>();
        }

        private static void CleanupDuplicateEventSystems()
        {
            EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null)
                {
                    Object.Destroy(eventSystems[i].gameObject);
                }
            }
        }

        private static void CleanupDuplicateAudioListeners()
        {
            AudioListener[] audioListeners = Object.FindObjectsOfType<AudioListener>(true);
            Camera mainCamera = Camera.main;

            for (int i = 0; i < audioListeners.Length; i++)
            {
                AudioListener listener = audioListeners[i];
                if (listener == null)
                {
                    continue;
                }

                if (mainCamera != null && listener.gameObject == mainCamera.gameObject)
                {
                    continue;
                }

                Object.Destroy(listener);
            }
        }

        private static void DisablePreviewColliders()
        {
            GameObject preview = GameObject.Find("RoguelikeScenePreview");
            if (preview == null)
            {
                return;
            }

            Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        private static void ApplyPrototypeMaterial(GameObject target, string resourcePath, Color fallbackColor)
        {
            if (target == null)
            {
                return;
            }

            UnityEngine.Material material = Resources.Load<UnityEngine.Material>(resourcePath);
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);

            if (material == null)
            {
                return;
            }

            if (texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(2f, 2f);
            }
            else
            {
                material.color = fallbackColor;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private static void EnsurePlayerEye(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return;
            }

            Transform eye = playerObject.transform.Find("FaceMarker");
            if (eye == null)
            {
                eye = playerObject.transform.Find("PlayerEye");
            }

            if (eye == null)
            {
                GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eyeObject.name = "FaceMarker";
                Object.Destroy(eyeObject.GetComponent<Collider>());
                eyeObject.transform.SetParent(playerObject.transform, false);
                eyeObject.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                eyeObject.transform.localPosition = new Vector3(0f, 0.25f, 0.34f);
                eye = eyeObject.transform;
            }
            else if (eye.name != "FaceMarker")
            {
                eye.name = "FaceMarker";
            }

            eye.SetParent(playerObject.transform, false);
            eye.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            eye.localPosition = new Vector3(0f, 0.25f, 0.34f);

            if (eye != null)
            {
                Renderer eyeRenderer = eye.GetComponent<Renderer>();
                if (eyeRenderer != null)
                {
                    eyeRenderer.enabled = true;
                    eyeRenderer.material.color = new Color(1f, 0.92f, 0.1f, 1f);
                }
            }
        }

        private static void EnsureCharacterPanelController()
        {
            if (Object.FindObjectOfType<CharacterPanelController>() != null)
            {
                return;
            }

            GameObject controllerObject = new GameObject("CharacterPanelController");
            controllerObject.AddComponent<CharacterPanelController>();
        }
    }
}
