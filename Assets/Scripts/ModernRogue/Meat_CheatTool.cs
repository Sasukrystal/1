using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class Meat_CheatTool : MonoBehaviour
    {
        private GameObject panelRoot;
        private Text liveInfoText;
        private Text statusText;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                ToggleCheatPanel();
            }

            if (panelRoot != null && panelRoot.activeSelf)
            {
                RefreshLiveInfo();
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                SpawnGoldAndGodGear();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                DungeonLoopDirector.Instance?.DebugStartCurrentChallenge();
                Debug.Log("Meat_CheatTool: I activated, current room challenge started.");
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                SoulKnightDirector.Instance?.StartRun();
                Debug.Log("Meat_CheatTool: U activated, SoulKnight run started at 1-1.");
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                DungeonLoopDirector.Instance?.DebugCompleteCurrentRoom();
                Debug.Log("Meat_CheatTool: L activated, current room completed for testing.");
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.LoadNextStage();
                }
                else
                {
                    DungeonLoopDirector.Instance?.GoToNextRoom();
                }

                Debug.Log("Meat_CheatTool: K activated, advanced to next stage.");
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    LoadBossStage(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    LoadBossStage(2);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    LoadBossStage(3);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    LoadBossStage(4);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    LoadBossStage(5);
                }
            }
        }

        private void ToggleCheatPanel()
        {
            EnsurePanel();
            if (panelRoot == null)
            {
                return;
            }

            bool visible = !panelRoot.activeSelf;
            panelRoot.SetActive(visible);
        }

        private void EnsurePanel()
        {
            if (panelRoot != null)
            {
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject modernCanvas = GameObject.Find("ModernRogueUICanvas");
            if (modernCanvas != null)
            {
                canvas = modernCanvas.GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            Transform old = canvas.transform.Find("MeatCheatPanel");
            if (old != null)
            {
                if (old.Find("ButtonScrollArea") == null)
                {
                    Destroy(old.gameObject);
                }
                else
                {
                    panelRoot = old.gameObject;
                    liveInfoText = panelRoot.transform.Find("LiveInfoText")?.GetComponent<Text>();
                    statusText = panelRoot.transform.Find("StatusText")?.GetComponent<Text>();
                    return;
                }
            }

            panelRoot = new GameObject("MeatCheatPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.sizeDelta = new Vector2(432f, 720f);
            rootRect.anchoredPosition = new Vector2(-18f, 0f);

            Image rootImage = panelRoot.GetComponent<Image>();
            RuntimeUIVisuals.StyleChrome(rootImage, new Color(0.16f, 0.17f, 0.19f, 0.98f));
            RuntimeUIVisuals.AddFrame(panelRoot.transform, "CheatFrame", new Color(0.7f, 0.42f, 0.18f, 0.95f), 0.012f);
            RuntimeUIVisuals.CreateBlock(panelRoot.transform, "HeaderBar", new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.985f), new Color(0.09f, 0.06f, 0.04f, 0.98f));

            Text title = CreateText(panelRoot.transform, "Title", "测试作弊面板", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.925f), new Vector2(0.72f, 0.985f));

            Text closeHint = CreateText(panelRoot.transform, "CloseHint", "P 关闭", 14, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(closeHint.rectTransform, new Vector2(0.72f, 0.925f), new Vector2(0.94f, 0.985f));

            RuntimeUIVisuals.CreateBlock(panelRoot.transform, "InfoBack", new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.9f), new Color(0.03f, 0.04f, 0.055f, 0.82f));
            liveInfoText = CreateText(panelRoot.transform, "LiveInfoText", "", 15, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(liveInfoText.rectTransform, new Vector2(0.08f, 0.795f), new Vector2(0.92f, 0.89f));

            statusText = CreateText(panelRoot.transform, "StatusText", "P 打开面板；Shift+1~5 挑战各层 Boss；O/I/U/L/K 仍可用。", 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(statusText.rectTransform, new Vector2(0.08f, 0.755f), new Vector2(0.92f, 0.785f));
            statusText.raycastTarget = false;
            statusText.color = new Color(0.82f, 0.86f, 0.92f, 0.95f);

            GameObject scrollArea = new GameObject("ButtonScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollArea.transform.SetParent(panelRoot.transform, false);
            SetRect(scrollArea.GetComponent<RectTransform>(), new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.75f));
            Image scrollBg = scrollArea.GetComponent<Image>();
            scrollBg.color = new Color(0.03f, 0.04f, 0.055f, 0.78f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollArea.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject buttonList = new GameObject("ButtonList", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            buttonList.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = buttonList.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            ContentSizeFitter fitter = buttonList.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = scrollArea.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            GridLayoutGroup layout = buttonList.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(148f, 34f);
            layout.spacing = new Vector2(10f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            CreateActionButton(buttonList.transform, "开始 Run", () =>
            {
                SoulKnightDirector.Instance?.StartRun();
                SetStatus("已开始 SoulKnight run。");
            });
            CreateActionButton(buttonList.transform, "返回 Lobby", () =>
            {
                SoulKnightDirector.Instance?.LoadLobby();
                SetStatus("已返回大厅。");
            });
            CreateActionButton(buttonList.transform, "下一层 / 下一房", () =>
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.LoadNextStage();
                    SetStatus("已切到下一层。");
                }
                else
                {
                    DungeonLoopDirector.Instance?.GoToNextRoom();
                    SetStatus("已切到下一房。");
                }
            });
            CreateActionButton(buttonList.transform, "当前房开战", () =>
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.DebugStartCurrentRoom();
                    SetStatus("已触发当前 SoulKnight 房间战斗。");
                }
                else
                {
                    DungeonLoopDirector.Instance?.DebugStartCurrentChallenge();
                    SetStatus("已触发当前房挑战。");
                }
            });
            CreateActionButton(buttonList.transform, "当前房结算", () =>
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.DebugCompleteCurrentRoom();
                    SetStatus("已完成当前 SoulKnight 房间。");
                }
                else
                {
                    DungeonLoopDirector.Instance?.DebugCompleteCurrentRoom();
                    SetStatus("已完成当前房。");
                }
            });
            CreateActionButton(buttonList.transform, "攻击 +999", () =>
            {
                GiveOneShotAttack();
                SetStatus("已给玩家攻击 +999。");
            });
            CreateActionButton(buttonList.transform, "发 500 金币", () =>
            {
                if (NewInventorySystem.Instance != null)
                {
                    NewInventorySystem.Instance.EarnCoins(500);
                }

                SetStatus("已发放 500 金币。");
            });
            CreateActionButton(buttonList.transform, "发测试礼包", () =>
            {
                SpawnGoldAndGodGear();
                SetStatus("已发放测试礼包。");
            });
            CreateActionButton(buttonList.transform, "强制升级", () =>
            {
                RunProgressionSystem.Instance?.ForceLevelUp();
                SetStatus("已强制升 1 级。");
            });
            CreateActionButton(buttonList.transform, "宝物 +1", () =>
            {
                RunProgressionSystem.Instance?.AddRandomTreasure();
                SetStatus("已添加 1 个随机宝物。");
            });
            CreateActionButton(buttonList.transform, "战士", () =>
            {
                RunProgressionSystem.Instance?.SelectClass(HeroClass.Warrior);
                SetStatus("已切换为战士。");
            });
            CreateActionButton(buttonList.transform, "弓手", () =>
            {
                RunProgressionSystem.Instance?.SelectClass(HeroClass.Archer);
                SetStatus("已切换为弓手。");
            });
            CreateActionButton(buttonList.transform, "法师", () =>
            {
                RunProgressionSystem.Instance?.SelectClass(HeroClass.Mage);
                SetStatus("已切换为法师。");
            });
            CreateActionButton(buttonList.transform, "风核 x3", () =>
            {
                GiveCore(101, "风核", 3);
            });
            CreateActionButton(buttonList.transform, "火核 x3", () =>
            {
                GiveCore(102, "火核", 3);
            });
            CreateActionButton(buttonList.transform, "水核 x3", () =>
            {
                GiveCore(103, "水核", 3);
            });
            CreateActionButton(buttonList.transform, "雷核 x3", () =>
            {
                GiveCore(104, "雷核", 3);
            });
            CreateActionButton(buttonList.transform, "金核 x3", () =>
            {
                GiveCore(105, "金核", 3);
            });
            CreateActionButton(buttonList.transform, "当前层商人", () =>
            {
                if (SoulKnightDirector.Instance != null)
                {
                    SoulKnightDirector.Instance.DebugEnterCurrentStageShop();
                    SetStatus("已传送到当前层商人并打开商店。");
                }
                else
                {
                    SetStatus("Director 未就绪，请先开始 Run。");
                }
            });
            CreateActionButton(buttonList.transform, "Boss 战 (1层)", () =>
            {
                LoadBossStage(1);
            });
            CreateActionButton(buttonList.transform, "Boss 战 (2层)", () =>
            {
                LoadBossStage(2);
            });
            CreateActionButton(buttonList.transform, "Boss 战 (3层)", () =>
            {
                LoadBossStage(3);
            });
            CreateActionButton(buttonList.transform, "Boss 战 (4层)", () =>
            {
                LoadBossStage(4);
            });
            CreateActionButton(buttonList.transform, "Boss 战 (5层)", () =>
            {
                LoadBossStage(5);
            });

            panelRoot.SetActive(false);
        }

        private void CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), new Color(0.42f, 0.3f, 0.18f, 0.96f));
            RuntimeUIVisuals.AddFrame(obj.transform, "ActionFrame", new Color(0.78f, 0.6f, 0.22f, 0.92f), 0.03f);
            LayoutElement element = obj.GetComponent<LayoutElement>();
            element.preferredHeight = 34f;

            Text text = CreateText(obj.transform, "Label", label, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f));

            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log("Meat_CheatTool: " + message);
        }

        private void RefreshLiveInfo()
        {
            if (liveInfoText == null)
            {
                return;
            }

            SoulKnightDirector director = SoulKnightDirector.Instance;
            RunProgressionSystem run = RunProgressionSystem.Instance;
            string stageText = director != null
                ? (director.CurrentStage <= 0 ? "Lobby" : "1-" + director.CurrentStage)
                : (DungeonLoopDirector.Instance != null ? "Loop-" + DungeonLoopDirector.Instance.CurrentStage : "未知");
            int gold = director != null ? director.RunGold : (NewInventorySystem.Instance != null ? NewInventorySystem.Instance.PlayerStats.coins : 0);
            string classText = run != null ? RunProgressionSystem.GetClassName(run.CurrentClass) : "未知";
            string runState = run != null && run.IsRunActive ? "进行中" : "未开始";

            liveInfoText.text =
                "关卡: " + stageText + "\n" +
                "金币: " + gold + "\n" +
                "职业: " + classText + "\n" +
                "Run: " + runState;
        }

        private void GiveOneShotAttack()
        {
            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.AddBaseAttack(999);
            }

            Bagsys.RogueLike.PlayerStats playerStats = FindObjectOfType<Bagsys.RogueLike.PlayerStats>();
            if (playerStats != null)
            {
                playerStats.ApplyEquipmentBonuses(999, 0, 0, 0, 999);
            }

            Debug.Log("Meat_CheatTool: player attack boosted by +999.");
        }

        private void GiveCore(int templateId, string displayName, int count)
        {
            if (NewInventorySystem.Instance == null)
            {
                SetStatus("背包系统未就绪。");
                return;
            }

            CoreQuality quality = CoreQuality.Rare;
            for (int i = 0; i < count; i++)
            {
                NewInventorySystem.Instance.AddCore(templateId, quality);
            }

            SetStatus("已添加 " + count + " 个蓝色" + displayName + "。");
        }

        private void LoadBossStage(int stage)
        {
            if (SoulKnightDirector.Instance == null)
            {
                SetStatus("Director 未就绪，请先开始 Run。");
                return;
            }

            SoulKnightDirector.Instance.LoadBossDebugStage(stage);
            SetStatus("正在加载第 " + stage + " 层 Boss 战…" + (stage >= 5 ? "（终局双 Boss）" : string.Empty));
            Debug.Log("Meat_CheatTool: Shift+" + stage + " activated, loading boss stage " + stage + ".");
        }

        private void SpawnGoldAndGodGear()
        {
            if (NewInventorySystem.Instance != null)
            {
                NewInventorySystem.Instance.EarnCoins(500);
                NewInventorySystem.Instance.AddItem(2, 3);
                NewInventorySystem.Instance.AddItem(RunProgressionSystem.Instance != null ? RunProgressionSystem.Instance.GetClassArmorDropId() : 5, 1);
                NewInventorySystem.Instance.AddCore(101, CoreQuality.Rare);
                NewInventorySystem.Instance.AddCore(102, CoreQuality.Rare);
                if (RunProgressionSystem.Instance != null)
                {
                    RunProgressionSystem.Instance.AddRandomTreasure();
                }
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                GameObject coin = new GameObject("CheatGoldVisual", typeof(SpriteRenderer));
                coin.name = "CheatGoldVisual";
                coin.transform.position = origin + new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(-1.2f, 1.2f), 0f);
                coin.transform.localScale = Vector3.one * 0.22f;
                SpriteRenderer renderer = coin.GetComponent<SpriteRenderer>();
                renderer.sprite = Art2DUtility.GetCircleSprite();
                renderer.color = new Color(1f, 0.78f, 0.16f);
                renderer.sortingOrder = 20;
                Destroy(coin, 4f);
            }

            Debug.Log("Meat_CheatTool: O activated, granted 500 gold, class armor, cores and treasure.");
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
