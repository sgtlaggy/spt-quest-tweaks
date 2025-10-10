using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Collections;
using SPTarkov.Server.Core.Utils.Json;


namespace sgtlaggyQuestTweaks;

public record Constants
{
    public static string[] TarkovShooter = [
        QuestTpl.THE_TARKOV_SHOOTER_PART_1,
        QuestTpl.THE_TARKOV_SHOOTER_PART_2,
        QuestTpl.THE_TARKOV_SHOOTER_PART_3,
        QuestTpl.THE_TARKOV_SHOOTER_PART_4,
        QuestTpl.THE_TARKOV_SHOOTER_PART_5,
        QuestTpl.THE_TARKOV_SHOOTER_PART_6,
        QuestTpl.THE_TARKOV_SHOOTER_PART_7,
        QuestTpl.THE_TARKOV_SHOOTER_PART_8,
    ];
    public static HashSet<string> KeyClasses = [
        BaseClasses.KEY,
        BaseClasses.KEY_MECHANICAL,
        BaseClasses.KEYCARD
    ];
    public static HashSet<string> HandoverCountItemBlacklist = [
        ItemTpl.RADIOTRANSMITTER_DIGITAL_SECURE_DSP_RADIO_TRANSMITTER,
        ItemTpl.BARTER_KOSA_UAV_ELECTRONIC_JAMMING_DEVICE,
        ItemTpl.INFO_NOTE_WITH_CODE_WORD_VORON,
    ];
}

public record LocationInfo(string Name, string Id, string MongoId);

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class QuestTweaks(
    ISptLogger<QuestTweaks> _logger,
    DatabaseService _db,
    ConfigServer _configServer,
    JsonUtil _json
) : IOnLoad
{
    protected Config? _config;
    protected string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    public Task OnLoad()
    {
        try
        {
            _config = _json.DeserializeFromFile<Config>(System.IO.Path.Join(_modDir, "config.json"));
        }
        catch (JsonException)
        {
            _logger.Error("Invalid config.");
            return Task.CompletedTask;
        }
        if (_config is null)
        {
            _logger.Error("Missing config.");
            return Task.CompletedTask;
        }

        var items = _db.GetItems();
        var quests = _db.GetQuests();
        var enLocale = _db.GetLocales().Global["en"].Value!;

        var locations = _db.GetLocations().GetDictionary().Values
            .Where(loc => loc.Base?.Enabled ?? false)
            .Select(
                (loc) => new LocationInfo(
                    // Terminal/Lab are undefined using ‘.Id’, need to get "proper" name
                    enLocale[loc.Base.Id] ?? enLocale[$"{loc.Base.IdField} Name"],
                    loc.Base.Id,
                    loc.Base.IdField
                )
            );
        // special-case factory night because it’s not enabled and name in locale is "Night Factory"
        var factoryNight = _db.GetLocation(ELocationName.factory4_night.ToString())!.Base;
        locations.Append(new LocationInfo(
            "Factory",
            factoryNight.Id,
            factoryNight.IdField
        ));

        // if (_config.RevealAllQuestObjectives)
        // {
        //     _logger.Info("Revealing hidden/conditional objectives.");
        // }

        // if (_config.RevealUnknownRewards)
        // {
        //     _logger.Info("Revealing unknown rewards.");
        // }

        // if (_config.RemoveTimeGates)
        // {
        //     _logger.Info("Removing time gates from all quests.");
        // }

        // if (_config.RemoveConditions.Target)
        // {
        //     _logger.Info("Removing target restrictions from elimination requirements.");
        // }

        // if (_config.RemoveConditions.Weapon)
        // {
        //     _logger.Info("Removing weapon/caliber restrictions from elimination requirements.");
        // }

        // if (_config.RemoveConditions.WeaponMods)
        // {
        //     _logger.Info("Removing weapon mod restrictions from elimination requirements.");
        // }

        // if (_config.RemoveConditions.SelfGear)
        // {
        //     _logger.Info("Removing equipment restrictions from elimination requirements.");
        // }

        // if (_config.RemoveConditions.EnemyGear)
        // {
        //     _logger.Info("Removing enemy equipment restrictions from elimination requirements.");
        // }

        // if (_config.RemoveConditions.SelfHealthEffect)
        // {
        //     _logger.Info("Removing status effects from elimination requirements.");
        // }

        // if (_config.RemoveConditions.EnemyHealthEffect)
        // {
        //     _logger.Info("Removing enemy status effects from elimination requirements.");
        // }

        // if (_config.RemoveConditions.BodyPart)
        // {
        //     _logger.Info("Removing body part elimniation requirement.");
        // }

        // if (_config.RemoveConditions.Distance)
        // {
        //     _logger.Info("Removing distance elimniation requirement.");
        // }

        // if (_config.RemoveConditions.Time)
        // {
        //     _logger.Info("Removing time from elimniation requirement.");
        // }

        // if (_config.RemoveConditions.Zone && _config.RemoveConditions.Map)
        // {
        //     _logger.Info("Removing zone and map elimination requirements.");
        // }
        // else if (_config.RemoveConditions.Zone)
        // {
        //     _logger.Info("Replacing zone elimination requirements with location.");
        // }
        // else if (_config.RemoveConditions.Map)
        // {
        //     _logger.Info("Removing map objective requirements.");
        // }

        // if (_config.RemoveConditions.FindInRaid)
        // {
        //     _logger.Info("Removing found in raid requirement for item hand-ins.");
        // }

        // if (_config.HandoverItemCount >= 0)
        // {
        //     _logger.Info($"Setting required number of items for hand-over to {_config.HandoverItemCount}.");
        // }

        // if (_config.EliminationCount >= 0)
        // {
        //     _logger.Info($"Setting required number of eliminations to {_config.EliminationCount}.");
        // }

        if (_config.LightkeeperOnlyRequireLevel > 0)
        {
            // _logger.Info($"Removing Network Provider Part 1 prerequisites, making it available at level {_config.LightkeeperOnlyRequireLevel}.");
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
            // _logger.Info("Adding Sako TRG M10 to old Tarkov Shooter quests.");

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

        var remove = _config.RemoveConditions;
        var shouldModifyConditions = remove.AnyEnabled
                                     || _config.HandoverItemCount >= 0
                                     || _config.EliminationCount >= 0;

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

            if (_config.ExemptQuests.Contains(quest.Id))
            {
                continue;
            }

            if (!shouldModifyConditions)
            {
                continue;
            }

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
                    if (_config.HandoverItemCount >= 0
                        && !(item.Properties!.QuestItem == true)
                        && !Constants.KeyClasses.Contains(item.Parent)
                        && !Constants.HandoverCountItemBlacklist.Contains(item.Id))
                    {
                        objective.Value = _config.HandoverItemCount;
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
                            if (quest.Location == loc.MongoId
                                || enLocale[objective.Id].Contains(loc.Name))
                            {
                                if (zoneCond.ConditionType == "InZone")
                                {
                                    zoneCond.Zones = null;
                                    zoneCond.ConditionType = "Location";
                                    zoneCond.Target = new ListOrT<string>([loc.Id], default);
                                }
                                // already replaced, support Factory and Ground Zero variants
                                else
                                {
                                    zoneCond.Target!.List!.Append(loc.Id);
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

                    if (_config.EliminationCount >= 0 && condition.ConditionType == "Kills")
                    {
                        objective.Value = _config.EliminationCount;
                    }

                    if (remove.Target)
                    {
                        condition.SavageRole?.Clear();
                        condition.Target = new ListOrT<string>(default, "Any");
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

        if (!(shouldModifyConditions && _config.AffectRepeatables))
        {
            return Task.CompletedTask;
        }

        var questConfig = _configServer.GetConfig<QuestConfig>();

        foreach (var quest in questConfig.RepeatableQuests)
        {
            if (remove.Map)
            {
                quest.Locations = new Dictionary<ELocationName, List<string>>
                {
                    [ELocationName.any] = ["any"]
                };

                foreach (var exploration in quest.QuestConfig.ExplorationConfig)
                {
                    exploration.SpecificExits.Chance = 0;
                }
            }

            if (remove.FindInRaid)
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

#if DEBUG
        // Dump modified quest database to a file for quick inspection in debug builds.
        var dumpFile = System.IO.Path.Join(_modDir, "dump.json");
        File.WriteAllText(dumpFile, _json.Serialize(quests, true));
#endif

        return Task.CompletedTask;
    }
}

public record ModMetadata : AbstractModMetadata
{
    public override string Name { get; init; } = "sgtlaggy's Quest Tweaks";
    public override string ModGuid { get; init; } = "com.sgtlaggy.questtweaks";
    public override string Author { get; init; } = "sgtlaggy";
    public override SemanticVersioning.Version Version { get; init; } = new(Assembly.GetExecutingAssembly().GetName().Version!.ToString(3));
    public override string? Url { get; init; } = "https://github.com/sgtlaggy/spt-quest-tweaks";
    public override string License { get; init; } = "MIT";
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override bool? IsBundleMod { get; init; }
}
