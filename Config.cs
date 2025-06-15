using System.Text.Json.Serialization;


namespace sgtlaggyQuestTweaks;

public record Config
{
    [JsonPropertyName("revealAllQuestObjectives")]
    public bool RevealAllQuestObjectives { get; set; }

    [JsonPropertyName("revealUnknownRewards")]
    public bool RevealUnknownRewards { get; set; }

    [JsonPropertyName("removeTimeGates")]
    public bool RemoveTimeGates { get; set; }

    [JsonPropertyName("removeConditions")]
    public ConditionsConfig RemoveConditions { get; set; }

    [JsonPropertyName("affectRepeatables")]
    public bool AffectRepeatables { get; set; }

    [JsonPropertyName("exemptQuests")]
    public HashSet<string> ExemptQuests { get; set; }

    [JsonPropertyName("lightkeeperOnlyRequireLevel")]
    public int LightkeeperOnlyRequireLevel { get; set; }

    [JsonPropertyName("tarkovShooterM10")]
    public bool TarkovShooterM10 { get; set; }
}

public record ConditionsConfig
{
    [JsonPropertyName("target")]
    public bool Target { get; set; }

    [JsonPropertyName("weapon")]
    public bool Weapon { get; set; }

    [JsonPropertyName("weaponMods")]
    public bool WeaponMods { get; set; }

    [JsonPropertyName("selfGear")]
    public bool SelfGear { get; set; }

    [JsonPropertyName("enemyGear")]
    public bool EnemyGear { get; set; }

    [JsonPropertyName("selfHealthEffect")]
    public bool SelfHealthEffect { get; set; }

    [JsonPropertyName("enemyHealthEffect")]
    public bool EnemyHealthEffect { get; set; }

    [JsonPropertyName("bodyPart")]
    public bool BodyPart { get; set; }

    [JsonPropertyName("distance")]
    public bool Distance { get; set; }

    [JsonPropertyName("time")]
    public bool Time { get; set; }

    [JsonPropertyName("map")]
    public bool Map { get; set; }

    [JsonPropertyName("zone")]
    public bool Zone { get; set; }

    [JsonPropertyName("findInRaid")]
    public bool FindInRaid { get; set; }

    public bool AnyEnabled
    {
        get => Target
               || Weapon
               || WeaponMods
               || SelfGear
               || EnemyGear
               || SelfHealthEffect
               || EnemyHealthEffect
               || BodyPart
               || Distance
               || Time
               || Map
               || Zone
               || FindInRaid;
    }
}
