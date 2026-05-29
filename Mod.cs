using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
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

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 999)]
public class Mod(
    ISptLogger<Mod> logger,
    DatabaseService db,
    ConfigServer configServer,
    JsonUtil json
) : IOnLoad
{
    private Config? _config;
    private readonly string _modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    public Task OnLoad()
    {
        LoadConfig();
        if (_config is null)
        {
            return Task.CompletedTask;
        }

        var allQuests = db.GetQuests();
        ModifySpecialCaseQuests(allQuests);
        ModifyQuestsNonExemptSettings(allQuests);

        var questsToModify = allQuests;
        if (_config.OnlyQuests.Count > 0)
        {
            questsToModify = allQuests.Where(kvp => _config.OnlyQuests.Contains(kvp.Key)).ToDictionary();
        }
        else if (_config.ExemptQuests.Count > 0)
        {
            questsToModify = allQuests.Where(kvp => !_config.ExemptQuests.Contains(kvp.Key)).ToDictionary();
        }

        ModifyQuestConditions(questsToModify);

#if DEBUG
        // Dump modified quest database to a file for quick inspection in debug builds.
        var dumpFile = Path.Join(_modDir, "dump.json");
        File.WriteAllText(dumpFile, json.Serialize(allQuests, true));
#endif

        return Task.CompletedTask;
    }

    private void ModifySpecialCaseQuests(Dictionary<MongoId, Quest> quests)
    {
        if (_config!.LightkeeperOnlyRequireLevel > 0)
        {
            var conditions = quests[QuestTpl.NETWORK_PROVIDER_PART_1].Conditions.AvailableForStart!;
            var reuseId = conditions[0].Id;
            conditions.Clear();
            conditions.Add(new QuestCondition
            {
                Id = reuseId,
                ConditionType = "Level",
                CompareMethod = ">=",
                Value = _config.LightkeeperOnlyRequireLevel,
                DynamicLocale = false,
                // Index = 0,
                // GlobalQuestCounterId = "",
                // ParentId = "",
                // VisibilityConditions = []
            });
        }

        if (_config.TarkovShooterM10)
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
    }

    private void ModifyQuestsNonExemptSettings(Dictionary<MongoId, Quest> quests)
    {
        if (!(_config!.RevealAllQuestObjectives
              || _config.RevealUnknownRewards
              || _config.RemoveTimeGates))
        {
            return;
        }

        foreach (var quest in quests.Values)
        {
            var objectives = quest.Conditions.AvailableForFinish!;

            if (_config.RevealAllQuestObjectives)
            {
                foreach (var objective in objectives)
                {
                    objective.VisibilityConditions?.Clear();
                }
            }

            if (_config.RevealUnknownRewards)
            {
                if (quest.Rewards is not null)
                {
                    foreach (var reward in quest.Rewards["Success"])
                    {
                        reward.Unknown = false;
                    }
                }
            }

            if (_config.RemoveTimeGates)
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
        var remove = _config!.RemoveConditions;
        var shouldModifyConditions = remove.AnyEnabled
                                     || _config.HandoverItemPercent >= 0
                                     || _config.EliminationPercent >= 0
                                     || _config.HandoverItemCount >= 0
                                     || _config.EliminationCount >= 0;
        if (!shouldModifyConditions)
        {
            return;
        }

        var items = db.GetItems();
        var enLocale = db.GetLocales().Global["en"].Value!;

        var locations = db.GetLocations().GetDictionary().Values
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
        var factoryNight = db.GetLocation(nameof(ELocationName.factory4_night))!.Base;
        locations.Add(new LocationInfo(
            "Factory",
            factoryNight.Id,
            factoryNight.IdField
        ));

        foreach (var quest in quests.Values)
        {
            var objectives = quest.Conditions.AvailableForFinish!;

            foreach (var objective in objectives)
            {
                if (objective.ConditionType == "HandoverItem" || objective.ConditionType == "FindItem")
                {
                    if (remove.FindInRaid)
                    {
                        objective.OnlyFoundInRaid = false;
                    }

                    TemplateItem item;
                    if (objective.Target!.IsList)
                    {
                        item = items[objective.Target.List![0]];
                    }
                    else
                    {
                        item = items[objective.Target.Item!];
                    }
                    if ((item.Properties!.QuestItem != true)
                        && !Constants.KeyClasses.Contains(item.Parent)
                        && !Constants.HandoverCountItemBlacklist.Contains(item.Id))
                    {
                        objective.Value = GetNewObjectiveValue(objective.Value, _config.HandoverItemCount, _config.HandoverItemPercent);
                    }
                }

                if (objective.ConditionType != "CounterCreator")
                {
                    continue;
                }

                if (remove.Zone && !remove.Map)
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
                    (remove.SelfHealthEffect && cond.ConditionType == "HealthEffect")
                    || (remove.SelfGear && cond.ConditionType == "Equipment")
                    || (remove.Map && cond.ConditionType == "Location")
                    || (remove.Zone && cond.ConditionType == "InZone")
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

                    if ((_config.EliminationCount >= 0 || _config.EliminationPercent >= 0)
                        && condition.ConditionType == "Kills")
                    {
                        objective.Value = GetNewObjectiveValue(objective.Value, _config.EliminationCount, _config.EliminationPercent);
                    }

                    if (remove.Target)
                    {
                        condition.SavageRole?.Clear();
                        condition.Target = new ListOrT<string>(null, "Any");
                    }

                    if (remove.Weapon)
                    {
                        condition.Weapon?.Clear();
                        condition.WeaponCaliber?.Clear();
                    }

                    if (remove.WeaponMods)
                    {
                        condition.WeaponModsExclusive = [];
                        condition.WeaponModsInclusive = [];
                    }

                    if (remove.EnemyHealthEffect)
                    {
                        condition.EnemyHealthEffects?.Clear();
                    }

                    if (remove.EnemyGear)
                    {
                        condition.EnemyEquipmentExclusive = [];
                        condition.EnemyEquipmentInclusive = [];
                    }

                    if (remove.BodyPart)
                    {
                        condition.BodyPart?.Clear();
                    }

                    if (remove.Distance)
                    {
                        condition.Distance = new CounterConditionDistance
                        {
                            CompareMethod = ">=",
                            Value = 0
                        };
                    }

                    if (remove.Time && (condition.Daytime is not null))
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

        if (!_config.AffectRepeatables)
        {
            return;
        }

        var questConfig = configServer.GetConfig<QuestConfig>();

        foreach (var quest in questConfig.RepeatableQuests)
        {
            if (remove.FindInRaid)
            {
                foreach (var completion in quest.QuestConfig.CompletionConfig)
                {
                    completion.RequiredItemsAreFiR = false;
                }
            }

            var elims = quest.QuestConfig.Elimination;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (elims is null)
            {
                continue;
            }

            foreach (var elim in elims)
            {
                if (remove.Target)
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

                if (remove.Weapon)
                {
                    elim.WeaponCategoryRequirementChance = 0;
                    elim.WeaponRequirementChance = 0;
                }

                if (remove.BodyPart)
                {
                    elim.BodyPartChance = 0;
                }

                if (remove.Distance)
                {
                    elim.DistanceProbability = 0;
                }
            }
        }
    }

    private static double? GetNewObjectiveValue(double? original, int absolute, int percent)
    {
        if (original is null)
        {
            return null;
        }

        if (absolute >= 0)
        {
            return absolute;
        }

        if (percent >= 0)
        {
            var value = Double.Round(original.Value * percent / 100);
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

    private void LoadConfig()
    {
        try
        {
            _config = json.DeserializeFromFile<Config>(Path.Join(_modDir, "config.json"));
        }
        catch (JsonException)
        {
            logger.Error("Invalid config.");
            return;
        }

        if (_config is null)
        {
            logger.Error("Missing config.");
            return;
        }

        return;
    }
}
