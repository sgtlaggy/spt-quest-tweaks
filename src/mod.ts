import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IQuestCondition } from "@spt-aki/models/eft/common/tables/IQuest";

import { CONFIG, gunsmithChallengeTargetTypes } from "./config";


class Mod implements IPostDBLoadMod {
    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const log = (msg: string) => logger.info(`[QuestTweaks] ${msg}`);

        const db = container.resolve<DatabaseServer>("DatabaseServer").getTables();
        const locales = db.locales.global;
        const quests = db.templates.quests;

        const [scavType, target, targetSingular, targetPluralSuffix] = gunsmithChallengeTargetTypes[CONFIG.gunsmithChallenge.targetType.toLowerCase()] || [undefined, undefined, undefined, undefined];
        const targetName = `${targetSingular}${CONFIG.gunsmithChallenge.killsRequired > 1 ? targetPluralSuffix : ""}`;

        if (CONFIG.revealAllQuestObjectives) {
            log("Revealing hidden/conditional objectives.");
        }

        if (CONFIG.removeTimeGates) {
            log("Removing time gates from all quests.")
        }

        if (CONFIG.gunsmithChallenge.killsRequired > 0) {
            if (scavType === undefined) {
                log(`${CONFIG.gunsmithChallenge.targetType} is not a valid target type, skipping.\nValid options: ${Object.keys(gunsmithChallengeTargetTypes).join(", ")}`);
                CONFIG.gunsmithChallenge.killsRequired = 0;
            } else {
                log(`Eliminate ${CONFIG.gunsmithChallenge.killsRequired} ${targetName} with each Gunsmith weapon.`);
            }
        }

        for (const quest of Object.values(quests)) {
            if (CONFIG.revealAllQuestObjectives) {
                for (const objective of quest.conditions.AvailableForFinish) {
                    objective.visibilityConditions = [];
                }
            }

            if (CONFIG.removeTimeGates) {
                for (const prereq of quest.conditions.AvailableForStart) {
                    if (prereq.availableAfter) {
                        prereq.availableAfter = 0;
                    }
                }
            }

            if (CONFIG.gunsmithChallenge.killsRequired > 0 && quest.QuestName?.startsWith("Gunsmith")) {
                quest.conditions.AvailableForFinish.filter((cond) => {
                    return cond.conditionType === "WeaponAssembly";
                }).map((weaponCondition) => {
                    const localeKey = `${quest._id} ${weaponCondition.id} challengeElims`;

                    // @ts-ignore                  containsItems undeclared/undocumented
                    const requiredMods: string[] = weaponCondition.containsItems;

                    const elimCondition: IQuestCondition = {
                        id: localeKey,
                        value: CONFIG.gunsmithChallenge.killsRequired,
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
                        locale[cond.id] = `Eliminate ${CONFIG.gunsmithChallenge.killsRequired} ${targetName} with the ${weapon}`;
                    }
                });
            }
        }
    }
}

export const mod = new Mod();
