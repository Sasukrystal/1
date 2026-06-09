using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ModernRogue.EditorTools
{
    public static class ReleaseBuildPreparer
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string StreamingAssetsRoot = "Assets/StreamingAssets";
        private const string DefaultBuildFolder = "Builds/Windows";
        private const string AutoBuildFlagFile = "Builds/auto_build_requested.txt";
        private const string CoreSourceFolderName = "核心代码";

        [MenuItem("Modern Rogue/Prepare Release Build", priority = 0)]
        public static void PrepareReleaseBuild()
        {
            ConfigurePlayerSettings();
            ConfigureBuildScenes();
            SyncResourcesForBuild();
            SyncAudioToStreamingAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ReleaseBuildPreparer] 发布准备工作已完成。可在 Build Settings 中直接 Build，或使用菜单 Modern Rogue → Build Windows Release。");
        }

        [MenuItem("Modern Rogue/Sync Resources For Build", priority = 2)]
        public static void SyncResourcesForBuildMenu()
        {
            SyncResourcesForBuild();
            AssetDatabase.Refresh();
            Debug.Log("[ReleaseBuildPreparer] 资源已同步到打包目录。");
        }

        public static void SyncResourcesForBuild()
        {
            MirrorArtIntegrationResources();
            SyncTinySwordsArcherToResources();
            SyncTinySwordsArcherToStreamingAssets();
            ValidateReleaseResources();
        }

        [MenuItem("Modern Rogue/Sync Core Source To Build Folder", priority = 3)]
        public static void SyncCoreSourceToBuildFolderMenu()
        {
            SyncCoreSourceToBuildFolder();
            Debug.Log("[ReleaseBuildPreparer] 核心代码已同步到 " + DefaultBuildFolder + "/" + CoreSourceFolderName);
        }

        [MenuItem("Modern Rogue/Build Windows Release", priority = 1)]
        public static void BuildWindowsRelease()
        {
            PrepareReleaseBuild();

            string productName = PlayerSettings.productName;
            string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(productName) ? "DarkDungeon" : productName);
            string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultBuildFolder));
            Directory.CreateDirectory(outputDir);
            string exePath = Path.Combine(outputDir, safeName + ".exe");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                SyncCoreSourceToBuildFolder();
                EditorUtility.RevealInFinder(exePath);
                Debug.Log("[ReleaseBuildPreparer] Windows 发布包已生成: " + exePath);
            }
            else
            {
                Debug.LogError("[ReleaseBuildPreparer] 构建失败: " + report.summary.result);
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoPrepareOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MainScenePath))
                {
                    return;
                }

                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                if (scenes == null || scenes.Length == 0)
                {
                    ConfigureBuildScenes();
                    Debug.Log("[ReleaseBuildPreparer] 已自动把 Main.unity 加入 Build Settings。");
                }

                TryRunPendingAutoBuild();
            };
        }

        public static void RequestAutoBuild()
        {
            string flagPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AutoBuildFlagFile));
            Directory.CreateDirectory(Path.GetDirectoryName(flagPath));
            File.WriteAllText(flagPath, System.DateTime.UtcNow.ToString("O"));
            Debug.Log("[ReleaseBuildPreparer] 已请求自动打包，Unity 编译完成后会开始构建。");
        }

        private static void TryRunPendingAutoBuild()
        {
            string flagPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AutoBuildFlagFile));
            if (!File.Exists(flagPath))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRunPendingAutoBuild;
                return;
            }

            try
            {
                File.Delete(flagPath);
            }
            catch
            {
            }

            Debug.Log("[ReleaseBuildPreparer] 检测到自动打包请求，开始 Build Windows Release...");
            BuildWindowsRelease();
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "DarkDungeon Team";
            PlayerSettings.productName = "黑暗地牢";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        private static string[] GetEnabledScenePaths()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                return new[] { MainScenePath };
            }

            int count = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return new[] { MainScenePath };
            }

            string[] paths = new string[count];
            int index = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].enabled)
                {
                    continue;
                }

                paths[index++] = scenes[i].path;
            }

            return paths;
        }

        public static void SyncAudioToStreamingAssets()
        {
            Directory.CreateDirectory(StreamingAssetsRoot);

            CopyRootMp3Files();
            CopyDirectoryIfExists(Path.Combine(Application.dataPath, "music"), Path.Combine(StreamingAssetsRoot, "music"));

            Debug.Log("[ReleaseBuildPreparer] 音频已同步到 StreamingAssets。");
        }

        private static void MirrorArtIntegrationResources()
        {
            string sourceRoot = Path.Combine(Application.dataPath, "Resources", "Art2D", "ArtIntegration");
            string targetRoot = Path.Combine(Application.dataPath, "Resources", "ArtIntegration");
            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning("[ReleaseBuildPreparer] 未找到 Art2D/ArtIntegration，跳过镜像。");
                return;
            }

            int copied = 0;
            string[] files = Directory.GetFiles(sourceRoot, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (!IsImageOrPrefabExtension(extension))
                {
                    continue;
                }

                string relative = file.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetPath = Path.Combine(targetRoot, relative);
                string targetFolder = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                File.Copy(file, targetPath, true);
                copied++;
            }

            Debug.Log("[ReleaseBuildPreparer] 已镜像 Art2D/ArtIntegration → ArtIntegration，共 " + copied + " 个文件。");
        }

        private static bool IsImageOrPrefabExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".prefab":
                    return true;
                default:
                    return false;
            }
        }

        private static void ValidateReleaseResources()
        {
            string[] requiredRelativePaths =
            {
                "Resources/Art2D/Projectile_SkeletonArrow.png",
                "Resources/Art2D/Projectile_Arrow.png",
                "Resources/Art2D/VFX_Boss_FireRune.png",
                "Resources/Art2D/VFX_Boss_RockShard.png",
                "Resources/Art2D/VFX_Boss_LightningLine.png",
                "Resources/Art2D/EnhancedBossBullet.png",
                "Resources/ArtIntegration/Player/Player_TinySwordsWarrior_Art.prefab",
                "Resources/ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_dark_0.png",
                "Resources/ArtIntegration/Environment/Lobby/Lobby_ClassStatue.png",
                "Resources/Art2D/External/TinySwords/Archer/Archer_Idle.png",
                "Resources/Art2D/External/TinySwords/Archer/Archer_Run.png",
                "Resources/Art2D/External/TinySwords/Archer/Archer_Shoot.png",
                "StreamingAssets/Art2D/External/TinySwords/Archer/Archer_Idle.png",
                "Resources/Sprites/Items/hp.png",
                "StreamingAssets/拉弓2.mp3"
            };

            int missing = 0;
            for (int i = 0; i < requiredRelativePaths.Length; i++)
            {
                string fullPath = Path.Combine(Application.dataPath, requiredRelativePaths[i].Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    missing++;
                    Debug.LogWarning("[ReleaseBuildPreparer] 打包关键资源缺失: Assets/" + requiredRelativePaths[i]);
                }
            }

            if (missing == 0)
            {
                Debug.Log("[ReleaseBuildPreparer] 关键打包资源检查通过。");
            }
            else
            {
                Debug.LogWarning("[ReleaseBuildPreparer] 关键打包资源缺失 " + missing + " 项，正式包可能和 Play 不一致。");
            }
        }

        public static void SyncTinySwordsArcherToResources()
        {
            string sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tiny Swords", "Tiny Swords", "Tiny Swords02", "Units", "Blue Units", "Archer"));
            string targetRoot = Path.Combine(Application.dataPath, "Resources", "Art2D", "External", "TinySwords", "Archer");
            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning("[ReleaseBuildPreparer] 未找到 Tiny Swords 弓手资源，打包后弓手将回退到 Resources 动画。");
                return;
            }

            Directory.CreateDirectory(targetRoot);
            string[] files = { "Archer_Idle.png", "Archer_Run.png", "Archer_Shoot.png", "Arrow.png" };
            for (int i = 0; i < files.Length; i++)
            {
                string source = Path.Combine(sourceRoot, files[i]);
                if (File.Exists(source))
                {
                    File.Copy(source, Path.Combine(targetRoot, files[i]), true);
                }
            }

            Debug.Log("[ReleaseBuildPreparer] 已同步 Tiny Swords 弓手图集到 Resources。");
        }

        public static void SyncTinySwordsArcherToStreamingAssets()
        {
            string sourceRoot = Path.Combine(Application.dataPath, "Resources", "Art2D", "External", "TinySwords", "Archer");
            string targetRoot = Path.Combine(Application.dataPath, "StreamingAssets", "Art2D", "External", "TinySwords", "Archer");
            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning("[ReleaseBuildPreparer] 未找到 Resources 弓手图集，跳过 StreamingAssets 同步。");
                return;
            }

            Directory.CreateDirectory(targetRoot);
            string[] files = { "Archer_Idle.png", "Archer_Run.png", "Archer_Shoot.png", "Arrow.png" };
            for (int i = 0; i < files.Length; i++)
            {
                string source = Path.Combine(sourceRoot, files[i]);
                if (File.Exists(source))
                {
                    File.Copy(source, Path.Combine(targetRoot, files[i]), true);
                }
            }

            Debug.Log("[ReleaseBuildPreparer] 已同步 Tiny Swords 弓手图集到 StreamingAssets。");
        }

        private static void CopyRootMp3Files()
        {
            string assetsRoot = Application.dataPath;
            string[] files = Directory.GetFiles(assetsRoot, "*.mp3", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                string target = Path.Combine(StreamingAssetsRoot, fileName);
                File.Copy(files[i], target, true);
            }
        }

        private static void CopyDirectoryIfExists(string sourceDir, string targetAssetDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            Directory.CreateDirectory(targetAssetDir);
            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                string relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetPath = Path.Combine(targetAssetDir, relative);
                string targetFolder = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                File.Copy(file, targetPath, true);
            }
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        public static void SyncCoreSourceToBuildFolder()
        {
            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultBuildFolder));
            string targetRoot = Path.Combine(buildDir, CoreSourceFolderName);
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, true);
            }

            Directory.CreateDirectory(targetRoot);

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            int copied = 0;
            copied += CopyCsTree(Path.Combine(scriptsRoot, "ModernRogue"), Path.Combine(targetRoot, "ModernRogue"));
            copied += CopyCsTree(Path.Combine(scriptsRoot, "Enemy"), Path.Combine(targetRoot, "Enemy"));
            copied += CopySingleCsFiles(scriptsRoot, targetRoot, new[]
            {
                "DungeonCrawlBaseRoomRuntimeVisual.cs",
                "TinySwordsPlayerVisual2D.cs",
                Path.Combine("Bootstrap", "GameBootstrapper.cs")
            });

            WriteCoreSourceReadme(targetRoot, copied);
        }

        private static int CopyCsTree(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                return 0;
            }

            int copied = 0;
            string[] files = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetPath = Path.Combine(targetDir, relative);
                string targetFolder = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                File.Copy(file, targetPath, true);
                copied++;
            }

            return copied;
        }

        private static int CopySingleCsFiles(string scriptsRoot, string targetRoot, string[] relativePaths)
        {
            int copied = 0;
            for (int i = 0; i < relativePaths.Length; i++)
            {
                string sourcePath = Path.Combine(scriptsRoot, relativePaths[i]);
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                string targetPath = Path.Combine(targetRoot, relativePaths[i]);
                string targetFolder = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                File.Copy(sourcePath, targetPath, true);
                copied++;
            }

            return copied;
        }

        private static void WriteCoreSourceReadme(string targetRoot, int fileCount)
        {
            string readmePath = Path.Combine(targetRoot, "README.txt");
            string content =
                "黑暗地牢 - 核心源代码\r\n" +
                "========================\r\n\r\n" +
                "本目录随 Windows 发布包一同分发，便于查阅与答辩展示。\r\n" +
                "完整 Unity 工程路径：Assets/Scripts/\r\n\r\n" +
                "目录说明：\r\n" +
                "  ModernRogue/   主玩法、UI、地牢生成、虫核、Meta 成长等核心逻辑\r\n" +
                "  Enemy/           敌人属性、AI、投射物\r\n" +
                "  Bootstrap/       启动入口（与 ModernRogueBootstrapper 配合）\r\n" +
                "  *.cs（根目录）   大厅/战斗房间视觉、TinySwords 战士表现\r\n\r\n" +
                "运行方式：双击同级目录中的「黑暗地牢.exe」即可游玩；\r\n" +
                "修改代码请在 Unity 2022.3 工程中重新编译并 Build。\r\n\r\n" +
                "同步文件数：" + fileCount + "\r\n" +
                "同步时间(UTC)：" + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
            File.WriteAllText(readmePath, content, System.Text.Encoding.UTF8);
        }
    }
}
