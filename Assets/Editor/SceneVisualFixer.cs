using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bagsys.RogueLike.EditorTools
{
    public static class SceneVisualFixer
    {
        [MenuItem("Tools/Bagsys/Fix Active Scene Visuals")]
        public static void FixActiveSceneVisuals()
        {
            HideLegacyUi();
            EnsurePlayer();
            EnsureCharacterPanelController();
            CleanupStandaloneTestEnemies();
            EnsureMainCamera();
            CleanupDuplicateEventSystems();
            CleanupDuplicateAudioListeners();

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Scene visual fix applied: legacy UI hidden, player ensured, standalone enemy cleaned, camera cleaned.");
        }

        private static void HideLegacyUi()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            Transform[] nodes = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform node in nodes)
            {
                if (node == null || node == canvas.transform)
                {
                    continue;
                }

                if (ShouldPreserve(node.name))
                {
                    continue;
                }

                node.gameObject.SetActive(false);
            }
        }

        private static bool ShouldPreserve(string nodeName)
        {
            return nodeName == "PickedItem" || nodeName == "ToolTip";
        }

        private static void EnsurePlayer()
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObject.name = "Player";
            }

            playerObject.transform.position = new Vector3(0f, 1f, 0f);
            playerObject.transform.localScale = Vector3.one;

            if (playerObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rigidbody = playerObject.AddComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            }

            if (playerObject.GetComponent<global::Player>() == null)
            {
                playerObject.AddComponent<global::Player>();
            }

            if (playerObject.GetComponent<Bagsys.RogueLike.PlayerController>() == null)
            {
                playerObject.AddComponent<Bagsys.RogueLike.PlayerController>();
            }

            if (playerObject.GetComponent<Bagsys.RogueLike.PlayerStats>() == null)
            {
                playerObject.AddComponent<Bagsys.RogueLike.PlayerStats>();
            }

            if (playerObject.GetComponent<Bagsys.RogueLike.PlayerAttack>() == null)
            {
                playerObject.AddComponent<Bagsys.RogueLike.PlayerAttack>();
            }

            SetMaterial(playerObject, "Materials/PlayerBlue", Color.blue);
            EnsurePlayerEye(playerObject);

            TrySetTag(playerObject, "Player");
        }

        private static void EnsurePlayerEye(GameObject playerObject)
        {
            Transform eye = playerObject.transform.Find("FaceMarker");
            if (eye == null)
            {
                eye = playerObject.transform.Find("PlayerEye");
            }

            if (eye == null)
            {
                GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eyeObject.name = "FaceMarker";
                Object.DestroyImmediate(eyeObject.GetComponent<Collider>());
                eyeObject.transform.SetParent(playerObject.transform, false);
                eyeObject.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                eyeObject.transform.localPosition = new Vector3(0f, 0.25f, 0.34f);
                eye = eyeObject.transform;
            }

            if (eye.name != "FaceMarker")
            {
                eye.name = "FaceMarker";
            }

            eye.SetParent(playerObject.transform, false);
            eye.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            eye.localPosition = new Vector3(0f, 0.25f, 0.34f);

            Renderer renderer = eye.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.sharedMaterial = null;
                renderer.material.color = new Color(1f, 0.92f, 0.1f, 1f);
            }
        }

        private static void EnsureCharacterPanelController()
        {
            if (Object.FindObjectOfType<CharacterPanelController>(true) != null)
            {
                return;
            }

            GameObject controllerObject = new GameObject("CharacterPanelController");
            controllerObject.AddComponent<CharacterPanelController>();
        }

        private static void CleanupStandaloneTestEnemies()
        {
            GameObject enemyObject = GameObject.Find("TestEnemy");
            if (enemyObject == null)
            {
                return;
            }

            if (enemyObject.transform.parent != null && enemyObject.transform.parent.GetComponent<Bagsys.RogueLike.Room>() != null)
            {
                return;
            }

            Object.DestroyImmediate(enemyObject);
        }

        private static void EnsureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera = cameraObject.GetComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 10f, -10f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 1f);
        }

        private static void CleanupDuplicateEventSystems()
        {
            EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null)
                {
                    Object.DestroyImmediate(eventSystems[i].gameObject);
                }
            }
        }

        private static void CleanupDuplicateAudioListeners()
        {
            AudioListener[] audioListeners = Object.FindObjectsOfType<AudioListener>(true);
            Camera mainCamera = Camera.main;

            foreach (AudioListener listener in audioListeners)
            {
                if (listener == null)
                {
                    continue;
                }

                if (mainCamera != null && listener.gameObject == mainCamera.gameObject)
                {
                    continue;
                }

                Object.DestroyImmediate(listener);
            }
        }

        private static void SetMaterial(GameObject target, string resourcePath, Color fallbackColor)
        {
            UnityEngine.Material material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/" + resourcePath + ".mat");
            if (material == null)
            {
                material = Resources.Load<UnityEngine.Material>(resourcePath);
            }

            if (material == null)
            {
                return;
            }

            material.color = fallbackColor;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void TrySetTag(GameObject target, string tagName)
        {
            try
            {
                target.tag = tagName;
            }
            catch
            {
                Debug.LogWarning($"Tag '{tagName}' is missing. Please add it in Tag Manager.");
            }
        }
    }
}