using flanne;
using flanne.Core;
using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace Callmore.MoreUI;

public delegate string StatEntryFormatterDelegate(
    float baseValue,
    Func<float, float> modifyFunc,
    Func<float, float> modifyInverseFunc,
    float multiplyerBonus,
    float multiplyerReduction,
    int flatBonus
);

struct StatEntry
{
    public string Name;
    public Func<PlayerController, StatMod> GetStat;
    public Func<PlayerController, float> GetStatBase;
    public StatEntryFormatterDelegate Format;

    public string GetString(PlayerController player)
    {
        StatMod stat = GetStat(player);
        float statBase = GetStatBase != null ? GetStatBase(player) : 1;
        string formattedString = Format(
            statBase,
            stat.Modify,
            stat.ModifyInverse,
            GetMultiplierBonus(stat),
            GetMultiplierReduction(stat),
            GetFlatBonus(stat)
        );
        return $"{Name}: {formattedString}";
    }

    private static readonly FieldInfo _multiplierBonusFieldInfo = typeof(StatMod).GetField(
        "_multiplierBonus",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    private static readonly FieldInfo _multiplierReductionFieldInfo = typeof(StatMod).GetField(
        "_multiplierReduction",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    private static readonly FieldInfo _flatBonusFieldInfo = typeof(StatMod).GetField(
        "_flatBonus",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    private static float GetMultiplierBonus(StatMod stat)
    {
        return (float)_multiplierBonusFieldInfo.GetValue(stat);
    }

    private static float GetMultiplierReduction(StatMod stat)
    {
        return (float)_multiplierReductionFieldInfo.GetValue(stat);
    }

    private static int GetFlatBonus(StatMod stat)
    {
        return (int)_flatBonusFieldInfo.GetValue(stat);
    }
}

public static class StatEntryFormatter
{
    public static StatEntryFormatterDelegate Value(
        string unit = "",
        int decimals = 0,
        bool inverse = false
    )
    {
        if (inverse)
        {
            return (baseValue, _, modifyInverse, _, _, _) =>
                $"{Math.Round(modifyInverse(baseValue), decimals)}{unit}";
        }
        return (baseValue, modify, _, _, _, _) =>
            $"{Math.Round(modify(baseValue), decimals)}{unit}";
    }

    public static StatEntryFormatterDelegate ValueDecimal(int places)
    {
        return (baseValue, modFunc, _, _, _, _) => $"{Math.Round(modFunc(baseValue), places)}";
    }

    public static StatEntryFormatterDelegate ValuePercent(
        string unit = "",
        int decimals = 0,
        bool inverse = false
    )
    {
        if (inverse)
        {
            return (
                baseValue,
                _,
                modifyInverse,
                multiplyerBonus,
                multiplyerReduction,
                flatBonus
            ) =>
                $"{Math.Round(modifyInverse(baseValue), decimals)}{unit} ({MakeStatString(true, multiplyerBonus, multiplyerReduction, flatBonus)})";
        }
        return (baseValue, modify, _, multiplyerBonus, multiplyerReduction, flatBonus) =>
            $"{Math.Round(modify(baseValue), decimals)}{unit} ({MakeStatString(false, multiplyerBonus, multiplyerReduction, flatBonus)})";
    }

    public static string Percent(
        float _,
        Func<float, float> _1,
        Func<float, float> _2,
        float multiplyerBonus,
        float multiplyerReduction,
        int flatBonus
    )
    {
        return $"{MakeStatString(false, multiplyerBonus, multiplyerReduction, flatBonus)}";
    }

    private static string MakeStatString(
        bool inverse,
        float multiplyerBonus,
        float multiplyerReduction,
        int flatBonus
    )
    {
        if (inverse)
        {
            return flatBonus > 0
                ? $"{1 / ((1 + multiplyerBonus) * multiplyerReduction):p0} +{flatBonus}"
                : $"{1 / ((1 + multiplyerBonus) * multiplyerReduction):p0}";
        }
        return flatBonus > 0
            ? $"{(1 + multiplyerBonus) * multiplyerReduction:p0} +{flatBonus}"
            : $"{(1 + multiplyerBonus) * multiplyerReduction:p0}";
    }
}

[RequireComponent(typeof(RectTransform), typeof(TextMeshProUGUI))]
class StatGUI : MonoBehaviour
{
    private GameController _gameController;

    /*
    TODO: Future stats to track:
        XP rate
        IDK POKE AROUND DLL FOR MORE STATENTRIES
    */

    private static readonly StatEntry[] _statEntries =
    {
        new StatEntry()
        {
            Name = "Fire Rate",
            GetStat = (player) => player.stats[StatType.FireRate],
            GetStatBase = (player) => player.gun.gunData.shotCooldown,
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Reload Time",
            GetStat = (player) => player.stats[StatType.ReloadRate],
            GetStatBase = (player) => player.gun.gunData.reloadDuration,
            Format = StatEntryFormatter.ValuePercent("s", 2, true),
        },
        new StatEntry()
        {
            Name = "Max Ammo",
            GetStat = (player) => player.stats[StatType.MaxAmmo],
            GetStatBase = (player) => player.gun.gunData.maxAmmo,
            Format = StatEntryFormatter.Value()
        },
        new StatEntry()
        {
            Name = "Damage",
            GetStat = (player) => player.stats[StatType.BulletDamage],
            GetStatBase = (player) => player.gun.gunData.damage,
            Format = StatEntryFormatter.ValueDecimal(1),
        },
        new StatEntry()
        {
            Name = "Summon Damage",
            GetStat = (player) => player.stats[StatType.SummonDamage],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Summon Attack Speed",
            GetStat = (player) => player.stats[StatType.SummonAttackSpeed],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Projectiles",
            GetStat = (player) => player.stats[StatType.Projectiles],
            GetStatBase = (player) => player.gun.gunData.numOfProjectiles,
            Format = StatEntryFormatter.Value()
        },
        new StatEntry()
        {
            Name = "Projectile Speed",
            GetStat = (player) => player.stats[StatType.ProjectileSpeed],
            GetStatBase = (player) => player.gun.gunData.projectileSpeed,
            Format = StatEntryFormatter.ValuePercent(decimals: 1),
        },
        new StatEntry()
        {
            Name = "Projectile Size",
            GetStat = (player) => player.stats[StatType.ProjectileSize],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Knockback",
            GetStat = (player) => player.stats[StatType.Knockback],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Movement Speed",
            GetStat = (player) => player.stats[StatType.MoveSpeed],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Walking Speed",
            GetStat = (player) => player.stats[StatType.MoveSpeed],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Spread",
            GetStat = (player) => player.stats[StatType.Spread],
            GetStatBase = (player) => player.gun.gunData.spread,
            Format = StatEntryFormatter.Value(),
        },
        new StatEntry()
        {
            Name = "Bounce",
            GetStat = (player) => player.stats[StatType.Bounce],
            GetStatBase = (player) => player.gun.gunData.bounce,
            Format = StatEntryFormatter.Value(),
        },
        new StatEntry()
        {
            Name = "Piercing",
            GetStat = (player) => player.stats[StatType.Piercing],
            GetStatBase = (player) => player.gun.gunData.piercing,
            Format = StatEntryFormatter.Value(),
        },
        new StatEntry()
        {
            Name = "Max HP",
            GetStat = (player) => player.stats[StatType.MaxHP],
            GetStatBase = (player) => player.loadedCharacter.startHP,
            Format = StatEntryFormatter.Value(),
        },
        new StatEntry()
        {
            Name = "Size",
            GetStat = (player) => player.stats[StatType.CharacterSize],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Pickup Range",
            GetStat = (player) => player.stats[StatType.PickupRange],
            Format = StatEntryFormatter.Percent,
        },
        new StatEntry()
        {
            Name = "Vision Range",
            GetStat = (player) => player.stats[StatType.VisionRange],
            Format = StatEntryFormatter.Percent,
        }
    };

    private void Awake()
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = "---";
        tmp.font = Helpers.GetTMPFontAssetByName("Express");
        tmp.UpdateFontAsset();

        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.fontSize = 9f;
    }

    public void SetGameControllerTarget(ref GameController controller)
    {
        _gameController = controller;
    }

    private void Update()
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = GetText();
    }

    private string GetText()
    {
        PlayerController player = _gameController.player.GetComponent<PlayerController>();
        StatsHolder stats = player.stats;
        StringBuilder sb = new StringBuilder();
        foreach (StatEntry stat in _statEntries)
        {
            sb.AppendLine(stat.GetString(player));
        }
        return sb.ToString();
    }
}
