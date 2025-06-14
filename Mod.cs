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
    public static readonly string NetworkProviderPart1 = "625d6ff5ddc94657c21a1625";
    public static readonly string[] TarkovShooter = [
        "5bc4776586f774512d07cf05",
        "5bc479e586f7747f376c7da3",
        "5bc47dbf86f7741ee74e93b9",
        "5bc480a686f7741af0342e29",
        "5bc4826c86f774106d22d88b",
        "5bc4836986f7740c0152911c"
    ];
}

public record LocationInfo
{
    public string Name;
    public string Id;
    public string MongoId;
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class QuestTweaks(
    ISptLogger<QuestTweaks> _logger,
    DatabaseService _db,
    ConfigServer _configServer,
    JsonUtil _json
) : IOnLoad
{
    protected Config _config;
    protected string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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

        var quests = _db.GetQuests();
        var enLocale = _db.GetLocales().Global["en"].Value;
        var locations = _db.GetLocations().GetDictionary().Values
            .Where(loc => loc.Base?.Enabled ?? false)
            .Select(
                (loc, index) => new LocationInfo
                {
                    Name = enLocale[loc.Base.Id],
                    Id = loc.Base.Id,
                    MongoId = loc.Base.IdField
                }
            );

        if (_config.RevealAllQuestObjectives)
        {
            _logger.Info("Revealing hidden/conditional objectives.");
        }

        if (_config.RevealUnknownRewards)
        {
            _logger.Info("Revealing unknown rewards.");
        }

        if (_config.RemoveTimeGates)
        {
            _logger.Info("Removing time gates from all quests.");
        }

        if (_config.RemoveConditions.Target)
        {
            _logger.Info("Removing target restrictions from elimination requirements.");
        }

        if (_config.RemoveConditions.Weapon)
        {
            _logger.Info("Removing weapon/caliber restrictions from elimination requirements.");
        }

        if (_config.RemoveConditions.WeaponMods)
        {
            _logger.Info("Removing weapon mod restrictions from elimination requirements.");
        }

        if (_config.RemoveConditions.SelfGear)
        {
            _logger.Info("Removing equipment restrictions from elimination requirements.");
        }

        if (_config.RemoveConditions.EnemyGear)
        {
            _logger.Info("Removing enemy equipment restrictions from elimination requirements.");
        }

        if (_config.RemoveConditions.SelfHealthEffect)
        {
            _logger.Info("Removing status effects from elimination requirements.");
        }

        if (_config.RemoveConditions.EnemyHealthEffect)
        {
            _logger.Info("Removing enemy status effects from elimination requirements.");
        }

        if (_config.RemoveConditions.BodyPart)
        {
            _logger.Info("Removing body part elimniation requirement.");
        }

        if (_config.RemoveConditions.Distance)
        {
            _logger.Info("Removing distance elimniation requirement.");
        }

        if (_config.RemoveConditions.Time)
        {
            _logger.Info("Removing time from elimniation requirement.");
        }

        if (_config.RemoveConditions.Zone && _config.RemoveConditions.Map)
        {
            _logger.Info("Removing zone and map elimination requirements.");
        }
        else if (_config.RemoveConditions.Zone)
        {
            _logger.Info("Replacing zone elimination requirements with location.");
        }
        else if (_config.RemoveConditions.Map)
        {
            _logger.Info("Removing map objective requirements.");
        }

        if (_config.RemoveConditions.FindInRaid)
        {
            _logger.Info("Removing found in raid requirement for item hand-ins.");
        }

        if (_config.LightkeeperOnlyRequireLevel > 0)
        {
            _logger.Info($"Removing Network Provider Part 1 prerequisites, making it available at level {_config.LightkeeperOnlyRequireLevel}.");
            var conditions = quests[Constants.NetworkProviderPart1].Conditions.AvailableForStart;
            var reuseId = conditions[0].Id;
            conditions.Clear();
            conditions.Add(new QuestCondition
            {
                Id = reuseId,
                ConditionType = "Level",
                CompareMethod = ">=",
                Value = _config.LightkeeperOnlyRequireLevel,
                // Index = 0,
                // DynamicLocale = false,
                // GlobalQuestCounterId = "",
                // ParentId = "",
                // VisibilityConditions = []
            });
        }

        if (_config.TarkovShooterM10)
        {
            _logger.Info("Adding Sako TRG M10 to Tarkov Shooter 1-6.");

            foreach (var questId in Constants.TarkovShooter)
            {
                var conditions = quests[questId].Conditions.AvailableForFinish;
                foreach (var counter in conditions)
                {
                    if (counter.ConditionType != "CounterCreator")
                    {
                        continue;
                    }

                    foreach (var cond in counter.Counter.Conditions)
                    {
                        if (cond.ConditionType != "Kills" && cond.ConditionType != "Shots")
                        {
                            continue;
                        }

                        cond.Weapon.Add(ItemTpl.SNIPERRIFLE_SAKO_TRG_M10_338_LM_BOLTACTION_SNIPER_RIFLE);
                    }
                }
            }
        }

        var remove = _config.RemoveConditions;
        var shouldRemoveConditions = remove.AnyEnabled;

        foreach (var quest in quests.Values)
        {
            var objectives = quest.Conditions.AvailableForFinish;

            if (_config.RevealAllQuestObjectives)
            {
                foreach (var objective in objectives)
                {
                    objective.VisibilityConditions.Clear();
                }
            }

            if (_config.RevealUnknownRewards)
            {
                foreach (var reward in quest.Rewards.Success)
                {
                    reward.Unknown = false;
                }
            }

            if (_config.RemoveTimeGates)
            {
                foreach (var prereq in quest.Conditions.AvailableForStart)
                {
                    if (prereq.AvailableAfter is not null)
                    {
                        prereq.AvailableAfter = 0;
                    }
                }
            }

            if (!shouldRemoveConditions)
            {
                continue;
            }

            foreach (var objective in objectives)
            {
                if (remove.FindInRaid
                    && (objective.ConditionType == "HandoverItem"
                        || objective.ConditionType == "FindItem"))
                {
                    objective.OnlyFoundInRaid = false;
                }

                if (objective.ConditionType != "CounterCreator")
                {
                    continue;
                }

                if (remove.Zone && !remove.Map)
                {
                    foreach (var cond in objective.Counter.Conditions)
                    {
                        if (cond.ConditionType != "InZone")
                        {
                            continue;
                        }

                        foreach (var loc in locations)
                        {
                            if (quest.Location == loc.MongoId
                                || enLocale[objective.Id].Contains(loc.Name))
                            {
                                cond.Zones = null;
                                cond.ConditionType = "Location";
                                cond.Target = new ListOrT<string>(null, loc.Id);
                                break;
                            }
                        }
                    }
                }

                objective.Counter.Conditions.RemoveAll(cond =>
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

                foreach (var cond in objective.Counter.Conditions)
                {
                    if (cond.ConditionType != "Shots" && cond.ConditionType != "Kills")
                    {
                        continue;
                    }

                    if (remove.Target)
                    {
                        cond.SavageRole.Clear();
                        cond.Target = new ListOrT<string>(null, "Any");
                    }

                    if (remove.Weapon)
                    {
                        cond.Weapon.Clear();
                        cond.WeaponCaliber.Clear();
                    }

                    if (remove.WeaponMods)
                    {
                        cond.WeaponModsExclusive.Clear();
                        cond.WeaponModsInclusive.Clear();
                    }

                    if (remove.EnemyHealthEffect)
                    {
                        cond.EnemyHealthEffects.Clear();
                    }

                    if (remove.EnemyGear)
                    {
                        cond.EnemyEquipmentExclusive.Clear();
                        cond.EnemyEquipmentInclusive.Clear();
                    }

                    if (remove.BodyPart)
                    {
                        cond.BodyPart.Clear();
                    }

                    if (remove.Distance)
                    {
                        cond.Distance = new CounterConditionDistance
                        {
                            CompareMethod = ">=",
                            Value = 0
                        };
                    }

                    if (remove.Time)
                    {
                        cond.Daytime = new DaytimeCounter
                        {
                            From = 0,
                            To = 0
                        };
                    }
                }
            }
        }

        if (!(shouldRemoveConditions && _config.AffectRepeatables))
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

                if (quest.QuestConfig.Exploration is not null)
                {
                    quest.QuestConfig.Exploration.SpecificExits.Probability = 0;
                }
            }

            if (remove.FindInRaid && (quest.QuestConfig.Completion is not null))
            {
                quest.QuestConfig.Completion.RequiredItemsAreFiR = false;
            }

            var elims = quest.QuestConfig.Elimination;
            if (elims is not null)
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
                    elim.WeaponCategoryRequirementProbability = 0;
                    elim.WeaponRequirementProbability = 0;
                }

                if (remove.BodyPart)
                {
                    elim.BodyPartProbability = 0;
                }

                if (remove.Distance)
                {
                    elim.DistanceProbability = 0;
                }
            }
        }
        return Task.CompletedTask;
    }
}

public record ModMetadata : AbstractModMetadata
{
    public override string Name { get; set; } = "sgtlaggy's Quest Tweaks";
    public override string Author { get; set; } = "sgtlaggy";
    public override string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public override string Url { get; set; } = "https://github.com/sgtlaggy/spt-quest-tweaks";
    public override string Licence { get; set; } = "MIT";
    public override string SptVersion { get; set; } = "~4.0.0";
    public override List<string> Contributors { get; set; }
    public override List<string> LoadBefore { get; set; }
    public override List<string> LoadAfter { get; set; }
    public override List<string> Incompatibilities { get; set; }
    public override Dictionary<string, string> ModDependencies { get; set; }
    public override bool? IsBundleMod { get; set; }
}
