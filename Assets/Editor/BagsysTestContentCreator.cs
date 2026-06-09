using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bagsys.RogueLike.EditorTools
{
    [InitializeOnLoad]
    public static class BagsysTestContentCreator
    {
        private const string EnemyPrefabPath = "Assets/Prefabs/TestEnemy.prefab";
        private const string DropPrefabPath = "Assets/Prefabs/DropItemPrefab.prefab";

        static BagsysTestContentCreator()
        {
            EditorApplication.delayCall += AutoCreateMissingTestContent;
        }

        private static void AutoCreateMissingTestContent()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath) != null && AssetDatabase.LoadAssetAtPath<GameObject>(DropPrefabPath) != null)
            {
                return;
            }

            CreateTestContent();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Bagsys/Create Test Enemy And Drop Prefabs")]
        public static void CreateTestContent()
        {
            EnsureEnemyTagExists();

            GameObject dropPrefab = CreateDropPrefab();
            GameObject enemyPrefab = CreateEnemyPrefab();

            EnsureLootManager(dropPrefab);

            Debug.Log($"Bagsys test content ready. Enemy prefab: {AssetDatabase.GetAssetPath(enemyPrefab)}, Drop prefab: {AssetDatabase.GetAssetPath(dropPrefab)}");
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "TestEnemy";
            enemy.tag = "Enemy";
            enemy.transform.position = new Vector3(0f, 0.5f, 0f);

            Rigidbody rigidbody = enemy.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = enemy.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            if (enemy.GetComponent<Bagsys.RogueLike.EnemyAI>() == null)
            {
                enemy.AddComponent<Bagsys.RogueLike.EnemyAI>();
            }

            if (enemy.GetComponent<Bagsys.RogueLike.EnemyStats>() == null)
            {
                enemy.AddComponent<Bagsys.RogueLike.EnemyStats>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject CreateDropPrefab()
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.name = "DropItemPrefab";
            drop.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Rigidbody rigidbody = drop.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = drop.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (drop.GetComponent<Bagsys.RogueLike.WorldItem>() == null)
            {
                drop.AddComponent<Bagsys.RogueLike.WorldItem>();
            }

            Bagsys.RogueLike.WorldItem worldItem = drop.GetComponent<Bagsys.RogueLike.WorldItem>();
            worldItem.Initialize(1, 1);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(drop, DropPrefabPath);
            Object.DestroyImmediate(drop);
            return prefab;
        }

        private static void EnsureLootManager(GameObject dropPrefab)
        {
            GameObject lootObject = GameObject.Find("LootManager");
            if (lootObject == null)
            {
                lootObject = new GameObject("LootManager");
            }

            Bagsys.RogueLike.LootManager lootManager = lootObject.GetComponent<Bagsys.RogueLike.LootManager>();
            if (lootManager == null)
            {
                lootManager = lootObject.AddComponent<Bagsys.RogueLike.LootManager>();
            }

            lootManager.SetDropPrefab(dropPrefab);

            if (SceneManager.GetActiveScene().isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorUtility.SetDirty(lootManager);
        }

        private static void EnsureEnemyTagExists()
        {
            if (System.Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, tag => tag == "Enemy"))
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            int newIndex = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(newIndex);
            tagsProp.GetArrayElementAtIndex(newIndex).stringValue = "Enemy";
            tagManager.ApplyModifiedProperties();
        }
    }
}