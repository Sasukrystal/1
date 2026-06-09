using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public class RoomTrigger2D : MonoBehaviour
    {
        private SoulKnightDirector director;
        private int stage;
        private bool finalRoom;
        private bool started;
        private bool cleared;
        private int waveIndex;
        private float nextWaveTime;
        private int playerHpAtStart;
        private SoulKnightStageProfile stageProfile;
        private RoomRewardType rewardType = RoomRewardType.Equipment;
        private readonly List<Bagsys.RogueLike.EnemyStats> liveEnemies = new List<Bagsys.RogueLike.EnemyStats>();
        private readonly List<GameObject> lockWalls = new List<GameObject>();
        private Coroutine spawnRoutine;
        private const float HalfRoomWidth = 15f;
        private const float HalfRoomHeight = 10f;
        private const float DoorOpening = 7.6f;

        public void Initialize(SoulKnightDirector owner, int stageIndex, bool isFinalRoom)
        {
            Initialize(owner, stageIndex, isFinalRoom, SoulKnightStageProfiles.Resolve(stageIndex));
        }

        public void Initialize(SoulKnightDirector owner, int stageIndex, bool isFinalRoom, SoulKnightStageProfile profile)
        {
            director = owner;
            stage = stageIndex;
            finalRoom = isFinalRoom;
            stageProfile = profile ?? SoulKnightStageProfiles.Resolve(stageIndex);
            BuildLockWalls();
            SetLocks(false);
        }

        public void SetRewardType(RoomRewardType type)
        {
            rewardType = type;
        }

        public bool IsBossRoom => finalRoom;

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            Vector2 center = transform.position;
            return Mathf.Abs(worldPoint.x - center.x) <= HalfRoomWidth - 0.8f
                && Mathf.Abs(worldPoint.y - center.y) <= HalfRoomHeight - 0.8f;
        }

        public void CheatStartEncounter()
        {
            if (started || cleared)
            {
                return;
            }

            StartEncounter();
        }

        public void CheatForceClear()
        {
            if (cleared)
            {
                return;
            }

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            started = true;
            waveIndex = GetTargetWaveCount();
            for (int i = liveEnemies.Count - 1; i >= 0; i--)
            {
                if (liveEnemies[i] != null)
                {
                    Destroy(liveEnemies[i].gameObject);
                }
            }

            liveEnemies.Clear();
            ClearRoom();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (started || cleared || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (!IsPlayerDeepInsideRoom(other))
            {
                return;
            }

            StartEncounter();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (started || cleared || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (!IsPlayerDeepInsideRoom(other))
            {
                return;
            }

            StartEncounter();
        }

        private bool IsPlayerDeepInsideRoom(Collider2D playerCollider)
        {
            if (playerCollider == null)
            {
                return false;
            }

            Vector2 delta = (Vector2)playerCollider.transform.position - (Vector2)transform.position;
            const float edgeMargin = 3.75f;
            return Mathf.Abs(delta.x) <= HalfRoomWidth - edgeMargin
                && Mathf.Abs(delta.y) <= HalfRoomHeight - edgeMargin;
        }

        private void Update()
        {
            if (!started || cleared)
            {
                return;
            }

            for (int i = liveEnemies.Count - 1; i >= 0; i--)
            {
                if (liveEnemies[i] == null || !liveEnemies[i].gameObject.activeInHierarchy)
                {
                    liveEnemies.RemoveAt(i);
                }
            }

            if (liveEnemies.Count == 0)
            {
                if (waveIndex < GetTargetWaveCount())
                {
                    if (Time.time >= nextWaveTime)
                    {
                        QueueSpawnWave();
                    }
                }
                else
                {
                    ClearRoom();
                }
            }
        }

        private void StartEncounter()
        {
            started = true;
            playerHpAtStart = NewInventorySystem.Instance != null ? NewInventorySystem.Instance.PlayerStats.currentHp : 0;
            SetLocks(true);
            SpawnWarningText(finalRoom ? "!! Boss 战开始 !!" : "! 遭遇怪物 !");
            if (finalRoom)
            {
                GameAudioService.Ensure()?.SetBossBattleActive(true);
            }

            waveIndex = 0;
            nextWaveTime = Time.time;
            QueueSpawnWave();
        }

        private void QueueSpawnWave()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            spawnRoutine = StartCoroutine(SpawnWaveRoutine());
        }

        private IEnumerator SpawnWaveRoutine()
        {
            waveIndex++;
            if (finalRoom)
            {
                Vector2 center = transform.localPosition;
                if (stage >= 5)
                {
                    PickRandomBossPair(out SoulKnightBossArchetype firstArchetype, out SoulKnightBossArchetype secondArchetype);
                    SpawnBossAt(center + new Vector2(-3.2f, 0f), firstArchetype);
                    SpawnBossAt(center + new Vector2(3.2f, 0f), secondArchetype);
                    SpawnWarningText("!! 双 Boss 降临 !!");
                    if (director != null)
                    {
                        director.ShowToast("第五层终局：双 Boss 同时登场（从四名 Boss 中随机组合）！");
                    }
                }
                else
                {
                    SpawnBossAt(center, stageProfile.BossArchetype);
                }

                waveIndex = GetTargetWaveCount();
                nextWaveTime = Time.time + 0.85f;
                spawnRoutine = null;
                yield break;
            }

            int count = Random.Range(stageProfile.EnemyCountMin, stageProfile.EnemyCountMax + 1);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2.6f;
                offset.y = Mathf.Clamp(offset.y, -2.2f, 2.2f);
                bool ranged = Random.value < stageProfile.RangedEnemyChance;
                Bagsys.RogueLike.EnemyStats enemy = SoulKnightEnemyFactory.SpawnEnemy(transform.parent, transform.localPosition + (Vector3)offset, ranged, stage, stageProfile);
                CombatRoomBounds2D bounds = enemy.gameObject.AddComponent<CombatRoomBounds2D>();
                bounds.Initialize(transform.localPosition, new Vector2(HalfRoomWidth - 1.1f, HalfRoomHeight - 1.1f));
                liveEnemies.Add(enemy);
                if (i < count - 1)
                {
                    yield return null;
                }
            }

            nextWaveTime = Time.time + 0.85f;
            spawnRoutine = null;
        }

        private void SpawnBossAt(Vector2 localPosition, SoulKnightBossArchetype archetype)
        {
            Bagsys.RogueLike.EnemyStats boss = SoulKnightEnemyFactory.SpawnBoss(
                transform.parent,
                localPosition,
                stage,
                stageProfile,
                archetype);
            CombatRoomBounds2D bounds = boss.gameObject.AddComponent<CombatRoomBounds2D>();
            bounds.Initialize(transform.localPosition, new Vector2(HalfRoomWidth - 1.4f, HalfRoomHeight - 1.4f));
            liveEnemies.Add(boss);
        }

        private static void PickRandomBossPair(out SoulKnightBossArchetype first, out SoulKnightBossArchetype second)
        {
            SoulKnightBossArchetype[] pool =
            {
                SoulKnightBossArchetype.Titan,
                SoulKnightBossArchetype.StormGuard,
                SoulKnightBossArchetype.BroodQueen,
                SoulKnightBossArchetype.EmberMage
            };
            int firstIndex = Random.Range(0, pool.Length);
            int secondIndex = Random.Range(0, pool.Length - 1);
            if (secondIndex >= firstIndex)
            {
                secondIndex++;
            }

            first = pool[firstIndex];
            second = pool[secondIndex];
        }

        private int GetTargetWaveCount()
        {
            if (finalRoom)
            {
                return 1;
            }

            return Mathf.Max(1, stageProfile != null ? stageProfile.WaveCount : 3);
        }

        private void ClearRoom()
        {
            cleared = true;
            DestroyLocks();
            SpawnWarningText("房门已开启");
            if (finalRoom)
            {
                GameAudioService.Ensure()?.SetBossBattleActive(false);
            }

            bool perfect = !finalRoom && NewInventorySystem.Instance != null && NewInventorySystem.Instance.PlayerStats.currentHp >= playerHpAtStart;
            if (ElementalCoreCombatSystem.Instance != null)
            {
                ElementalCoreCombatSystem.Instance.OnRoomCleared(perfect);
            }

            if (perfect && RunProgressionSystem.Instance != null)
            {
                RunProgressionSystem.Instance.OnPerfectRoomCleared();
            }

            if (finalRoom)
            {
                SoulKnightChest bossChest = SoulKnightDungeonBuilder.CreateChest(transform.parent, transform.localPosition + new Vector3(-1.8f, 0f, 0f), SoulKnightChestMode.BossReward);
                bossChest.Initialize(director, SoulKnightChestMode.BossReward);
                SoulKnightPortalMode mode = stage >= 5 ? SoulKnightPortalMode.Victory : SoulKnightPortalMode.NextStage;
                SoulKnightDungeonBuilder.CreateFloorPortal(transform.parent, transform.localPosition + new Vector3(2f, 0f, 0f), director, mode, true);
                return;
            }

            SoulKnightChest chest = SoulKnightDungeonBuilder.CreateChest(transform.parent, transform.localPosition, SoulKnightChestMode.ClearReward);
            chest.Initialize(director, SoulKnightChestMode.ClearReward);
            chest.SetRewardType(rewardType);
        }

        private void BuildLockWalls()
        {
            Vector2 center = transform.localPosition;
            lockWalls.Add(CreateLock("NorthLock", center + new Vector2(0f, HalfRoomHeight), new Vector2(DoorOpening, 0.66f)));
            lockWalls.Add(CreateLock("SouthLock", center + new Vector2(0f, -HalfRoomHeight), new Vector2(DoorOpening, 0.66f)));
            lockWalls.Add(CreateLock("EastLock", center + new Vector2(HalfRoomWidth, 0f), new Vector2(0.66f, DoorOpening)));
            lockWalls.Add(CreateLock("WestLock", center + new Vector2(-HalfRoomWidth, 0f), new Vector2(0.66f, DoorOpening)));
        }

        private GameObject CreateLock(string name, Vector2 pos, Vector2 scale)
        {
            GameObject wall = new GameObject(name, typeof(BoxCollider2D), typeof(SpikeLockWall2D));
            wall.transform.SetParent(transform.parent, false);
            wall.transform.localPosition = pos;
            SpikeLockWall2D spikeWall = wall.GetComponent<SpikeLockWall2D>();
            spikeWall.Configure(scale);
            return wall;
        }

        private void SetLocks(bool active)
        {
            for (int i = 0; i < lockWalls.Count; i++)
            {
                if (lockWalls[i] != null)
                {
                    SpikeLockWall2D spikeWall = lockWalls[i].GetComponent<SpikeLockWall2D>();
                    if (spikeWall != null)
                    {
                        spikeWall.SetClosed(active, false);
                    }
                }
            }
        }

        private void DestroyLocks()
        {
            for (int i = 0; i < lockWalls.Count; i++)
            {
                if (lockWalls[i] != null)
                {
                    SpikeLockWall2D spikeWall = lockWalls[i].GetComponent<SpikeLockWall2D>();
                    if (spikeWall != null)
                    {
                        spikeWall.OpenAndDestroy();
                    }
                    else
                    {
                        Destroy(lockWalls[i]);
                    }
                }
            }

            lockWalls.Clear();
        }

        private void SpawnWarningText(string value)
        {
            GameObject obj = new GameObject("EncounterWarning", typeof(RectTransform), typeof(Canvas));
            obj.transform.SetParent(transform.parent, false);
            obj.transform.localPosition = transform.localPosition + Vector3.up * 2.3f;
            obj.transform.localScale = Vector3.one * 0.02f;
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 40;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 70f);
            Text text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(obj.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.24f, 0.18f);
            text.text = value;
            Destroy(obj, 1.35f);
        }
    }

    public sealed class SpikeLockWall2D : MonoBehaviour
    {
        private BoxCollider2D wallCollider;

        public void Configure(Vector2 scale)
        {
            wallCollider = GetComponent<BoxCollider2D>();
            wallCollider.size = scale;
            wallCollider.enabled = false;

            bool horizontal = scale.x > scale.y;
            Sprite brickDark = Resources.Load<Sprite>("ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_dark_0");
            Sprite brickGray = Resources.Load<Sprite>("ArtIntegration/Environment/DungeonCrawlCombatRoom/brick_gray_0");

            GameObject visuals = new GameObject("LockWallVisual");
            visuals.transform.SetParent(transform, false);

            if (horizontal)
            {
                float[] offsets = { -2.82f, -1.41f, 0f, 1.41f, 2.82f };
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateTile(visuals.transform, new Vector2(offsets[i], 0f), brickDark, brickGray, i);
                }
            }
            else
            {
                float[] offsets = { -2.82f, -1.41f, 0f, 1.41f, 2.82f };
                for (int i = 0; i < offsets.Length; i++)
                {
                    CreateTile(visuals.transform, new Vector2(0f, offsets[i]), brickDark, brickGray, i);
                }
            }

            gameObject.SetActive(false);
        }

        public void SetClosed(bool closed, bool immediate)
        {
            gameObject.SetActive(closed);
            if (wallCollider != null)
            {
                wallCollider.enabled = closed;
            }
        }

        public void OpenAndDestroy()
        {
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        private static void CreateTile(Transform parent, Vector2 pos, Sprite dark, Sprite gray, int index)
        {
            GameObject obj = new GameObject("Tile" + index, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = (index & 1) == 0 ? gray : dark;
            sr.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            sr.sortingOrder = 10;
        }
    }

    public class CombatRoomBounds2D : MonoBehaviour
    {
        private Vector2 center;
        private Vector2 halfSize;

        public void Initialize(Vector2 roomCenter, Vector2 roomHalfSize)
        {
            center = roomCenter;
            halfSize = roomHalfSize;
        }

        public Vector2 Clamp(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, center.x - halfSize.x, center.x + halfSize.x),
                Mathf.Clamp(position.y, center.y - halfSize.y, center.y + halfSize.y));
        }
    }

    public static class SoulKnightEnemyFactory
    {
        public static Bagsys.RogueLike.EnemyStats SpawnEnemy(Transform parent, Vector2 localPosition, bool ranged, int stage)
        {
            return SpawnEnemy(parent, localPosition, ranged, stage, SoulKnightStageProfiles.Resolve(stage));
        }

        public static Bagsys.RogueLike.EnemyStats SpawnEnemy(Transform parent, Vector2 localPosition, bool ranged, int stage, SoulKnightStageProfile profile)
        {
            profile = profile ?? SoulKnightStageProfiles.Resolve(stage);
            bool eliteSlime = !ranged && Random.value < profile.EliteMeleeChance;
            string name = ranged ? "骷髅弓手" : eliteSlime ? "精英史莱姆" : "红色史莱姆";
            string sprite = ranged ? "Enemy_SkeletonArcher" : eliteSlime ? "Enemy_Slime_Elite" : "Enemy_Slime";
            Vector2 visualScale = ranged ? new Vector2(1.52f, 1.52f) : eliteSlime ? new Vector2(1.72f, 1.72f) : new Vector2(1.38f, 1.38f);
            SpriteRenderer renderer = SoulKnightDungeonBuilder.CreateSprite(parent, name, localPosition, visualScale, sprite, Color.white, 4);
            GameObject enemy = renderer.gameObject;
            enemy.tag = "Enemy";

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = eliteSlime ? 0.58f : 0.46f;

            RogueActorVisual2D visual = enemy.AddComponent<RogueActorVisual2D>();
            visual.Configure(ranged ? "SkeletonArcher" : eliteSlime ? "EliteSlime" : "Slime");

            Bagsys.RogueLike.EnemyStats stats = enemy.AddComponent<Bagsys.RogueLike.EnemyStats>();
            stats.ConfigureBaseStats(ranged ? 42 + stage * 8 : eliteSlime ? 70 + stage * 10 : 34 + stage * 7, ranged ? 7 + stage * 2 : eliteSlime ? 9 + stage * 2 : 5 + stage, 0);
            enemy.AddComponent<SKHealthBar2D>();
            if (ranged)
            {
                enemy.AddComponent<SkeletonArcherAI>().Configure(7 + stage * 2, 1.45f + stage * 0.04f);
            }
            else
            {
                enemy.AddComponent<SlimeAI>().Configure(5 + stage, 1.48f + stage * 0.04f);
            }

            return stats;
        }

        public static Bagsys.RogueLike.EnemyStats SpawnBoss(Transform parent, Vector2 localPosition, int stage)
        {
            return SpawnBoss(parent, localPosition, stage, SoulKnightStageProfiles.Resolve(stage));
        }

        public static Bagsys.RogueLike.EnemyStats SpawnBoss(Transform parent, Vector2 localPosition, int stage, SoulKnightStageProfile profile)
        {
            profile = profile ?? SoulKnightStageProfiles.Resolve(stage);
            return SpawnBoss(parent, localPosition, stage, profile, profile.BossArchetype);
        }

        public static Bagsys.RogueLike.EnemyStats SpawnBoss(
            Transform parent,
            Vector2 localPosition,
            int stage,
            SoulKnightStageProfile profile,
            SoulKnightBossArchetype archetype)
        {
            profile = profile ?? SoulKnightStageProfiles.Resolve(stage);
            int bossIndex = ResolveBossIndex(archetype);
            string bossName;
            Color bossColor;
            string bossSprite;
            string bossVisualId;
            switch (bossIndex)
            {
                case 1:
                    bossName = "烬火术士";
                    bossColor = new Color(1f, 0.36f, 0.12f);
                    bossSprite = "Boss_EmberMage";
                    bossVisualId = "EmberMage";
                    break;
                case 2:
                    bossName = "风暴守卫";
                    bossColor = new Color(0.24f, 0.74f, 1f);
                    bossSprite = "Boss_StormGuard";
                    bossVisualId = "StormGuard";
                    break;
                case 3:
                    bossName = "巢穴母虫";
                    bossColor = new Color(0.62f, 0.9f, 0.22f);
                    bossSprite = "Boss_BroodQueen";
                    bossVisualId = "BroodQueen";
                    break;
                default:
                    bossName = "地牢巨像";
                    bossColor = new Color(0.28f, 0.18f, 0.95f);
                    bossSprite = "Boss_Titan";
                    bossVisualId = "Titan";
                    break;
            }

            SpriteRenderer renderer = SoulKnightDungeonBuilder.CreateSprite(parent, bossName, localPosition, new Vector2(3.25f, 3.25f), bossSprite, Color.white, 5);
            GameObject boss = renderer.gameObject;
            boss.tag = "Enemy";

            Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = boss.AddComponent<CircleCollider2D>();
            collider.radius = 0.76f;

            RogueActorVisual2D visual = boss.AddComponent<RogueActorVisual2D>();
            visual.Configure(bossVisualId);

            Bagsys.RogueLike.EnemyStats stats = boss.AddComponent<Bagsys.RogueLike.EnemyStats>();
            stats.ConfigureBaseStats(240 + stage * 90, 16 + stage * 3, 1);
            BossTopHealthBar2D topBar = boss.AddComponent<BossTopHealthBar2D>();
            topBar.Initialize(bossName, bossColor);
            boss.AddComponent<BossEncounterEnhancer2D>().Initialize(bossColor, bossName);
            switch (bossIndex)
            {
                case 1:
                    boss.AddComponent<EmberMageBossAI>().Configure(14 + stage * 3, 1.05f + stage * 0.03f);
                    break;
                case 2:
                    boss.AddComponent<StormGuardBossAI>().Configure(15 + stage * 3, 1.15f + stage * 0.04f);
                    break;
                case 3:
                    boss.AddComponent<BroodQueenBossAI>().Configure(13 + stage * 3, 1.05f + stage * 0.03f);
                    break;
                default:
                    boss.AddComponent<FloorBossAI>().Configure(16 + stage * 3, 1.35f + stage * 0.05f);
                    break;
            }

            return stats;
        }

        private static int ResolveBossIndex(SoulKnightBossArchetype archetype)
        {
            switch (archetype)
            {
                case SoulKnightBossArchetype.EmberMage:
                    return 1;
                case SoulKnightBossArchetype.StormGuard:
                    return 2;
                case SoulKnightBossArchetype.BroodQueen:
                    return 3;
                default:
                    return 0;
            }
        }
    }

    public enum SoulKnightChestMode
    {
        Starter,
        ClearReward,
        BossReward
    }

    public class SoulKnightChest : MonoBehaviour
    {
        private SoulKnightDirector director;
        private SoulKnightChestMode mode;
        private RoomRewardType rewardType = RoomRewardType.Equipment;
        private int hp = 1;

        public void Initialize(SoulKnightDirector owner, SoulKnightChestMode chestMode)
        {
            director = owner;
            mode = chestMode;
        }

        public void SetRewardType(RoomRewardType type)
        {
            rewardType = type;
        }

        public void TakeDamage(int damage)
        {
            hp -= Mathf.Max(1, damage);
            if (hp > 0)
            {
                return;
            }

            Reward();
            Destroy(gameObject);
        }

        private void Reward()
        {
            if (director == null)
            {
                director = SoulKnightDirector.Instance;
            }

            NewInventorySystem inventory = NewInventorySystem.Instance;
            if (inventory == null)
            {
                return;
            }

            if (mode == SoulKnightChestMode.Starter)
            {
                inventory.AddItem(2, 1);
                if (director != null)
                {
                    director.AddRunGold(20);
                    director.ShowToast("出征补给：猩红药剂 +1\n金币 +20\n靠近职业雕像右键切换职业");
                }
                return;
            }

            if (mode == SoulKnightChestMode.BossReward)
            {
                int bossGold = Random.Range(90, 151);
                inventory.EarnCoins(bossGold);
                inventory.AddExperience(45);
                int coreId = Random.Range(101, 106);
                inventory.AddCore(coreId, CoreQuality.Legendary);
                if (RunProgressionSystem.Instance != null)
                {
                    RunProgressionSystem.Instance.GrantBossDefeatRewards();
                    RunProgressionSystem.Instance.AddRandomTreasure();
                }

                CoreData core = GameDataModel.GetCore(coreId);
                if (director != null)
                {
                    director.ShowToast("Boss 奖励：金币 +" + bossGold + "\n金色虫核 + 宝物 + Boss 精华 x1");
                }

                return;
            }

            int gold = Random.Range(25, 61);
            if (director != null)
            {
                director.AddRunGold(gold);
            }

            if (rewardType == RoomRewardType.Gold)
            {
                int bonusGold = Random.Range(40, 86);
                inventory.EarnCoins(bonusGold);
                if (director != null)
                {
                    director.ShowToast("金币房奖励：金币 +" + (gold + bonusGold));
                }

                return;
            }

            if (rewardType == RoomRewardType.Vitality)
            {
                inventory.PlayerStats.permanentHpBonus += 6;
                inventory.Heal(28);
                if (director != null)
                {
                    director.ShowToast("生命房奖励：生命上限 +6\n生命恢复 +28，金币 +" + gold);
                }

                return;
            }

            if (rewardType == RoomRewardType.Core)
            {
                int coreId = Random.Range(101, 105);
                CoreQuality quality = Random.value > 0.58f ? CoreQuality.Rare : CoreQuality.Common;
                inventory.AddCore(coreId, quality);
                CoreData core = GameDataModel.GetCore(coreId);
                if (director != null)
                {
                    director.ShowToast("虫核房奖励：" + GameDataModel.GetCoreQualityName(quality) + "色 " + (core != null ? core.coreName : "未知虫核") + "\n金币 +" + gold + "\n金色虫核仅由 Boss 掉落");
                }

                return;
            }

            if (rewardType == RoomRewardType.Treasure)
            {
                if (RunProgressionSystem.Instance != null)
                {
                    RunProgressionSystem.Instance.AddRandomTreasure();
                }

                if (director != null)
                {
                    director.ShowToast("宝物房奖励：获得 1 件宝物\n金币 +" + gold);
                }

                return;
            }

            float roll = Random.value;
            if (roll > 0.48f)
            {
                inventory.AddItem(2, 1);
                director?.ShowToast("清房奖励：金币 +" + gold + "\n获得道具：猩红药剂");
            }
            else
            {
                inventory.Heal(18);
                if (director != null)
                {
                    director.ShowToast("清房奖励：金币 +" + gold + "\n生命恢复 +18");
                }
            }
        }
    }

    public enum SoulKnightPortalMode
    {
        StartRun,
        NextStage,
        Victory
    }

    public class SoulKnightPortal : MonoBehaviour
    {
        private SoulKnightDirector director;
        private SoulKnightPortalMode mode;
        private bool used;

        public void Initialize(SoulKnightDirector owner, SoulKnightPortalMode portalMode)
        {
            director = owner;
            mode = portalMode;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryUse(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryUse(other);
        }

        private void TryUse(Collider2D other)
        {
            if (used || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (director == null)
            {
                director = SoulKnightDirector.Instance;
            }

            if (director == null)
            {
                return;
            }

            used = true;
            if (mode == SoulKnightPortalMode.StartRun)
            {
                director.StartRun();
            }
            else if (mode == SoulKnightPortalMode.Victory)
            {
                director.ShowSettlement();
            }
            else
            {
                director.LoadNextStage();
            }
        }
    }

    public class SoulKnightShopStand : MonoBehaviour
    {
        private SoulKnightDirector director;
        private readonly string[] lines =
        {
            "别摸柜台，刚擦过。",
            "这年头打怪的都比做生意的有钱。",
            "右键都点了，不买点什么说不过去吧？",
            "虫核保真，副作用概不负责。"
        };

        public void Initialize(SoulKnightDirector owner)
        {
            director = owner;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(1) || !IsMouseOverMe())
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector2.Distance(player.transform.position, transform.position) > 3.2f)
            {
                ResolveDirector();
                if (director != null)
                {
                    director.ShowToast("商人嘀咕：" + lines[Random.Range(0, lines.Length)] + "\n靠近后右键购买。");
                }

                return;
            }

            ResolveDirector();
            RogueShopPanel.Open(director);
        }

        private void ResolveDirector()
        {
            if (director == null)
            {
                director = SoulKnightDirector.Instance;
            }
        }

        private bool IsMouseOverMe()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(world.x, world.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].transform == transform)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
