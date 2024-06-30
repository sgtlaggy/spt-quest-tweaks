import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IQuestCondition } from "@spt-aki/models/eft/common/tables/IQuest";

import { killsRequired, targetType } from "../config/config.json";


const targetTypes = {
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
    ], "Savage", "boss", "es"],
}


class Mod implements IPostDBLoadMod {
    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const db = container.resolve<DatabaseServer>("DatabaseServer").getTables();
        const locales = db.locales.global;
        const quests = db.templates.quests;

        const [scavType, target, targetSingular, targetPluralSuffix] = targetTypes[targetType.toLowerCase()] || [undefined, undefined, undefined];
        const targetName = `${targetSingular}${killsRequired > 1 ? targetPluralSuffix : ""}`;

        if (killsRequired <= 0) {
            logger.info("[GunsmithChallenge] Not adding eliminations to gunsmith quests.")
            return;
        } else if (scavType === undefined) {
            throw new Error(`[GunsmithChallenge] ${targetType} is not a valid target type.\nValid options: any, pmc, usec, bear, scav, boss, goons`);
        }

        logger.info(`[GunsmithChallenge] Eliminate ${killsRequired} ${targetName} with each Gunsmith weapon.`);

        for (const quest of Object.values(quests)) {
            if (quest.QuestName?.startsWith("Gunsmith")) {
                quest.conditions.AvailableForFinish.filter((cond) => {
                    return cond.conditionType === "WeaponAssembly";
                }).map((weaponCondition) => {
                    const localeKey = `${quest._id} ${weaponCondition.id} challengeElims`;

                    // @ts-ignore                  containsItems undeclared/undocumented
                    const requiredMods: string[] = weaponCondition.containsItems;

                    const elimCondition: IQuestCondition = {
                        id: localeKey,
                        value: killsRequired,
                        conditionType: "CounterCreator",
                        counter: {
                            id: `${localeKey} counter`,
                            conditions: [
                                {
                                    bodyPart: [],
                                    compareMethod: ">=",
                                    conditionType: "Kills",
                                    daytime: {
                                        from: 0,
                                        to: 0
                                    },
                                    distance: {
                                        compareMethod: ">=",
                                        value: 0
                                    },
                                    dynamicLocale: false,
                                    enemyEquipmentExclusive: [],
                                    enemyEquipmentInclusive: [],
                                    enemyHealthEffects: [],
                                    id: `${localeKey} condition`,
                                    resetOnSessionEnd: false,
                                    savageRole: scavType,
                                    target: target,
                                    value: 1,
                                    weapon: [weaponCondition.target].flat(),
                                    weaponCaliber: [],
                                    weaponModsExclusive: [],
                                    weaponModsInclusive: requiredMods ? [requiredMods] : []
                                }
                            ]
                        },
                        dynamicLocale: false,
                        completeInSeconds: 0,
                        target: []
                    };
                    return elimCondition;
                }).forEach((cond) => {
                    quest.conditions.AvailableForFinish.push(cond);

                    const weaponId = cond.counter.conditions[0].weapon[0];
                    const weapon = locales.en[`${weaponId} ShortName`];
                    for (const locale of Object.values(locales)) {
                        locale[cond.id] = `Eliminate ${killsRequired} ${targetName} with the ${weapon}`;
                    }
                });
            }
        }
    }
}

export const mod = new Mod();
