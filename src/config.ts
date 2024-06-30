import config from "../config/config.json";

class IGunsmithChallengeConfig {
        killsRequired: number
        targetType: string
}

class IConfig {
    revealAllQuestObjectives: boolean
    removeTimeGates: boolean
    gunsmithChallenge: IGunsmithChallengeConfig
}


// targetType: [[ScavType], Faction, SingularName, PluralSuffix]
export const gunsmithChallengeTargetTypes = {
    any: [[], "Any", "target", "s"],
    pmc: [[], "AnyPmc", "PMC operative", "s"],
    usec: [[], "Usec", "USEC PMC operative", "s"],
    bear: [[], "Bear", "BEAR PMC operative", "s"],
    scav: [[], "Savage", "scav", "s"],
    raider: [["pmcBot"], "Savage", "raider", "s"],
    rogue: [["exUsec"], "Savage", "rogue", "s"],
    cultist: [["sectantPriest", "sectantWarrior"], "Savage", "cultist", "s"],
    boss: [[
        "bossBully",            // reshala
        "bossKilla",
        "bossGluhar",
        "bossKojaniy",          // shturman
        "bossSanitar",
        "bossTagilla",
        "bossZryachiy",
        "bossKolontay",
        "bossBoar",             // kaban
        "bossKnight",
        "followerBigPipe",
        "followerBirdEye",
        "sectantPriest"
    ], "Savage", "boss", "es"]
}

export const CONFIG: IConfig = config;
