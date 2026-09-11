using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils.Json.Converters;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;


namespace sgtlaggyQuestTweaks;

public record Config
{
    [JsonPropertyName("QualityOfLife")]
    public required QualityOfLifeConfig QualityOfLife { get; set; }

    [JsonPropertyName("GlobalConditions")]
    public required GlobalConditionsConfig GlobalConditions { get; set; }

    [JsonPropertyName("SpecialCases")]
    public required SpecialCasesConfig SpecialCases { get; set; }

    [JsonPropertyName("exemptQuests")]
    public required HashSet<MongoId> ExemptQuests { get; set; }

    [JsonPropertyName("onlyQuests")]
    public required HashSet<MongoId> OnlyQuests { get; set; }

    [JsonPropertyName("questOverrides")]
    public required Dictionary<MongoId, ConditionsConfig> QuestOverrides { get; set; }

    public bool IsQuestExempt(MongoId questId)
    {
        if ((OnlyQuests.Count > 0) && !OnlyQuests.Contains(questId))
        {
            return true;
        }
        if (ExemptQuests.Contains(questId))
        {
            return true;
        }
        return false;
    }

    public ConditionsConfig GetConditionsWithOverrides(MongoId questId)
    {
        ConditionsConfig conditions = new();

        ConditionsConfig? questOverride;
        QuestOverrides.TryGetValue(questId, out questOverride);

        var properties = typeof(ConditionsConfig).GetProperties()
            .Where((prop) => (prop.GetCustomAttribute(typeof(JsonPropertyNameAttribute)) is not null));
        foreach (var prop in properties)
        {
            if (questOverride is not null)
            {
                var overrideValue = prop.GetValue(questOverride);
                if (overrideValue is not null)
                {
                    prop.SetValue(conditions, overrideValue);
                    continue;
                }
            }

            if (IsQuestExempt(questId))
            {
                prop.SetValue(conditions, GetDefaultValue(prop));
                continue;
            }

            prop.SetValue(conditions, prop.GetValue(GlobalConditions) ?? GetDefaultValue(prop));
        }

        return conditions;
    }

    private static object? GetDefaultValue(PropertyInfo property)
    {
        var type = property.PropertyType;
        if (type == typeof(bool?))
        {
            return false;
        }
        else if (type == typeof(int?))
        {
            return -1;
        }
        return null;
    }

    public double? GetNewObjectiveValue(ConditionsConfig config, string condition, double? original)
    {
        if (original is null)
        {
            return null;
        }

        var absoluteProp = typeof(ConditionsConfig).GetProperty($"{condition}Count")!;
        var percentProp = typeof(ConditionsConfig).GetProperty($"{condition}Percent")!;

        int? absolute;
        int? percent;

        absolute = absoluteProp.GetValue(config) as int?;
        if (absolute >= 0)
        {
            return absolute;
        }

        percent = percentProp.GetValue(config) as int?;
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
}

public record QualityOfLifeConfig
{
    [JsonPropertyName("revealAllQuestObjectives")]
    public bool RevealAllQuestObjectives { get; set; }

    [JsonPropertyName("revealUnknownRewards")]
    public bool RevealUnknownRewards { get; set; }

    [JsonPropertyName("removeTimeGates")]
    public bool RemoveTimeGates { get; set; }
}

public record ConditionsConfig
{
    [JsonPropertyName("removeTarget")]
    public bool? RemoveTarget { get; set; }

    [JsonPropertyName("removeWeapon")]
    public bool? RemoveWeapon { get; set; }

    [JsonPropertyName("removeWeaponMods")]
    public bool? RemoveWeaponMods { get; set; }

    [JsonPropertyName("removeSelfGear")]
    public bool? RemoveSelfGear { get; set; }

    [JsonPropertyName("removeEnemyGear")]
    public bool? RemoveEnemyGear { get; set; }

    [JsonPropertyName("removeSelfHealthEffect")]
    public bool? RemoveSelfHealthEffect { get; set; }

    [JsonPropertyName("removeEnemyHealthEffect")]
    public bool? RemoveEnemyHealthEffect { get; set; }

    [JsonPropertyName("removeBodyPart")]
    public bool? RemoveBodyPart { get; set; }

    [JsonPropertyName("removeDistance")]
    public bool? RemoveDistance { get; set; }

    [JsonPropertyName("removeTime")]
    public bool? RemoveTime { get; set; }

    [JsonPropertyName("removeMap")]
    public bool? RemoveMap { get; set; }

    [JsonPropertyName("removeZone")]
    public bool? RemoveZone { get; set; }

    [JsonPropertyName("removeFindInRaid")]
    public bool? RemoveFindInRaid { get; set; }

    [JsonPropertyName("removeInOneRaid")]
    public bool? RemoveInOneRaid { get; set; }

    [JsonPropertyName("handoverItemCount")]
    public int? HandoverItemCount { get; set; }

    [JsonPropertyName("handoverItemPercent")]
    public int? HandoverItemPercent { get; set; }

    [JsonPropertyName("eliminationCount")]
    public int? EliminationCount { get; set; }

    [JsonPropertyName("eliminationPercent")]
    public int? EliminationPercent { get; set; }

    [JsonIgnore]
    public bool AnyChanged
    {
        get => (RemoveTarget ?? false)
               || (RemoveWeapon ?? false)
               || (RemoveWeaponMods ?? false)
               || (RemoveSelfGear ?? false)
               || (RemoveEnemyGear ?? false)
               || (RemoveSelfHealthEffect ?? false)
               || (RemoveEnemyHealthEffect ?? false)
               || (RemoveBodyPart ?? false)
               || (RemoveDistance ?? false)
               || (RemoveTime ?? false)
               || (RemoveMap ?? false)
               || (RemoveZone ?? false)
               || (RemoveFindInRaid ?? false)
               || (RemoveInOneRaid ?? false)
               || (HandoverItemCount >= 0)
               || (HandoverItemPercent >= 0)
               || (EliminationCount >= 0)
               || (EliminationPercent >= 0);
    }
}

public record GlobalConditionsConfig : ConditionsConfig
{
    [JsonPropertyName("affectRepeatables")]
    public bool AffectRepeatables { get; set; }
}

public record SpecialCasesConfig
{
    [JsonPropertyName("lightkeeperOnlyRequireLevel")]
    public int LightkeeperOnlyRequireLevel { get; set; }

    [JsonPropertyName("tarkovShooterM10")]
    public bool TarkovShooterM10 { get; set; }

    [JsonPropertyName("collectorPrerequisiteBackport")]
    public bool CollectorPrerequisiteBackport { get; set; }
}

public class ConfigRegistration : IOnDIConstruct
{
    public static async Task OnDIConstructAsync(
        IServiceCollection serviceCollection,
        CancellationToken cancellationToken
    )
    {
        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new StringToMongoIdConverter() }
        };

        var configJson = await File.ReadAllTextAsync(Path.Join(modDir, "config.json"), cancellationToken);
        var config = JsonSerializer.Deserialize<Config>(configJson, jsonSerializerOptions)!;

        serviceCollection.AddSingleton(config);
    }
}

[Injectable(InjectionType = InjectionType.Singleton)]
public class ConfigEditorProvider(Config config, ModHelper modHelper) : IConfigEditorConfigProvider
{
    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        var metadata = new ModMetadata();
        var modDir = modHelper.GetAbsolutePathToModFolder();
        yield return ConfigEditorConfigRegistration.Create(
            metadata.ModGuid,
            metadata.Name,
            config,
            Path.Combine(modDir, "config.json")
        );
    }
}
