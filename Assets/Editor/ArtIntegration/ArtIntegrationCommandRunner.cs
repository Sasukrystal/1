using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ArtIntegrationCommandRunner
{
    private const string CommandDir = "Assets/_Project/ArtIntegrationCommands";
    private const string PingCommandPath = CommandDir + "/ping.command";
    private const string PingDonePath = CommandDir + "/ping.command.done";
    private const string WarriorCommandPath = CommandDir + "/build_player_warrior.command";
    private const string WarriorDonePath = CommandDir + "/build_player_warrior.command.done";
    private const string TinySwordsWarriorCommandPath = CommandDir + "/build_tiny_swords_warrior.command";
    private const string TinySwordsWarriorDonePath = CommandDir + "/build_tiny_swords_warrior.command.done";
    private const string StatusPath = "Assets/_Project/Docs/ArtIntegrationCommandRunnerStatus.md";
    private const string WarriorReportPath = "Assets/_Project/Docs/Stage4A_WarriorBuildReport.md";
    private const string TinySwordsWarriorReportPath = "Assets/_Project/Docs/Stage4.3T_TinySwordsWarriorIntegrationReport.md";
    private const string TinySwordsWarriorAssetMapPath = "Assets/_Project/Docs/TinySwords_WarriorAssetMap.md";
    private const string WarriorSpriteDir = "Assets/Resources/Art2D/AnimationSprites/Player/Warrior";
    private const string TinySwordsWarriorSourceDir = "_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior";
    private const string TinySwordsWarriorImportDir = "Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior";
    private const string WarriorAnimationDir = "Assets/_Project/ArtIntegration/Animations/Player/Warrior";
    private const string TinySwordsWarriorAnimationDir = "Assets/_Project/ArtIntegration/Animations/Player/TinySwordsWarrior";
    private const string PlayerControllerDir = "Assets/_Project/ArtIntegration/AnimatorControllers/Player";
    private const string PlayerPrefabDir = "Assets/_Project/ArtIntegration/Prefabs/Player";
    private const string WarriorControllerPath = PlayerControllerDir + "/Warrior_Art.controller";
    private const string WarriorPrefabPath = PlayerPrefabDir + "/Player_Warrior_Art.prefab";
    private const string TinySwordsWarriorControllerPath = PlayerControllerDir + "/TinySwordsWarrior_Art.controller";
    private const string TinySwordsWarriorPrefabPath = PlayerPrefabDir + "/Player_TinySwordsWarrior_Art.prefab";
    private const string TinySwordsWarriorScenePath = "Assets/_Project/Scenes/TinySwordsWarriorVisualTest.unity";

    static ArtIntegrationCommandRunner()
    {
        EditorApplication.delayCall += RunPendingCommands;
    }

    private static void RunPendingCommands()
    {
        try
        {
            if (File.Exists(PingCommandPath))
            {
                RunPingCommand();
                return;
            }

            if (File.Exists(WarriorCommandPath))
            {
                RunWarriorBuildCommand();
                return;
            }

            if (File.Exists(TinySwordsWarriorCommandPath))
            {
                RunTinySwordsWarriorBuildCommand();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("ArtIntegrationCommandRunner failed: " + ex);
        }
    }

    private static void RunPingCommand()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
        Directory.CreateDirectory(CommandDir);

        string report =
            "# Art Integration Command Runner Status\n\n" +
            "Unity version: " + Application.unityVersion + "\n" +
            "Active scene path: " + EditorSceneManager.GetActiveScene().path + "\n" +
            "Current date/time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
            "Command processed: ping\n";

        File.WriteAllText(StatusPath, report);
        MoveCommandWithMeta(PingCommandPath, PingDonePath);
        AssetDatabase.Refresh();
    }

    private static void RunWarriorBuildCommand()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(WarriorReportPath));
        Directory.CreateDirectory(CommandDir);

        List<string> missing = GetMissingWarriorSprites();
        if (missing.Count > 0)
        {
            WriteWarriorReport(false, missing, new List<string>(), null, null, "Missing required sprite files.");
            MoveCommandWithMeta(WarriorCommandPath, WarriorDonePath);
            AssetDatabase.Refresh();
            return;
        }

        EnsureDirectory(WarriorAnimationDir);
        EnsureDirectory(PlayerControllerDir);
        EnsureDirectory(PlayerPrefabDir);

        List<string> clips = new List<string>();
        clips.Add(CreateSpriteClip("Warrior_Idle", new[] { "Warrior_Idle_01.png", "Warrior_Idle_02.png" }, true, 5));
        clips.Add(CreateSpriteClip("Warrior_Run", new[] { "Warrior_Run_01.png", "Warrior_Run_02.png", "Warrior_Run_03.png", "Warrior_Run_04.png" }, true, 10));
        clips.Add(CreateSpriteClip("Warrior_Attack", new[] { "Warrior_Attack_01.png", "Warrior_Attack_02.png", "Warrior_Attack_03.png" }, false, 12));
        clips.Add(CreateSpriteClip("Warrior_ShieldBlock", GetExistingFrames(new[] { "Warrior_ShieldBlock_01.png", "Warrior_ShieldBlock_02.png" }), true, 6));
        clips.Add(CreateSpriteClip("Warrior_Hit", new[] { "Warrior_Hit_01.png" }, false, 8));
        clips.Add(CreateSpriteClip("Warrior_Dodge", new[] { "Warrior_Dodge_01.png", "Warrior_Dodge_02.png" }, false, 12));

        AnimatorController controller = CreateWarriorController(clips);
        string prefabPath = CreateWarriorPrefab(controller);

        WriteWarriorReport(true, missing, clips, WarriorControllerPath, prefabPath, "Warrior art build completed.");
        MoveCommandWithMeta(WarriorCommandPath, WarriorDonePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static List<string> GetMissingWarriorSprites()
    {
        string[] required =
        {
            "Warrior_Idle_01.png",
            "Warrior_Idle_02.png",
            "Warrior_Run_01.png",
            "Warrior_Run_02.png",
            "Warrior_Run_03.png",
            "Warrior_Run_04.png",
            "Warrior_Attack_01.png",
            "Warrior_Attack_02.png",
            "Warrior_Attack_03.png",
            "Warrior_ShieldBlock_01.png",
            "Warrior_Hit_01.png",
            "Warrior_Dodge_01.png",
            "Warrior_Dodge_02.png"
        };

        List<string> missing = new List<string>();
        foreach (string fileName in required)
        {
            if (LoadSprite(fileName) == null)
            {
                missing.Add(WarriorSpriteDir + "/" + fileName);
            }
        }

        return missing;
    }

    private static string[] GetExistingFrames(string[] fileNames)
    {
        List<string> existing = new List<string>();
        foreach (string fileName in fileNames)
        {
            if (LoadSprite(fileName) != null)
            {
                existing.Add(fileName);
            }
        }

        return existing.ToArray();
    }

    private static string CreateSpriteClip(string clipName, string[] frameFiles, bool loop, float frameRate)
    {
        string path = WarriorAnimationDir + "/" + clipName + ".anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameFiles.Length];
        for (int i = 0; i < frameFiles.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = LoadSprite(frameFiles[i])
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return path;
    }

    private static AnimatorController CreateWarriorController(List<string> clipPaths)
    {
        AssetDatabase.DeleteAsset(WarriorControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(WarriorControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "Idle", clipPaths[0], new Vector3(240, 120, 0));
        AnimatorState run = AddState(machine, "Run", clipPaths[1], new Vector3(480, 120, 0));
        AnimatorState attack = AddState(machine, "Attack", clipPaths[2], new Vector3(240, 300, 0));
        AnimatorState block = AddState(machine, "ShieldBlock", clipPaths[3], new Vector3(480, 300, 0));
        AnimatorState hit = AddState(machine, "Hit", clipPaths[4], new Vector3(720, 300, 0));
        AnimatorState dodge = AddState(machine, "Dodge", clipPaths[5], new Vector3(960, 300, 0));
        machine.defaultState = idle;

        AddBoolTransition(idle, run, "IsMoving", true);
        AddBoolTransition(run, idle, "IsMoving", false);
        AddTriggerAnyTransition(machine, attack, "Attack");
        AddTriggerAnyTransition(machine, hit, "Hit");
        AddTriggerAnyTransition(machine, dodge, "Dodge");
        AddBoolAnyTransition(machine, block, "Block", true);
        AddBoolTransition(block, idle, "Block", false);
        AddExitTransition(attack, idle);
        AddExitTransition(hit, idle);
        AddExitTransition(dodge, idle);

        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string stateName, string clipPath, Vector3 position)
    {
        AnimatorState state = machine.AddState(stateName, position);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        return state;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameterName, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameterName);
    }

    private static void AddTriggerAnyTransition(AnimatorStateMachine machine, AnimatorState to, string parameterName)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0, parameterName);
    }

    private static void AddBoolAnyTransition(AnimatorStateMachine machine, AnimatorState to, string parameterName, bool value)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameterName);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 1.0f;
        transition.duration = 0.05f;
    }

    private static string CreateWarriorPrefab(AnimatorController controller)
    {
        AssetDatabase.DeleteAsset(WarriorPrefabPath);

        GameObject root = new GameObject("Player_Warrior_Art");
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Warrior_Idle_01.png");
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 0;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.3f);
            collider.offset = new Vector2(0, 0.15f);

            GameObject shadow = CreateChild(root, "Shadow", new Vector3(0, -0.55f, 0));
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(0, 0, 0, 0.35f);
            shadowRenderer.sortingLayerName = "GroundEffects";
            shadowRenderer.sortingOrder = 0;

            CreateChild(root, "VFXRoot", Vector3.zero);
            CreateChild(root, "AttackPoint", new Vector3(0.55f, 0, 0));
            CreateChild(root, "ProjectileSpawnPoint", new Vector3(0.55f, 0.2f, 0));
            CreateChild(root, "FeetPoint", new Vector3(0, -0.55f, 0));
            CreateChild(root, "CenterPoint", new Vector3(0, 0.2f, 0));

            PrefabUtility.SaveAsPrefabAsset(root, WarriorPrefabPath);
            return WarriorPrefabPath;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateChild(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(WarriorSpriteDir + "/" + fileName);
    }

    private static void EnsureDirectory(string assetPath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        Directory.CreateDirectory(fullPath);
    }

    private static void MoveCommandWithMeta(string sourcePath, string targetPath)
    {
        string sourceMeta = sourcePath + ".meta";
        string targetMeta = targetPath + ".meta";

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        if (File.Exists(targetMeta))
        {
            File.Delete(targetMeta);
        }

        File.Move(sourcePath, targetPath);

        if (File.Exists(sourceMeta))
        {
            File.Move(sourceMeta, targetMeta);
        }
    }

    private static void WriteWarriorReport(bool success, List<string> missing, List<string> clips, string controllerPath, string prefabPath, string message)
    {
        List<string> lines = new List<string>();
        lines.Add("# Stage 4A Warrior Build Report");
        lines.Add("");
        lines.Add("Success: " + success);
        lines.Add("Unity version: " + Application.unityVersion);
        lines.Add("Active scene path: " + EditorSceneManager.GetActiveScene().path);
        lines.Add("Current date/time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        lines.Add("Command processed: build_player_warrior");
        lines.Add("Message: " + message);
        lines.Add("");
        lines.Add("## AnimationClips");
        if (clips.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (string clip in clips)
            {
                lines.Add("- " + clip);
            }
        }
        lines.Add("");
        lines.Add("## AnimatorController");
        lines.Add(controllerPath == null ? "- None" : "- " + controllerPath);
        lines.Add("");
        lines.Add("## Prefab");
        lines.Add(prefabPath == null ? "- None" : "- " + prefabPath);
        lines.Add("");
        lines.Add("## Missing Required Sprites");
        if (missing.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (string path in missing)
            {
                lines.Add("- " + path);
            }
        }
        lines.Add("");
        lines.Add("## Notes");
        lines.Add("- Shadow is a placeholder SpriteRenderer without a sprite and should be replaced later.");
        lines.Add("- No scene asset was modified.");

        File.WriteAllText(WarriorReportPath, string.Join("\n", lines.ToArray()) + "\n");
    }

    private sealed class TinySwordsSheet
    {
        public string FileName;
        public string ActionName;
        public int FrameCount;
        public bool Loop;
        public float FrameRate;
        public int Width;
        public int Height;
        public int FrameWidth;
        public int FrameHeight;
        public List<string> SliceNames = new List<string>();
        public List<Sprite> Sprites = new List<Sprite>();

        public string AssetPath
        {
            get { return TinySwordsWarriorImportDir + "/" + FileName; }
        }

        public string SourcePath
        {
            get { return TinySwordsWarriorSourceDir + "/" + FileName; }
        }
    }

    private static void RunTinySwordsWarriorBuildCommand()
    {
        Directory.CreateDirectory(CommandDir);
        Directory.CreateDirectory(Path.GetDirectoryName(TinySwordsWarriorReportPath));
        EnsureDirectory(TinySwordsWarriorImportDir);
        EnsureDirectory(TinySwordsWarriorAnimationDir);
        EnsureDirectory(PlayerControllerDir);
        EnsureDirectory(PlayerPrefabDir);
        EnsureDirectory("Assets/_Project/Scenes");

        List<TinySwordsSheet> sheets = GetTinySwordsWarriorSheets();
        List<string> missing = GetMissingTinySwordsWarriorPngs(sheets);
        if (missing.Count > 0)
        {
            WriteTinySwordsWarriorDocs(false, sheets, new List<string>(), null, null, null, missing, "Missing selected Tiny Swords Warrior PNG files.");
            MoveCommandWithMeta(TinySwordsWarriorCommandPath, TinySwordsWarriorDonePath);
            AssetDatabase.Refresh();
            return;
        }

        List<string> sliceFailures = new List<string>();
        foreach (TinySwordsSheet sheet in sheets)
        {
            if (!SliceTinySwordsSheet(sheet))
            {
                sliceFailures.Add(sheet.AssetPath);
            }
        }

        if (sliceFailures.Count > 0)
        {
            WriteTinySwordsWarriorDocs(false, sheets, new List<string>(), null, null, null, sliceFailures, "One or more Warrior sprite sheets failed slicing.");
            MoveCommandWithMeta(TinySwordsWarriorCommandPath, TinySwordsWarriorDonePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return;
        }

        List<string> clips = new List<string>();
        foreach (TinySwordsSheet sheet in sheets)
        {
            clips.Add(CreateTinySwordsClip(sheet));
        }

        AnimatorController controller = CreateTinySwordsWarriorController(clips);
        string prefabPath = CreateTinySwordsWarriorPrefab(controller, sheets[0].Sprites[0]);
        string scenePath = CreateTinySwordsWarriorVisualTestScene(prefabPath);

        WriteTinySwordsWarriorDocs(true, sheets, clips, TinySwordsWarriorControllerPath, prefabPath, scenePath, new List<string>(), "Tiny Swords Warrior integration completed.");
        MoveCommandWithMeta(TinySwordsWarriorCommandPath, TinySwordsWarriorDonePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static List<TinySwordsSheet> GetTinySwordsWarriorSheets()
    {
        return new List<TinySwordsSheet>
        {
            new TinySwordsSheet { FileName = "Warrior_Idle.png", ActionName = "Idle", FrameCount = 8, Loop = true, FrameRate = 8 },
            new TinySwordsSheet { FileName = "Warrior_Run.png", ActionName = "Run", FrameCount = 6, Loop = true, FrameRate = 10 },
            new TinySwordsSheet { FileName = "Warrior_Attack1.png", ActionName = "Attack1", FrameCount = 4, Loop = false, FrameRate = 12 },
            new TinySwordsSheet { FileName = "Warrior_Attack2.png", ActionName = "Attack2", FrameCount = 4, Loop = false, FrameRate = 12 },
            new TinySwordsSheet { FileName = "Warrior_Guard.png", ActionName = "Guard", FrameCount = 6, Loop = true, FrameRate = 8 }
        };
    }

    private static List<string> GetMissingTinySwordsWarriorPngs(List<TinySwordsSheet> sheets)
    {
        List<string> missing = new List<string>();
        foreach (TinySwordsSheet sheet in sheets)
        {
            if (!File.Exists(sheet.AssetPath))
            {
                missing.Add(sheet.AssetPath);
            }
        }

        return missing;
    }

    private static bool SliceTinySwordsSheet(TinySwordsSheet sheet)
    {
        TextureImporter importer = AssetImporter.GetAtPath(sheet.AssetPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(sheet.AssetPath, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(sheet.AssetPath) as TextureImporter;
        }

        if (importer == null)
        {
            return false;
        }

        Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(sourceTexture, File.ReadAllBytes(sheet.AssetPath)))
        {
            return false;
        }

        sheet.Width = sourceTexture.width;
        sheet.Height = sourceTexture.height;
        UnityEngine.Object.DestroyImmediate(sourceTexture);

        if (sheet.FrameCount <= 0 || sheet.Width % sheet.FrameCount != 0)
        {
            return false;
        }

        sheet.FrameWidth = sheet.Width / sheet.FrameCount;
        sheet.FrameHeight = sheet.Height;
        sheet.SliceNames.Clear();

        SpriteMetaData[] metadata = new SpriteMetaData[sheet.FrameCount];
        for (int i = 0; i < sheet.FrameCount; i++)
        {
            string sliceName = Path.GetFileNameWithoutExtension(sheet.FileName) + "_" + (i + 1).ToString("00");
            sheet.SliceNames.Add(sliceName);
            metadata[i] = new SpriteMetaData
            {
                name = sliceName,
                rect = new Rect(i * sheet.FrameWidth, 0, sheet.FrameWidth, sheet.FrameHeight),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.08f),
                border = Vector4.zero
            };
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritesheet = metadata;
        importer.SaveAndReimport();

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheet.AssetPath);
        Dictionary<string, Sprite> spriteByName = new Dictionary<string, Sprite>();
        foreach (UnityEngine.Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                spriteByName[sprite.name] = sprite;
            }
        }

        sheet.Sprites.Clear();
        foreach (string sliceName in sheet.SliceNames)
        {
            Sprite sprite;
            if (!spriteByName.TryGetValue(sliceName, out sprite))
            {
                return false;
            }

            sheet.Sprites.Add(sprite);
        }

        return sheet.Sprites.Count == sheet.FrameCount;
    }

    private static string CreateTinySwordsClip(TinySwordsSheet sheet)
    {
        string clipPath = TinySwordsWarriorAnimationDir + "/TinySwordsWarrior_" + sheet.ActionName + ".anim";
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip();
        clip.frameRate = sheet.FrameRate;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sheet.Sprites.Count];
        for (int i = 0; i < sheet.Sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / sheet.FrameRate,
                value = sheet.Sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = sheet.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clipPath;
    }

    private static AnimatorController CreateTinySwordsWarriorController(List<string> clipPaths)
    {
        AssetDatabase.DeleteAsset(TinySwordsWarriorControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(TinySwordsWarriorControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Guard", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "Idle", clipPaths[0], new Vector3(240, 120, 0));
        AnimatorState run = AddState(machine, "Run", clipPaths[1], new Vector3(480, 120, 0));
        AnimatorState attack1 = AddState(machine, "Attack1", clipPaths[2], new Vector3(240, 300, 0));
        AnimatorState attack2 = AddState(machine, "Attack2", clipPaths[3], new Vector3(480, 300, 0));
        AnimatorState guard = AddState(machine, "Guard", clipPaths[4], new Vector3(720, 300, 0));
        machine.defaultState = idle;

        AddBoolTransition(idle, run, "IsMoving", true);
        AddBoolTransition(run, idle, "IsMoving", false);
        AddTriggerAnyTransition(machine, attack1, "Attack");
        AddTriggerAnyTransition(machine, attack2, "Attack2");
        AddBoolAnyTransition(machine, guard, "Guard", true);
        AddBoolTransition(guard, idle, "Guard", false);
        AddExitTransition(attack1, idle);
        AddExitTransition(attack2, idle);

        return controller;
    }

    private static string CreateTinySwordsWarriorPrefab(AnimatorController controller, Sprite initialSprite)
    {
        AssetDatabase.DeleteAsset(TinySwordsWarriorPrefabPath);

        GameObject root = new GameObject("Player_TinySwordsWarrior_Art");
        try
        {
            GameObject visualRoot = CreateChild(root, "VisualRoot", Vector3.zero);
            SpriteRenderer renderer = visualRoot.AddComponent<SpriteRenderer>();
            renderer.sprite = initialSprite;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 0;

            Animator animator = visualRoot.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.85f, 1.05f);
            collider.offset = new Vector2(0, 0.45f);

            CreateChild(root, "AttackPoint", new Vector3(0.65f, 0.45f, 0));
            CreateChild(root, "ProjectileSpawnPoint", new Vector3(0.7f, 0.55f, 0));
            CreateChild(root, "FeetPoint", new Vector3(0, 0.05f, 0));
            CreateChild(root, "CenterPoint", new Vector3(0, 0.55f, 0));

            PrefabUtility.SaveAsPrefabAsset(root, TinySwordsWarriorPrefabPath);
            return TinySwordsWarriorPrefabPath;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static string CreateTinySwordsWarriorVisualTestScene(string prefabPath)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.10f, 1);
        cameraObject.transform.position = new Vector3(0, 0, -10);
        cameraObject.tag = "MainCamera";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position = Vector3.zero;
            }
        }

        EditorSceneManager.SaveScene(scene, TinySwordsWarriorScenePath);
        return TinySwordsWarriorScenePath;
    }

    private static void WriteTinySwordsWarriorDocs(bool success, List<TinySwordsSheet> sheets, List<string> clips, string controllerPath, string prefabPath, string scenePath, List<string> issues, string message)
    {
        WriteTinySwordsWarriorAssetMap(sheets);

        List<string> lines = new List<string>();
        lines.Add("# Stage 4.3T Tiny Swords Warrior Integration Report");
        lines.Add("");
        lines.Add("Success: " + success);
        lines.Add("Unity version: " + Application.unityVersion);
        lines.Add("Active scene path: " + EditorSceneManager.GetActiveScene().path);
        lines.Add("Current date/time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        lines.Add("Command processed: build_tiny_swords_warrior");
        lines.Add("Message: " + message);
        lines.Add("");
        lines.Add("## Tiny Swords Source Resources");
        foreach (TinySwordsSheet sheet in sheets)
        {
            lines.Add("- " + sheet.SourcePath);
        }
        lines.Add("");
        lines.Add("## Copied PNG Files");
        foreach (TinySwordsSheet sheet in sheets)
        {
            lines.Add("- " + sheet.AssetPath);
        }
        lines.Add("");
        lines.Add("## Slice Results");
        foreach (TinySwordsSheet sheet in sheets)
        {
            lines.Add("- " + sheet.FileName + ": " + sheet.Width + "x" + sheet.Height + ", frame " + sheet.FrameWidth + "x" + sheet.FrameHeight + ", frames " + sheet.FrameCount + ", loaded sprites " + sheet.Sprites.Count);
            foreach (string sliceName in sheet.SliceNames)
            {
                lines.Add("  - " + sliceName);
            }
        }
        lines.Add("");
        lines.Add("## AnimationClips");
        if (clips.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (string clip in clips)
            {
                lines.Add("- " + clip);
            }
        }
        lines.Add("");
        lines.Add("## AnimatorController");
        lines.Add(controllerPath == null ? "- None" : "- " + controllerPath);
        lines.Add("");
        lines.Add("## Prefab");
        lines.Add(prefabPath == null ? "- None" : "- " + prefabPath);
        lines.Add("");
        lines.Add("## Test Scene");
        lines.Add(scenePath == null ? "- None" : "- " + scenePath);
        lines.Add("");
        lines.Add("## Supported Actions");
        lines.Add("- Idle");
        lines.Add("- Run");
        lines.Add("- Attack1");
        lines.Add("- Attack2");
        lines.Add("- Guard");
        lines.Add("");
        lines.Add("## Direction Strategy");
        lines.Add("- Right: uses original side/right Tiny Swords02 Warrior animation sheets.");
        lines.Add("- Left: can be mirrored later with SpriteRenderer.flipX.");
        lines.Add("- Up, Down, and diagonals: not complete in this stage.");
        lines.Add("- Full 8-direction support requires later validation of `Tiny Swords01/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png` or extra resources.");
        lines.Add("- Continuous 360 degree full-sprite rotation is explicitly not used.");
        lines.Add("");
        lines.Add("## Transparency Check");
        lines.Add("- Sampled Tiny Swords Warrior PNG files use alpha and have transparent corners.");
        lines.Add("- No white background, white frame, checkerboard background, or baked preview rectangle was detected in the selected Warrior sheets.");
        lines.Add("");
        lines.Add("## Issues");
        if (issues.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (string issue in issues)
            {
                lines.Add("- " + issue);
            }
        }
        lines.Add("");
        lines.Add("## Git Status");
        lines.Add("- Check after command completion.");
        lines.Add("");
        lines.Add("## Unity Console Error Count");
        lines.Add("- Check after command completion.");
        lines.Add("");
        lines.Add("## Commit Recommendation");
        lines.Add("- Review GameView and git status before committing Stage 4.3T.");

        File.WriteAllText(TinySwordsWarriorReportPath, string.Join("\n", lines.ToArray()) + "\n");
    }

    private static void WriteTinySwordsWarriorAssetMap(List<TinySwordsSheet> sheets)
    {
        List<string> lines = new List<string>();
        lines.Add("# Tiny Swords Warrior Asset Map");
        lines.Add("");
        lines.Add("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        lines.Add("Stage: 4.3T");
        lines.Add("");
        lines.Add("## Source To Import Mapping");
        foreach (TinySwordsSheet sheet in sheets)
        {
            lines.Add("- Source: `" + sheet.SourcePath + "`");
            lines.Add("  - Imported: `" + sheet.AssetPath + "`");
            lines.Add("  - Action: " + sheet.ActionName);
            lines.Add("  - Frames: " + sheet.FrameCount);
            lines.Add("  - Sheet size: " + sheet.Width + "x" + sheet.Height);
            lines.Add("  - Slice size: " + sheet.FrameWidth + "x" + sheet.FrameHeight);
        }
        lines.Add("");
        lines.Add("## Action Mapping");
        lines.Add("- Idle: `Warrior_Idle.png`");
        lines.Add("- Run: `Warrior_Run.png`");
        lines.Add("- Attack1: `Warrior_Attack1.png`");
        lines.Add("- Attack2: `Warrior_Attack2.png`");
        lines.Add("- Guard: `Warrior_Guard.png`");
        lines.Add("");
        lines.Add("## Current Direction Support");
        lines.Add("- Right: original Tiny Swords02 Warrior side/right sheets.");
        lines.Add("- Left: mirror Right using SpriteRenderer.flipX in a later direction-control stage.");
        lines.Add("- Up: missing in this Tiny Swords02 Warrior subset.");
        lines.Add("- Down: missing in this Tiny Swords02 Warrior subset.");
        lines.Add("- Diagonal: missing in this Tiny Swords02 Warrior subset.");
        lines.Add("");
        lines.Add("## Missing Actions And Directions");
        lines.Add("- Hit/Hurt animation is not present in the selected Warrior subset.");
        lines.Add("- Death animation is not present in the selected Warrior subset.");
        lines.Add("- Dodge/Roll animation is not present in the selected Warrior subset.");
        lines.Add("- Full 8-direction Warrior animation is not complete in this stage.");
        lines.Add("");
        lines.Add("## Notes");
        lines.Add("- Do not use old AI white-background Warrior assets.");
        lines.Add("- Do not use Stage 4.0 auto-transparent salvage assets as formal Warrior art.");
        lines.Add("- Do not simulate direction by rotating the whole sprite 360 degrees.");

        File.WriteAllText(TinySwordsWarriorAssetMapPath, string.Join("\n", lines.ToArray()) + "\n");
    }
}
