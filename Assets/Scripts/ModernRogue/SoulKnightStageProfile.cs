using UnityEngine;

namespace ModernRogue
{
    public enum SoulKnightBossArchetype
    {
        Titan,
        EmberMage,
        StormGuard,
        BroodQueen
    }

    public enum StageSignatureKind
    {
        Combat,
        Crossroad,
        Treasure,
        Shop
    }

    public sealed class SoulKnightStageProfile
    {
        public int Stage { get; private set; }
        public string ThemeName { get; private set; }
        public string ThemeHint { get; private set; }
        public string SignatureRoomTitle { get; private set; }
        public StageSignatureKind SignatureKind { get; private set; }
        public SoulKnightBossArchetype BossArchetype { get; private set; }
        public int RoomCountMin { get; private set; }
        public int RoomCountMax { get; private set; }
        public float ShopChance { get; private set; }
        public float TreasureChance { get; private set; }
        public float CrossroadChance { get; private set; }
        public int WaveCount { get; private set; }
        public int EnemyCountMin { get; private set; }
        public int EnemyCountMax { get; private set; }
        public float RangedEnemyChance { get; private set; }
        public float EliteMeleeChance { get; private set; }
        public Color BaseFloorTint { get; private set; }
        public Color AccentTint { get; private set; }
        public Color CorridorShadowTint { get; private set; }
        public float CorridorShadowAlpha { get; private set; }
        public bool DenseDecor { get; private set; }
        public bool ExtraBanner { get; private set; }
        public int GoldWeight { get; private set; }
        public int VitalityWeight { get; private set; }
        public int CoreWeight { get; private set; }
        public int TreasureWeight { get; private set; }
        public int EquipmentWeight { get; private set; }

        public static SoulKnightStageProfile Create(
            int stage,
            string themeName,
            string themeHint,
            string signatureRoomTitle,
            StageSignatureKind signatureKind,
            SoulKnightBossArchetype bossArchetype,
            int roomCountMin,
            int roomCountMax,
            float shopChance,
            float treasureChance,
            float crossroadChance,
            int waveCount,
            int enemyCountMin,
            int enemyCountMax,
            float rangedEnemyChance,
            float eliteMeleeChance,
            Color baseFloorTint,
            Color accentTint,
            Color corridorShadowTint,
            float corridorShadowAlpha,
            bool denseDecor,
            bool extraBanner,
            int goldWeight,
            int vitalityWeight,
            int coreWeight,
            int treasureWeight,
            int equipmentWeight)
        {
            return new SoulKnightStageProfile
            {
                Stage = stage,
                ThemeName = themeName,
                ThemeHint = themeHint,
                SignatureRoomTitle = signatureRoomTitle,
                SignatureKind = signatureKind,
                BossArchetype = bossArchetype,
                RoomCountMin = roomCountMin,
                RoomCountMax = roomCountMax,
                ShopChance = shopChance,
                TreasureChance = treasureChance,
                CrossroadChance = crossroadChance,
                WaveCount = waveCount,
                EnemyCountMin = enemyCountMin,
                EnemyCountMax = enemyCountMax,
                RangedEnemyChance = rangedEnemyChance,
                EliteMeleeChance = eliteMeleeChance,
                BaseFloorTint = baseFloorTint,
                AccentTint = accentTint,
                CorridorShadowTint = corridorShadowTint,
                CorridorShadowAlpha = corridorShadowAlpha,
                DenseDecor = denseDecor,
                ExtraBanner = extraBanner,
                GoldWeight = goldWeight,
                VitalityWeight = vitalityWeight,
                CoreWeight = coreWeight,
                TreasureWeight = treasureWeight,
                EquipmentWeight = equipmentWeight
            };
        }
    }

    public static class SoulKnightStageProfiles
    {
        public static SoulKnightStageProfile Resolve(int stage)
        {
            switch (Mathf.Clamp(stage, 1, 5))
            {
                case 2:
                    return SoulKnightStageProfile.Create(
                        2,
                        "箭廊墓道",
                        "第二层特色：远程敌人更常出现，通道更冷、更开阔，金币房权重提高。",
                        "箭廊岔路",
                        StageSignatureKind.Crossroad,
                        SoulKnightBossArchetype.StormGuard,
                        9,
                        12,
                        0.22f,
                        0.18f,
                        0.42f,
                        3,
                        3,
                        5,
                        0.72f,
                        0.06f,
                        new Color(0.84f, 0.88f, 0.95f, 1f),
                        new Color(0.76f, 0.84f, 0.96f, 1f),
                        new Color(0.04f, 0.08f, 0.12f, 1f),
                        0.4f,
                        false,
                        false,
                        5,
                        1,
                        2,
                        1,
                        4);
                case 3:
                    return SoulKnightStageProfile.Create(
                        3,
                        "腐潮深井",
                        "第三层特色：精英近战更常出现，房间更压迫，生命与虫核奖励倾向提高。",
                        "腐潮祭井",
                        StageSignatureKind.Combat,
                        SoulKnightBossArchetype.BroodQueen,
                        9,
                        12,
                        0.16f,
                        0.2f,
                        0.34f,
                        3,
                        4,
                        6,
                        0.46f,
                        0.24f,
                        new Color(0.78f, 0.88f, 0.82f, 1f),
                        new Color(0.54f, 0.82f, 0.58f, 1f),
                        new Color(0.03f, 0.1f, 0.05f, 1f),
                        0.48f,
                        true,
                        true,
                        2,
                        4,
                        4,
                        1,
                        3);
                case 4:
                    return SoulKnightStageProfile.Create(
                        4,
                        "熔炉回廊",
                        "第四层特色：波次数提高，宝物与装备房更激进，走廊与门厅更厚重。",
                        "熔炉密藏室",
                        StageSignatureKind.Treasure,
                        SoulKnightBossArchetype.EmberMage,
                        10,
                        13,
                        0.18f,
                        0.24f,
                        0.28f,
                        4,
                        4,
                        6,
                        0.5f,
                        0.18f,
                        new Color(0.92f, 0.82f, 0.72f, 1f),
                        new Color(0.96f, 0.62f, 0.32f, 1f),
                        new Color(0.12f, 0.05f, 0.02f, 1f),
                        0.56f,
                        true,
                        true,
                        1,
                        1,
                        2,
                        4,
                        5);
                case 5:
                    return SoulKnightStageProfile.Create(
                        5,
                        "王座外环",
                        "第五层终局：始终双 Boss 同场；组合从巨像、风暴守卫、母虫、烬火术士中随机抽取。",
                        "王座前厅",
                        StageSignatureKind.Combat,
                        SoulKnightBossArchetype.StormGuard,
                        8,
                        10,
                        0.12f,
                        0.28f,
                        0.22f,
                        4,
                        5,
                        7,
                        0.58f,
                        0.2f,
                        new Color(0.86f, 0.82f, 0.94f, 1f),
                        new Color(0.76f, 0.58f, 0.96f, 1f),
                        new Color(0.08f, 0.04f, 0.12f, 1f),
                        0.62f,
                        true,
                        true,
                        1,
                        1,
                        4,
                        3,
                        2);
                default:
                    return SoulKnightStageProfile.Create(
                        1,
                        "废墟前厅",
                        "第一层特色：平衡教学层，敌人与奖励都更平均，便于熟悉流程。",
                        "前厅战场",
                        StageSignatureKind.Combat,
                        SoulKnightBossArchetype.Titan,
                        8,
                        11,
                        0.16f,
                        0.16f,
                        0.24f,
                        3,
                        3,
                        5,
                        0.18f,
                        0.02f,
                        new Color(0.9f, 0.9f, 0.9f, 1f),
                        new Color(0.92f, 0.88f, 0.84f, 1f),
                        new Color(0.02f, 0.02f, 0.04f, 1f),
                        0.34f,
                        false,
                        false,
                        3,
                        2,
                        2,
                        1,
                        4);
            }
        }
    }
}
