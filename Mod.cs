using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Collections;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;


namespace sgtlaggyQuestTweaks;

public static class Constants
{
    public static readonly string[] TarkovShooter = [
        QuestTpl.THE_TARKOV_SHOOTER_PART_1,
        QuestTpl.THE_TARKOV_SHOOTER_PART_2,
        QuestTpl.THE_TARKOV_SHOOTER_PART_3,
        QuestTpl.THE_TARKOV_SHOOTER_PART_4,
        QuestTpl.THE_TARKOV_SHOOTER_PART_5,
        QuestTpl.THE_TARKOV_SHOOTER_PART_6,
        QuestTpl.THE_TARKOV_SHOOTER_PART_7,
        QuestTpl.THE_TARKOV_SHOOTER_PART_8,
    ];
    public static readonly HashSet<string> KeyClasses = [
        BaseClasses.KEY,
        BaseClasses.KEY_MECHANICAL,
        BaseClasses.KEYCARD
    ];
    public static readonly HashSet<string> HandoverCountItemBlacklist = [
        ItemTpl.RADIOTRANSMITTER_DIGITAL_SECURE_DSP_RADIO_TRANSMITTER,
        ItemTpl.BARTER_KOSA_UAV_ELECTRONIC_JAMMING_DEVICE,
        ItemTpl.INFO_NOTE_WITH_CODE_WORD_VORON,
    ];
}

public record LocationInfo(string Name, string Id, string MongoId);

[Injectable(TypePriority = OnLoadOrder.PostLoad + 999)]
public class Mod(
#if DEBUG
    JsonUtil json,
#endif
    Config config,
    QuestConfig questConfig,
    TemplateTable templates,
    LocaleTable locales,
    LocationTable locationsTable
) : IOnLoad
{
    private readonly string _modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private Dictionary<string, string> localeOverrides = [];

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var allQuests = templates.Quests;
        ModifySpecialCaseQuests(allQuests);
        ModifyQuestsNonExemptSettings(allQuests);
        ModifyQuestConditions(allQuests);

#if DEBUG
        // Dump modified quest database to a file for quick inspection in debug builds.
        var dumpFile = Path.Join(_modDir, "dump.json");
        File.WriteAllText(dumpFile, json.Serialize(allQuests, true));
#endif

        return Task.CompletedTask;
    }

    private void ModifySpecialCaseQuests(Dictionary<MongoId, Quest> quests)
    {
        if (config.SpecialCases.LightkeeperOnlyRequireLevel > 0)
        {
            var conditions = quests[QuestTpl.NETWORK_PROVIDER_PART_1].Conditions.AvailableForStart!;
            var reuseId = conditions[0].Id;
            conditions.Clear();
            conditions.Add(new QuestCondition
            {
                Id = reuseId,
                ConditionType = "Level",
                CompareMethod = ">=",
                Value = config.SpecialCases.LightkeeperOnlyRequireLevel,
                DynamicLocale = false,
                // Index = 0,
                // GlobalQuestCounterId = "",
                // ParentId = "",
                // VisibilityConditions = []
            });
        }

        if (config.SpecialCases.TarkovShooterM10)
        {
            foreach (var questId in Constants.TarkovShooter)
            {
                var conditions = quests[questId].Conditions.AvailableForFinish!;
                foreach (var objective in conditions)
                {
                    if (objective.ConditionType != "CounterCreator")
                    {
                        continue;
                    }

                    foreach (var condition in objective.Counter!.Conditions!)
                    {
                        if (condition.ConditionType != "Kills" && condition.ConditionType != "Shots")
                        {
                            continue;
                        }

                        condition.Weapon!.Add(ItemTpl.SNIPERRIFLE_SAKO_TRG_M10_338_LM_BOLTACTION_SNIPER_RIFLE);
                    }
                }
            }
        }

        if (config.SpecialCases.CollectorPrerequisiteBackport)
        {
            List<QuestCondition> prerequisites = [];
            var index = 0;

            // Require max-level base-game traders
            var maxTraders = new[] { Traders.PRAPOR, Traders.THERAPIST, Traders.SKIER, Traders.PEACEKEEPER, Traders.MECHANIC, Traders.RAGMAN, Traders.JAEGER };
            foreach (var trader in maxTraders)
            {
                prerequisites.Add(new()
                {
                    Id = new MongoId(),
                    Index = index++,
                    ParentId = "",
                    DynamicLocale = false,
                    VisibilityConditions = [],
                    GlobalQuestCounterId = "",
                    ConditionType = "TraderLoyalty",
                    Target = new(null, trader),
                    CompareMethod = ">=",
                    Value = 4,
                });
            }

            // Require 3+ Fence rep
            prerequisites.Add(new()
            {
                Id = new MongoId(),
                Index = index++,
                ParentId = "",
                DynamicLocale = false,
                VisibilityConditions = [],
                GlobalQuestCounterId = "",
                ConditionType = "TraderStanding",
                Target = new(null, Traders.FENCE),
                CompareMethod = ">=",
                Value = 3
            });

            // Require player level 40
            prerequisites.Add(new()
            {
                Id = new MongoId(),
                Index = index++,
                ParentId = "",
                DynamicLocale = false,
                VisibilityConditions = [],
                GlobalQuestCounterId = "",
                ConditionType = "Level",
                CompareMethod = ">=",
                Value = 40
            });

            // Require only a few quests
            var requiredQuests = new[] { QuestTpl.A_SHOOTER_BORN_IN_HEAVEN, QuestTpl.THE_TARKOV_SHOOTER_PART_4, QuestTpl.SEW_IT_GOOD_PART_4 };
            foreach (var quest in requiredQuests)
            {
                prerequisites.Add(new()
                {
                    Id = new MongoId(),
                    Index = index++,
                    ParentId = "",
                    DynamicLocale = false,
                    VisibilityConditions = [],
                    GlobalQuestCounterId = "",
                    ConditionType = "Quest",
                    Target = new(null, quest),
                    Status = [QuestStatusEnum.Success]
                });
            }
            // Special-case Chemical Part 4 and alternate choices
            var chemicalPart4Options = new[] { QuestTpl.CHEMICAL_PART_4, QuestTpl.OUT_OF_CURIOSITY, QuestTpl.BIG_CUSTOMER };
            foreach (var quest in chemicalPart4Options)
            {
                prerequisites.Add(new()
                {
                    Id = new MongoId(),
                    Index = index++,
                    ParentId = "",
                    DynamicLocale = false,
                    VisibilityConditions = [],
                    GlobalQuestCounterId = "",
                    ConditionType = "Quest",
                    Target = new(null, quest),
                    Status = [QuestStatusEnum.Success, QuestStatusEnum.Fail]
                });
            }

            quests[QuestTpl.COLLECTOR].Conditions.AvailableForStart = prerequisites;
        }
    }

    private void ModifyQuestsNonExemptSettings(Dictionary<MongoId, Quest> quests)
    {
        if (!(config.QualityOfLife.RevealAllQuestObjectives
              || config.QualityOfLife.RevealUnknownRewards
              || config.QualityOfLife.RemoveTimeGates))
        {
            return;
        }

        foreach (var quest in quests.Values)
        {
            var objectives = quest.Conditions.AvailableForFinish!;

            if (config.QualityOfLife.RevealAllQuestObjectives)
            {
                foreach (var objective in objectives)
                {
                    objective.VisibilityConditions?.Clear();
                }
            }

            if (config.QualityOfLife.RevealUnknownRewards)
            {
                if (quest.Rewards is not null)
                {
                    foreach (var reward in quest.Rewards["Success"])
                    {
                        reward.Unknown = false;
                    }
                }
            }

            if (config.QualityOfLife.RemoveTimeGates)
            {
                foreach (var prereq in quest.Conditions.AvailableForStart!)
                {
                    if (prereq.AvailableAfter is not null)
                    {
                        prereq.AvailableAfter = 0;
                    }
                }
            }
        }
    }

    private void ModifyQuestConditions(Dictionary<MongoId, Quest> quests)
    {
        var shouldModifyConditions = config.GlobalConditions.AnyChanged
                                     || (config.QuestOverrides.Count > 0);
        if (!shouldModifyConditions)
        {
            return;
        }

        var items = templates.Items;
        var enLocale = locales.Global["en"].Value!;

        var locations = locationsTable.GetDictionary().Values
            .Where(loc => loc.Base?.Enabled ?? false)
            .Select(
                (loc) =>
                {
                    string? name;
                    if (!enLocale.TryGetValue(loc.Base.Id, out name))
                    {
                        if (!enLocale.TryGetValue($"{loc.Base.IdField} Name", out name))
                        {
                            return null;
                        }
                    }
                    return new LocationInfo(
                        // Terminal/Lab are undefined using ‘.Id’, need to get "proper" name
                        name,
                        loc.Base.Id,
                        loc.Base.IdField
                    );
                }
            )
            .Where(info => (info is not null))
            .ToList();
        // special-case factory night because it’s not enabled and name in locale is "Night Factory"
        var factoryNight = locationsTable.GetLocation(nameof(ELocationName.factory4_night))!.Base;
        locations.Add(new LocationInfo(
            "Factory",
            factoryNight.Id,
            factoryNight.IdField
        ));

        foreach (var (questId, quest) in quests)
        {
            var objectives = quest.Conditions.AvailableForFinish!;

            foreach (var objective in objectives)
            {
                if ((objective.OneSessionOnly ?? false)
                    && ShouldModifyCondition(questId, "RemoveInOneRaid"))
                {
                    objective.OneSessionOnly = false;
                }

                if (objective.ConditionType == "HandoverItem" || objective.ConditionType == "FindItem")
                {
                    if (ShouldModifyCondition(questId, "RemoveFindInRaid"))
                    {
                        objective.OnlyFoundInRaid = false;
                    }

                    TemplateItem? item;
                    if (objective.Target!.IsList)
                    {
                        items.TryGetValue(objective.Target.List![0], out item);
                    }
                    else
                    {
                        items.TryGetValue(objective.Target.Item!, out item);
                    }

                    if ((item is not null)
                        && (item.Properties!.QuestItem != true)
                        && !Constants.KeyClasses.Contains(item.Parent)
                        && !Constants.HandoverCountItemBlacklist.Contains(item.Id))
                    {
                        objective.Value = GetNewObjectiveValue(questId, "HandoverItem", objective.Value);
                    }
                }

                if (objective.ConditionType != "CounterCreator")
                {
                    continue;
                }

                if (ShouldModifyCondition(questId, "RemoveZone") && !ShouldModifyCondition(questId, "RemoveMap"))
                {
                    var zoneCond = objective.Counter!.Conditions!.Find(
                        cond => cond.ConditionType == "InZone");
                    var mapCond = objective.Counter.Conditions.Find(
                        cond => cond.ConditionType == "Location");

                    if ((zoneCond is not null) && (mapCond is null))
                    {
                        foreach (var loc in locations)
                        {
                            if (quest.Location == loc!.MongoId
                                || enLocale[objective.Id].Contains(loc.Name))
                            {
                                if (zoneCond.ConditionType == "InZone")
                                {
                                    zoneCond.Zones = null;
                                    zoneCond.ConditionType = "Location";
                                    zoneCond.Target = new ListOrT<string>([loc.Id], null);
                                }
                                // already replaced, support Factory and Ground Zero variants
                                else
                                {
                                    zoneCond.Target!.List!.Add(loc.Id);
                                }
                            }
                        }
                    }
                }

                objective.Counter!.Conditions!.RemoveAll(cond =>
                    (ShouldModifyCondition(questId, "RemoveSelfHealthEffect") && (cond.ConditionType == "HealthEffect"))
                    || (ShouldModifyCondition(questId, "RemoveSelfGear") && (cond.ConditionType == "Equipment"))
                    || (ShouldModifyCondition(questId, "RemoveMap") && (cond.ConditionType == "Location"))
                    || (ShouldModifyCondition(questId, "RemoveZone") && (cond.ConditionType == "InZone"))
                );

                // auto-complete counters that no longer have an "action" condition
                if (objective.Counter.Conditions.TrueForAll(cond =>
                    cond.ConditionType == "Equipment"
                    || cond.ConditionType == "Location"
                    || cond.ConditionType == "InZone"))
                {
                    objective.Value = 0;
                    continue;
                }

                foreach (var condition in objective.Counter.Conditions)
                {
                    if (condition.ConditionType != "Shots" && condition.ConditionType != "Kills")
                    {
                        continue;
                    }

                    if ((config.GlobalConditions.EliminationCount >= 0 || config.GlobalConditions.EliminationPercent >= 0)
                        && condition.ConditionType == "Kills")
                    {
                        objective.Value = GetNewObjectiveValue(questId, "Elimination", objective.Value);
                    }

                    if (ShouldModifyCondition(questId, "RemoveTarget"))
                    {
                        condition.SavageRole?.Clear();
                        condition.Target = new ListOrT<string>(null, "Any");
                    }

                    if (ShouldModifyCondition(questId, "RemoveWeapon"))
                    {
                        condition.Weapon?.Clear();
                        condition.WeaponCaliber?.Clear();
                    }

                    if (ShouldModifyCondition(questId, "RemoveWeaponMods"))
                    {
                        condition.WeaponModsExclusive = [];
                        condition.WeaponModsInclusive = [];
                    }

                    if (ShouldModifyCondition(questId, "RemoveEnemyHealthEffect"))
                    {
                        condition.EnemyHealthEffects?.Clear();
                    }

                    if (ShouldModifyCondition(questId, "RemoveEnemyGear"))
                    {
                        condition.EnemyEquipmentExclusive = [];
                        condition.EnemyEquipmentInclusive = [];
                    }

                    if (ShouldModifyCondition(questId, "RemoveBodyPart"))
                    {
                        condition.BodyPart?.Clear();
                    }

                    if (ShouldModifyCondition(questId, "RemoveDistance"))
                    {
                        condition.Distance = new CounterConditionDistance
                        {
                            CompareMethod = ">=",
                            Value = 0
                        };
                    }

                    if (ShouldModifyCondition(questId, "RemoveTime") && (condition.Daytime is not null))
                    {
                        condition.Daytime = new DaytimeCounter
                        {
                            From = 0,
                            To = 0
                        };
                    }
                }
            }
        }

        if (!config.GlobalConditions.AffectRepeatables)
        {
            return;
        }

        foreach (var quest in questConfig.RepeatableQuests)
        {
            var questId = quest.Id;
            if (ShouldModifyCondition(questId, "RemoveFindInRaid"))
            {
                foreach (var completion in quest.QuestConfig.CompletionConfig)
                {
                    completion.RequiredItemsAreFiR = false;
                }
            }

            var elims = quest.QuestConfig.Elimination;
            if (elims is null)
            {
                continue;
            }

            foreach (var elim in elims)
            {
                if (ShouldModifyCondition(questId, "RemoveTarget"))
                {
                    elim.Targets = [new ProbabilityObject<string, BossInfo> {
                        Key = "Any",
                        RelativeProbability = 1,
                        Data = new BossInfo{
                            IsBoss = false,
                            IsPmc = false
                        }
                    }];
                }

                if (ShouldModifyCondition(questId, "RemoveWeapon"))
                {
                    elim.WeaponCategoryRequirementChance = 0;
                    elim.WeaponRequirementChance = 0;
                }

                if (ShouldModifyCondition(questId, "RemoveBodyPart"))
                {
                    elim.BodyPartChance = 0;
                }

                if (ShouldModifyCondition(questId, "RemoveDistance"))
                {
                    elim.DistanceProbability = 0;
                }
            }
        }
    }

    private double? GetNewObjectiveValue(MongoId questId, string condition, double? original)
    {
        if (original is null)
        {
            return null;
        }

        var absoluteProp = typeof(ConditionsConfig).GetProperty($"{condition}Count")!;
        var percentProp = typeof(ConditionsConfig).GetProperty($"{condition}Percent")!;

        int? absolute;
        int? percent;

        ConditionsConfig? questOverride;
        if (config.QuestOverrides.TryGetValue(questId, out questOverride))
        {
            absolute = absoluteProp.GetValue(questOverride) as int?;
            if (absolute < 0)
            {
                return original;
            }
            else if (absolute >= 0)
            {
                return absolute;
            }

            percent = percentProp.GetValue(questOverride) as int?;
            if (percent < 0)
            {
                return original;
            }
            else if (percent >= 0)
            {
                var value = Double.Round((original.Value * percent / 100).Value);
                if (value == 0)
                {
                    return 1;
                }
                else
                {
                    return value;
                }
            }
        }

        if (IsQuestExempt(questId))
        {
            return original;
        }

        absolute = absoluteProp.GetValue(config.GlobalConditions) as int?;
        if (absolute >= 0)
        {
            return absolute;
        }

        percent = percentProp.GetValue(config.GlobalConditions) as int?;
        if (percent >= 0)
        {
            var value = Double.Round((original.Value * percent / 100).Value);
            if (value == 0)
            {
                return 1;
            }
            else
            {
                return value;
            }
        }

        return original;
    }

    private bool ShouldModifyCondition(MongoId questId, string condition)
    {
        var prop = typeof(ConditionsConfig).GetProperty(condition)!;

        ConditionsConfig? questOverride;
        if (config.QuestOverrides.TryGetValue(questId, out questOverride))
        {
            var overrideValue = prop.GetValue(questOverride) as bool?;
            if (overrideValue is not null)
            {
                return overrideValue.Value;
            }
        }

        if (IsQuestExempt(questId))
        {
            return false;
        }

        return (prop.GetValue(config.GlobalConditions) as bool?) ?? false;
    }

    private bool IsQuestExempt(MongoId questId)
    {
        if ((config.OnlyQuests.Count > 0) && !config.OnlyQuests.Contains(questId))
        {
            return true;
        }
        if (config.ExemptQuests.Contains(questId))
        {
            return true;
        }
        return false;
    }
}
