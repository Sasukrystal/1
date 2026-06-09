namespace ModernRogue
{
    public enum MetaAffixKind
    {
        LifeSteal,
        CritRate,
        AttackSpeed,
        DamageReduction,
        MaxHpPercent
    }

    public static class MetaAffixUtility
    {
        public static string GetName(MetaAffixKind kind)
        {
            switch (kind)
            {
                case MetaAffixKind.LifeSteal:
                    return "吸血";
                case MetaAffixKind.CritRate:
                    return "暴击率";
                case MetaAffixKind.AttackSpeed:
                    return "攻击速度";
                case MetaAffixKind.DamageReduction:
                    return "减伤";
                case MetaAffixKind.MaxHpPercent:
                    return "生命上限";
                default:
                    return "未知词条";
            }
        }

        public static string GetDescription(MetaAffixKind kind)
        {
            switch (kind)
            {
                case MetaAffixKind.LifeSteal:
                    return "造成伤害后恢复 3% 生命";
                case MetaAffixKind.CritRate:
                    return "暴击率 +5%";
                case MetaAffixKind.AttackSpeed:
                    return "攻击速度 +8%";
                case MetaAffixKind.DamageReduction:
                    return "受到的伤害 -4%";
                case MetaAffixKind.MaxHpPercent:
                    return "生命上限 +6%";
                default:
                    return "";
            }
        }

        public static bool IsWeaponAffix(MetaAffixKind kind)
        {
            return kind == MetaAffixKind.LifeSteal || kind == MetaAffixKind.CritRate || kind == MetaAffixKind.AttackSpeed;
        }

        public static bool IsArmorAffix(MetaAffixKind kind)
        {
            return kind == MetaAffixKind.LifeSteal || kind == MetaAffixKind.CritRate
                || kind == MetaAffixKind.DamageReduction || kind == MetaAffixKind.MaxHpPercent;
        }
    }
}
