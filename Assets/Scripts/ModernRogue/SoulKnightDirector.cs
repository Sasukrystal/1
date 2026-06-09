using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DefaultExecutionOrder(-440)]
    [DisallowMultipleComponent]
    public class SoulKnightDirector : MonoBehaviour
    {
        public static SoulKnightDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private GameObject currentRoot;
        private Transform player;
        private Canvas canvas;
        private Text hudText;
        private Text combatInfoText;
        private Text pickupTipText;
        private GameObject pickupTipPanel;
        private Image hpFill;
        private Image manaFill;
        private Image xpFill;
        private Image topXpFill;
        private Text hpLabel;
        private Text manaLabel;
        private Text xpLabel;
        private Text topXpLabel;
        private float pickupTipUntil;
        private GameObject settlementPanel;
        private GameObject toastPanel;
        private int currentStage;
        private int runGold;
        private int killCount;
        private bool pendingPlayerPlacement;
        private Vector2 pendingSpawn;
        private Coroutine stageLoadRoutine;
        private Coroutine assetWarmRoutine;
        private bool combatSpritesWarmed;
        private System.Action stageLoadCompleteCallback;

        public int CurrentStage => currentStage;
        public int RunGold => runGold;
        public int KillCount => killCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }
        }

        private void Start()
        {
            EnsureHud();
            GameAudioService.Ensure();
        }

        public void EnsureStartupMenuVisible()
        {
            if (RunProgressionSystem.Instance != null && RunProgressionSystem.Instance.IsRunActive && currentStage > 0 && currentRoot != null)
            {
                ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(false);
                return;
            }

            RequestStartupMenu();
        }

        public void RequestStartupMenu()
        {
            if (GameStartMenuPanel.IsVisible)
            {
                ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(true);
                return;
            }

            currentStage = 0;
            if (currentRoot != null)
            {
                CleanupCurrent();
            }

            ModernRogueLegacyUiSuppressor.DisableConflictingLegacySystems();
            GameStartMenuPanel.Show(this);
            ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(true);
        }

        public void SetHudVisible(bool visible)
        {
            EnsureHud();
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            if (GameStartMenuPanel.IsVisible)
            {
                return;
            }

            if (currentStage <= 0 && currentRoot == null && !GameStartMenuPanel.IsVisible && stageLoadRoutine == null)
            {
                RequestStartupMenu();
                return;
            }

            TryPlacePlayer();
            RefreshHud(currentStage == 0 ? "Lobby" : "1-" + currentStage);
            RefreshCombatInfo();
        }

        public void LoadLobby()
        {
            Time.timeScale = 1f;
            ModernRogueLegacyUiSuppressor.ApplyStartupPresentation(false);
            GameAudioService.Ensure()?.SetBossBattleActive(false);
            if (stageLoadRoutine != null)
            {
                StopCoroutine(stageLoadRoutine);
                stageLoadRoutine = null;
            }

            currentStage = 0;
            runGold = 0;
            killCount = 0;
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.EndRun();
            }
            CleanupCurrent();
            currentRoot = new GameObject("SoulKnight_Lobby");
            SoulKnightDungeonBuilder.BuildLobby(currentRoot.transform, this);
            DungeonCrawlBaseRoomRuntimeVisual.EnsureForLobby(currentRoot.transform);
            QueuePlayerPlacement(new Vector2(0f, -7.8f));
            ResetPlayerLifeState();
            RefreshHud("Lobby");
            HideSettlement();
            BeginWarmCombatSprites();
        }

        private void ResetPlayerLifeState()
        {
            CachePlayer();
            if (player != null)
            {
                PlayerLifeState2D.Ensure(player)?.ResetForRun();
            }
        }

        private void BeginWarmCombatSprites()
        {
            if (combatSpritesWarmed)
            {
                return;
            }

            if (assetWarmRoutine != null)
            {
                StopCoroutine(assetWarmRoutine);
            }

            assetWarmRoutine = StartCoroutine(WarmCombatSpritesRoutine());
        }

        private IEnumerator WarmCombatSpritesRoutine()
        {
            yield return Art2DUtility.WarmCombatSpritesAsync();
            combatSpritesWarmed = true;
            assetWarmRoutine = null;
        }

        public void StartRun()
        {
            Time.timeScale = 1f;
            runGold = 0;
            killCount = 0;
            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.BeginRun();
            }

            BeginLoadStage(1, new Vector2(0f, -45f), true);
        }

        public void LoadNextStage()
        {
            if (currentStage >= 5)
            {
                return;
            }

            BeginLoadStage(currentStage + 1, new Vector2(0f, -45f), true);
        }

        public void LoadStage(int stage)
        {
            BeginLoadStage(stage, new Vector2(0f, -45f), true);
        }

        public void LoadBossDebugStage(int stage)
        {
            Time.timeScale = 1f;
            if (RunProgressionSystem.Instance != null && !RunProgressionSystem.Instance.IsRunActive)
            {
                RunProgressionSystem.Instance.BeginRun();
            }

            BeginLoadStage(Mathf.Clamp(stage, 1, 5), new Vector2(0f, -45f), false, TeleportToBossRoomAndStart);
        }

        public void DebugStartCurrentRoom()
        {
            RoomTrigger2D room = FindPlayerRoomTrigger();
            if (room == null)
            {
                ShowToast("当前不在战斗房间");
                return;
            }

            room.CheatStartEncounter();
        }

        public void DebugCompleteCurrentRoom()
        {
            RoomTrigger2D room = FindPlayerRoomTrigger();
            if (room == null)
            {
                ShowToast("当前不在战斗房间");
                return;
            }

            room.CheatForceClear();
        }

        public void DebugEnterCurrentStageShop()
        {
            if (currentStage <= 0)
            {
                ShowToast("当前不在冒险关卡中");
                return;
            }

            Transform merchant = FindCurrentStageMerchant();
            if (merchant != null)
            {
                Vector2 spawn = (Vector2)merchant.position + new Vector2(0f, -1.8f);
                QueuePlayerPlacement(spawn);
                TryPlacePlayer();
            }
            else
            {
                ShowToast("本层地图里没有商人房间，直接打开商店界面。");
            }

            RogueShopPanel.Open(this);
        }

        private Transform FindCurrentStageMerchant()
        {
            if (currentRoot == null)
            {
                return null;
            }

            SoulKnightShopStand[] stands = currentRoot.GetComponentsInChildren<SoulKnightShopStand>(true);
            if (stands != null && stands.Length > 0 && stands[0] != null)
            {
                return stands[0].transform;
            }

            Transform[] children = currentRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == "牢骚商人")
                {
                    return child;
                }
            }

            return null;
        }

        private void BeginLoadStage(int stage, Vector2 spawn, bool saveAfterLoad, System.Action onComplete = null)
        {
            if (stageLoadRoutine != null)
            {
                StopCoroutine(stageLoadRoutine);
            }

            stageLoadCompleteCallback = onComplete;
            stageLoadRoutine = StartCoroutine(LoadStageRoutine(stage, spawn, saveAfterLoad));
        }

        private IEnumerator LoadStageRoutine(int stage, Vector2 spawn, bool saveAfterLoad)
        {
            Time.timeScale = 1f;
            currentStage = Mathf.Clamp(stage, 1, 5);
            GameObject previousRoot = currentRoot;
            currentRoot = new GameObject("SoulKnight_Stage_1-" + currentStage);

            SoulKnightStageProfile profile = SoulKnightStageProfiles.Resolve(currentStage);
            bool playerPlaced = false;
            yield return SoulKnightDungeonBuilder.BuildStageAsync(
                currentRoot.transform,
                this,
                currentStage,
                profile,
                () =>
                {
                    if (playerPlaced)
                    {
                        return;
                    }

                    QueuePlayerPlacement(spawn);
                    playerPlaced = true;
                    DestroyStageRoot(previousRoot);
                });

            if (!playerPlaced)
            {
                QueuePlayerPlacement(spawn);
            }

            DestroyStageRoot(previousRoot);

            RefreshHud("1-" + currentStage);
            GameAudioService.Ensure()?.SetBossBattleActive(false);
            if (saveAfterLoad)
            {
                GameSaveService.SaveCurrentRun(this, false);
                StartCoroutine(FlushRunSaveNextFrame());
            }

            System.Action callback = stageLoadCompleteCallback;
            stageLoadCompleteCallback = null;
            callback?.Invoke();
            stageLoadRoutine = null;
        }

        private void TeleportToBossRoomAndStart()
        {
            RoomTrigger2D bossRoom = FindBossRoomTrigger();
            if (bossRoom == null)
            {
                ShowToast("未找到 Boss 房，请稍后重试");
                return;
            }

            Vector2 spawn = bossRoom.transform.position;
            QueuePlayerPlacement(spawn);
            pendingPlayerPlacement = true;
            TryPlacePlayer();
            bossRoom.CheatStartEncounter();
            string toast = currentStage >= 5
                ? "已进入第 " + currentStage + " 层 Boss 战（始终双 Boss）"
                : "已进入第 " + currentStage + " 层 Boss 战";
            ShowToast(toast);
        }

        private static RoomTrigger2D FindBossRoomTrigger()
        {
            RoomTrigger2D[] rooms = FindObjectsOfType<RoomTrigger2D>();
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] != null && rooms[i].IsBossRoom)
                {
                    return rooms[i];
                }
            }

            return null;
        }

        public static RoomTrigger2D FindPlayerRoomTrigger()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return null;
            }

            Vector2 playerPosition = playerObject.transform.position;
            RoomTrigger2D[] rooms = FindObjectsOfType<RoomTrigger2D>();
            RoomTrigger2D best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] == null)
                {
                    continue;
                }

                if (rooms[i].ContainsWorldPoint(playerPosition))
                {
                    return rooms[i];
                }

                float distance = Vector2.Distance(playerPosition, rooms[i].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = rooms[i];
                }
            }

            return bestDistance <= 18f ? best : null;
        }

        private IEnumerator FlushRunSaveNextFrame()
        {
            yield return null;
            GameSaveService.SaveCurrentRun(this);
        }

        public void RestoreRunState(int stage, int savedRunGold, int savedKillCount, Vector2 spawn)
        {
            runGold = savedRunGold;
            killCount = savedKillCount;
            BeginLoadStage(stage, spawn, false);
        }

        public void AddRunGold(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            runGold += safeAmount;
            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.EarnCoins(safeAmount);
            }

            RefreshHud(currentStage == 0 ? "Lobby" : "1-" + currentStage);
        }

        public void ShowToast(string message)
        {
            EnsureHud();
            if (toastPanel != null)
            {
                Destroy(toastPanel);
            }

            toastPanel = new GameObject("SoulKnightToast", typeof(RectTransform), typeof(Image));
            toastPanel.transform.SetParent(canvas.transform, false);
            RectTransform rect = toastPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.82f);
            rect.anchorMax = new Vector2(0.5f, 0.82f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 78f);
            rect.anchoredPosition = Vector2.zero;
            toastPanel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.055f, 0.92f);

            Text text = CreateText(toastPanel.transform, "Text", message, 20, FontStyle.Bold, new Color(1f, 0.93f, 0.58f, 1f));
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16f, 10f);
            text.rectTransform.offsetMax = new Vector2(-16f, -10f);
            Destroy(toastPanel, 2.2f);
        }

        public void ShowPickupTip(string message)
        {
            EnsureHud();
            if (pickupTipText == null)
            {
                return;
            }

            pickupTipText.text = message;
            pickupTipUntil = Time.time + 2.1f;
            pickupTipText.enabled = true;
            if (pickupTipPanel != null)
            {
                pickupTipPanel.SetActive(true);
            }
        }

        public void RegisterKill()
        {
            killCount++;
            RefreshHud(currentStage == 0 ? "Lobby" : "1-" + currentStage);
        }

        public void ShowSettlement()
        {
            ShowRunResultPanel(true);
        }

        public void ShowDeathSettlement()
        {
            ShowRunResultPanel(false);
        }

        public void AbandonRunAndRestart()
        {
            UIManager.Instance?.CloseUnified();
            ShowRunResultPanel(
                false,
                () =>
                {
                    GameSaveService.ClearRunSave();
                    LoadLobby();
                },
                "重新开始",
                "你已结束本次冒险，将回到基地开启全新征程。");
        }

        public void AbandonRunAndReturnToMainMenu()
        {
            UIManager.Instance?.CloseUnified();
            ShowRunResultPanel(
                false,
                () => RequestStartupMenu(),
                "返回主菜单",
                "你已结束本次冒险，局外成长仍会保留。");
        }

        private void ShowRunResultPanel(bool victory)
        {
            ShowRunResultPanel(victory, null, null, null);
        }

        private void ShowRunResultPanel(bool victory, System.Action confirmAction, string confirmLabel, string subtitleOverride)
        {
            EnsureHud();
            Time.timeScale = 0f;
            if (settlementPanel != null)
            {
                Destroy(settlementPanel);
            }

            int stage = currentStage;
            int kills = killCount;
            int gold = NewInventorySystem.Instance != null ? NewInventorySystem.Instance.PlayerStats.coins : runGold;
            int level = NewInventorySystem.Instance != null ? NewInventorySystem.Instance.PlayerStats.level : 1;

            if (RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.EndRun();
            }

            settlementPanel = new GameObject("SoulKnightSettlementRoot", typeof(RectTransform));
            settlementPanel.transform.SetParent(canvas.transform, false);
            settlementPanel.AddComponent<ModalPauseToken>();
            RectTransform rootRect = settlementPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(settlementPanel.transform, false);
            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            RuntimeUIVisuals.StyleImage(backdrop.GetComponent<Image>(), new Color(0.01f, 0.015f, 0.03f, 0.82f));

            GameObject panel = new GameObject("SoulKnightSettlementPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(settlementPanel.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 560f);
            panelRect.anchoredPosition = Vector2.zero;
            RuntimeUIVisuals.StyleImage(panel.GetComponent<Image>(), new Color(0.035f, 0.045f, 0.065f, 0.98f));

            Color frameColor = victory
                ? new Color(0.82f, 0.62f, 0.22f, 0.9f)
                : new Color(0.78f, 0.34f, 0.28f, 0.88f);
            RuntimeUIVisuals.AddFrame(panel.transform, "SettlementFrame", frameColor, 0.01f);

            Color titleColor = victory ? new Color(1f, 0.9f, 0.55f, 1f) : new Color(1f, 0.58f, 0.48f, 1f);
            string titleText = victory ? "通关成功！" : "冒险失败";
            string subtitleText = subtitleOverride ?? (victory
                ? "魔法石已夺回，本次冒险圆满收官。"
                : "本次冒险到此为止，带回的收获仍会保留。");

            RuntimeUIVisuals.CreateBlock(panel.transform, "HeaderBar", new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.96f),
                victory ? new Color(0.16f, 0.12f, 0.04f, 0.95f) : new Color(0.14f, 0.05f, 0.05f, 0.95f));

            Text title = CreateText(panel.transform, "Title", titleText, 52, FontStyle.Bold, titleColor);
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.845f);
            title.rectTransform.anchorMax = new Vector2(0.94f, 0.955f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            Outline titleOutline = title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Text subtitle = CreateText(panel.transform, "Subtitle", subtitleText, 18, FontStyle.Normal, new Color(0.82f, 0.86f, 0.94f, 0.92f));
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.rectTransform.anchorMin = new Vector2(0.08f, 0.76f);
            subtitle.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;

            GameObject statsCard = new GameObject("StatsCard", typeof(RectTransform), typeof(Image));
            statsCard.transform.SetParent(panel.transform, false);
            RectTransform statsRect = statsCard.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.08f, 0.24f);
            statsRect.anchorMax = new Vector2(0.92f, 0.74f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            RuntimeUIVisuals.StyleImage(statsCard.GetComponent<Image>(), new Color(0.02f, 0.025f, 0.04f, 0.92f));
            RuntimeUIVisuals.AddFrame(statsCard.transform, "StatsFrame", new Color(0.58f, 0.66f, 0.82f, 0.42f), 0.012f);

            Text statsHeader = CreateText(statsCard.transform, "StatsHeader", "—— 本次冒险记录 ——", 20, FontStyle.Bold, new Color(0.92f, 0.78f, 0.42f, 1f));
            statsHeader.alignment = TextAnchor.MiddleCenter;
            statsHeader.rectTransform.anchorMin = new Vector2(0.05f, 0.84f);
            statsHeader.rectTransform.anchorMax = new Vector2(0.95f, 0.96f);
            statsHeader.rectTransform.offsetMin = Vector2.zero;
            statsHeader.rectTransform.offsetMax = Vector2.zero;

            string stageLabel = stage > 0 ? "1-" + stage : "—";
            if (victory)
            {
                CreateSettlementStatRow(statsCard.transform, 0.66f, "获得金币", gold.ToString(), new Color(1f, 0.86f, 0.34f, 1f));
                CreateSettlementStatRow(statsCard.transform, 0.50f, "击杀怪物", kills.ToString(), Color.white);
                CreateSettlementStatRow(statsCard.transform, 0.34f, "角色等级", "Lv." + level, Color.white);
                CreateSettlementStatRow(statsCard.transform, 0.18f, "通关层数", stageLabel, Color.white);
            }
            else
            {
                CreateSettlementStatRow(statsCard.transform, 0.66f, "到达层数", stageLabel, Color.white);
                CreateSettlementStatRow(statsCard.transform, 0.50f, "击杀怪物", kills.ToString(), Color.white);
                CreateSettlementStatRow(statsCard.transform, 0.34f, "角色等级", "Lv." + level, Color.white);
                CreateSettlementStatRow(statsCard.transform, 0.18f, "获得金币", gold.ToString(), new Color(1f, 0.86f, 0.34f, 1f));
            }

            GameObject buttonObj = new GameObject("ReturnLobbyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.28f, 0.06f);
            buttonRect.anchorMax = new Vector2(0.72f, 0.16f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            RuntimeUIVisuals.StyleAccent(buttonObj.GetComponent<Image>(),
                victory ? new Color(0.72f, 0.52f, 0.18f, 1f) : new Color(0.18f, 0.34f, 0.58f, 1f));
            RuntimeUIVisuals.AddFrame(buttonObj.transform, "ButtonFrame", new Color(1f, 0.92f, 0.68f, 0.75f), 0.05f);
            Text buttonLabel = CreateText(buttonObj.transform, "Text",
                confirmLabel ?? (victory ? "返回大厅" : "返回基地"), 26, FontStyle.Bold, Color.white);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                HideSettlement();
                Time.timeScale = 1f;
                if (confirmAction != null)
                {
                    confirmAction.Invoke();
                }
                else
                {
                    LoadLobby();
                }
            });
        }

        private void CreateSettlementStatRow(Transform parent, float anchorY, string label, string value, Color valueColor)
        {
            GameObject row = new GameObject("Stat_" + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.06f, anchorY - 0.12f);
            rowRect.anchorMax = new Vector2(0.94f, anchorY);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            RuntimeUIVisuals.CreateBlock(row.transform, "Divider", new Vector2(0f, 0f), new Vector2(1f, 0.04f), new Color(0.72f, 0.52f, 0.18f, 0.22f));

            Text labelText = CreateText(row.transform, "Label", label, 22, FontStyle.Normal, new Color(0.78f, 0.82f, 0.9f, 0.95f));
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.rectTransform.anchorMin = new Vector2(0.02f, 0.08f);
            labelText.rectTransform.anchorMax = new Vector2(0.48f, 0.96f);
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;

            Text valueText = CreateText(row.transform, "Value", value, 26, FontStyle.Bold, valueColor);
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.rectTransform.anchorMin = new Vector2(0.52f, 0.08f);
            valueText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
            valueText.rectTransform.offsetMin = Vector2.zero;
            valueText.rectTransform.offsetMax = Vector2.zero;
        }

        public void QueuePlayerPlacement(Vector2 spawnPoint)
        {
            pendingSpawn = spawnPoint;
            pendingPlayerPlacement = true;
        }

        private void TryPlacePlayer()
        {
            if (!pendingPlayerPlacement)
            {
                return;
            }

            CachePlayer();
            if (player == null)
            {
                return;
            }

            player.position = new Vector3(pendingSpawn.x, pendingSpawn.y, 0f);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = pendingSpawn;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            SetupCamera(player);
            pendingPlayerPlacement = false;
        }

        private void SetupCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.orthographic = true;
            bool isLobby = currentStage == 0;
            Vector3 cameraOffset = isLobby ? new Vector3(0f, 2.1f, 0f) : Vector3.zero;
            camera.orthographicSize = isLobby ? 15.6f : 14.2f;
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(target.position.x + cameraOffset.x, target.position.y + cameraOffset.y, -10f);

            LoopCameraFollow follow = camera.GetComponent<LoopCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<LoopCameraFollow>();
            }

            follow.Configure(target, cameraOffset);
        }

        private void CleanupCurrent()
        {
            DestroyStageRoot(currentRoot);
            currentRoot = null;
        }

        private static void DestroyStageRoot(GameObject root)
        {
            Art2DUtility.CleanupTransientCombatVfx();
            Art2DUtility.CleanupLegacyShopPlaceholderSprites();
            if (root != null)
            {
                Destroy(root);
            }

            GameObject bossBar = GameObject.Find("BossTopHealthBar");
            if (bossBar != null)
            {
                Destroy(bossBar);
            }

            BossTopHealthBar2D.DestroySharedBar();
        }

        private void CachePlayer()
        {
            if (player != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        private void EnsureHud()
        {
            EnsureModernUiManager();
            HideTopHud();

            if (canvas == null)
            {
                GameObject existing = GameObject.Find("SoulKnightOverlayCanvas");
                if (existing != null)
                {
                    canvas = existing.GetComponent<Canvas>();
                }
                else
                {
                    GameObject canvasObject = new GameObject("SoulKnightOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasObject.GetComponent<Canvas>();
                    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1280f, 720f);
                }

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                canvas.worldCamera = null;
            }

            if (combatInfoText == null)
            {
                GameObject combat = new GameObject("SoulKnightCombatPanel", typeof(RectTransform), typeof(Image));
                combat.transform.SetParent(canvas.transform, false);
                RectTransform rect = combat.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(300f, 136f);
                rect.anchoredPosition = new Vector2(18f, 18f);
                Image combatImage = combat.GetComponent<Image>();
                RuntimeUIVisuals.StyleImage(combatImage, new Color(0.025f, 0.03f, 0.04f, 0.88f));

                hpFill = CreateHudBar(combat.transform, "生命", new Vector2(0.07f, 0.66f), new Vector2(0.93f, 0.86f), new Color(0.92f, 0.12f, 0.18f, 1f), out hpLabel);
                manaFill = CreateHudBar(combat.transform, "法力", new Vector2(0.07f, 0.42f), new Vector2(0.93f, 0.62f), new Color(0.16f, 0.48f, 1f, 1f), out manaLabel);
                xpFill = CreateHudBar(combat.transform, "经验", new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.38f), new Color(0.88f, 0.68f, 0.18f, 1f), out xpLabel);

                combatInfoText = CreateText(combat.transform, "StatsFooter", "", 14, FontStyle.Bold, new Color(0.9f, 0.94f, 1f, 1f));
                combatInfoText.rectTransform.anchorMin = new Vector2(0.07f, 0.01f);
                combatInfoText.rectTransform.anchorMax = new Vector2(0.93f, 0.16f);
                combatInfoText.rectTransform.offsetMin = Vector2.zero;
                combatInfoText.rectTransform.offsetMax = Vector2.zero;
                combatInfoText.alignment = TextAnchor.MiddleLeft;
                combatInfoText.supportRichText = true;
            }

            if (pickupTipText == null)
            {
                pickupTipPanel = new GameObject("PickupTipPanel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
                pickupTipPanel.transform.SetParent(canvas.transform, false);
                Canvas promptCanvas = pickupTipPanel.GetComponent<Canvas>();
                promptCanvas.overrideSorting = true;
                promptCanvas.sortingOrder = 200;
                RectTransform panelRect = pickupTipPanel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0f);
                panelRect.anchorMax = new Vector2(0.5f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.sizeDelta = new Vector2(360f, 38f);
                panelRect.anchoredPosition = new Vector2(0f, 34f);
                Image panelImage = pickupTipPanel.GetComponent<Image>();
                RuntimeUIVisuals.StyleImage(panelImage, new Color(0.02f, 0.025f, 0.035f, 0.88f));
                pickupTipPanel.SetActive(false);

                GameObject pickup = new GameObject("PickupTipText", typeof(RectTransform), typeof(Text));
                pickup.transform.SetParent(pickupTipPanel.transform, false);
                RectTransform rect = pickup.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(14f, 4f);
                rect.offsetMax = new Vector2(-14f, -4f);
                pickupTipText = pickup.GetComponent<Text>();
                pickupTipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                pickupTipText.fontSize = 16;
                pickupTipText.fontStyle = FontStyle.Bold;
                pickupTipText.alignment = TextAnchor.MiddleCenter;
                pickupTipText.color = new Color(1f, 0.92f, 0.56f, 1f);
                pickupTipText.enabled = false;
            }
        }

        private static void EnsureModernUiManager()
        {
            if (FindObjectOfType<UIManager>() != null)
            {
                return;
            }

            new GameObject("ModernUIManager").AddComponent<UIManager>();
        }

        private static void HideTopHud()
        {
            GameObject topHud = GameObject.Find("SoulKnightHud");
            if (topHud == null)
            {
                return;
            }

            Graphic[] graphics = topHud.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].enabled = false;
                }
            }
        }

        private void RefreshHud(string stageName)
        {
            EnsureHud();
        }

        private void RefreshCombatInfo()
        {
            EnsureHud();
            if (combatInfoText == null)
            {
                return;
            }

            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            PlayerStatBlock stats = inventory.PlayerStats;
            int maxHp = stats.TotalMaxHp(inventory.GetEquippedArmor());
            float hpRatio = maxHp > 0 ? stats.currentHp / (float)maxHp : 0f;
            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, hpRatio, Time.unscaledDeltaTime * 12f);
            }

            if (hpLabel != null)
            {
                hpLabel.text = "生命  " + stats.currentHp + " / " + maxHp;
            }

            RunProgressionSystem run = RunProgressionSystem.Instance;
            if (run != null && run.CurrentClass == HeroClass.Mage)
            {
                float manaRatio = run.MaxMana > 0f ? run.Mana / run.MaxMana : 0f;
                if (manaFill != null)
                {
                    manaFill.fillAmount = Mathf.Lerp(manaFill.fillAmount, manaRatio, Time.unscaledDeltaTime * 12f);
                    manaFill.color = new Color(0.16f, 0.48f, 1f, 1f);
                }

                if (manaLabel != null)
                {
                    manaLabel.text = "法力  " + Mathf.RoundToInt(run.Mana) + " / " + Mathf.RoundToInt(run.MaxMana);
                }
            }
            else
            {
                if (manaFill != null)
                {
                    if (run != null && run.HasArmorShield)
                    {
                        float shieldRatio = run.ArmorShieldMax > 0 ? run.ArmorShieldCurrent / run.ArmorShieldMax : 0f;
                        manaFill.fillAmount = Mathf.Lerp(manaFill.fillAmount, shieldRatio, Time.unscaledDeltaTime * 12f);
                        manaFill.color = new Color(0.42f, 0.78f, 1f, 1f);
                    }
                    else
                    {
                        manaFill.fillAmount = run != null && run.IsShielding ? 1f : 0.28f;
                        manaFill.color = run != null && run.CurrentClass == HeroClass.Warrior ? new Color(0.82f, 0.82f, 0.88f, 1f) : new Color(0.18f, 0.72f, 0.42f, 1f);
                    }
                }

                if (manaLabel != null)
                {
                    if (run != null && run.HasArmorShield)
                    {
                        float regenLeft = run.GetArmorShieldRegenRemaining();
                        manaLabel.text = regenLeft > 0.05f
                            ? "护盾  " + Mathf.CeilToInt(run.ArmorShieldCurrent) + " / " + run.ArmorShieldMax + "  (" + regenLeft.ToString("0.0") + "秒后回复)"
                            : "护盾  " + Mathf.CeilToInt(run.ArmorShieldCurrent) + " / " + run.ArmorShieldMax;
                    }
                    else
                    {
                        string label = run != null && run.CurrentClass == HeroClass.Warrior ? "防御  盾牌就绪" : "机动  闪避强化";
                        manaLabel.text = label;
                    }
                }
            }

            int requiredExp = 28 + stats.level * 14;
            float expRatio = Mathf.Clamp01(stats.experience / (float)Mathf.Max(1, requiredExp));
            if (xpFill != null)
            {
                xpFill.fillAmount = Mathf.Lerp(xpFill.fillAmount, expRatio, Time.unscaledDeltaTime * 12f);
            }

            if (xpLabel != null)
            {
                xpLabel.text = "等级 " + stats.level + "    经验 " + stats.experience + " / " + requiredExp;
            }

            if (topXpFill != null)
            {
                topXpFill.fillAmount = Mathf.Lerp(topXpFill.fillAmount, expRatio, Time.unscaledDeltaTime * 12f);
            }

            if (topXpLabel != null)
            {
                topXpLabel.text = "局内经验  " + stats.experience + " / " + requiredExp;
            }

            combatInfoText.text = "攻击 " + stats.TotalAtk(inventory.GetEquippedWeapon()) + "    防御 " + stats.TotalDef(inventory.GetEquippedArmor());
            if (pickupTipText != null && pickupTipText.enabled && Time.time > pickupTipUntil)
            {
                pickupTipText.enabled = false;
                if (pickupTipPanel != null)
                {
                    pickupTipPanel.SetActive(false);
                }
            }
        }

        private void HideSettlement()
        {
            if (settlementPanel != null)
            {
                Destroy(settlementPanel);
            }
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private Image CreateHudBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color fillColor, out Text label)
        {
            GameObject root = new GameObject(name + "条", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image backImage = root.GetComponent<Image>();
            RuntimeUIVisuals.StyleImage(backImage, new Color(0.06f, 0.07f, 0.085f, 0.92f));

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = RuntimeUIVisuals.SolidSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = fillColor;

            label = CreateText(root.transform, name + "Label", "", 14, FontStyle.Bold, Color.white);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            label.alignment = TextAnchor.MiddleLeft;
            return fill;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.15f, 0.42f, 0.95f, 1f);
            Button button = obj.GetComponent<Button>();
            Text text = CreateText(obj.transform, "Text", label, 22, FontStyle.Bold, Color.white);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
