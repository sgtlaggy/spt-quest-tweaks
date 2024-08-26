import { DependencyContainer } from "tsyringe";

import { IQuestCondition } from "@spt/models/eft/common/tables/IQuest";
import { QuestTypeEnum } from "@spt/models/enums/QuestTypeEnum";
import { Weapons } from "@spt/models/enums/Weapons";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt/servers/DatabaseServer";

import { CONFIG, gunsmithChallengeTargetTypes } from "./config";


export const IDS = {
    setupQuest: "5c1234c286f77406fa13baeb",
    networkProviderPart1: "625d6ff5ddc94657c21a1625",
    collectorQuest: "5c51aac186f77432ea65c552",
};


class Mod implements IPostDBLoadMod {
    protected getLevelCondition(conditionId: string, level: number): IQuestCondition {
        return {
            id: conditionId,
            conditionType: "Level",
            compareMethod: ">=",
            value: level,
            dynamicLocale: false,
            globalQuestCounterId: "",
            index: 0,
            parentId: "",
            visibilityConditions: [],
            // `target` is not optional, but it should be
            target: ""
        };
    }

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

        if (CONFIG.addMissingSetupShotguns) {
            log("Making the MP-18 & MP-43 sawed off valid for Setup.");
            const setupShotguns = quests[IDS.setupQuest].conditions
                .AvailableForFinish[0].counter.conditions
                .find((cond) => cond.conditionType === "Kills").weapon;
            setupShotguns.push(Weapons.SHOTGUN_762X54R_MP_18);
            setupShotguns.push(Weapons.SHOTGUN_12G_SAWED_OFF);
        }

        const lightkeeperLevel = CONFIG.lightkeeperOnlyRequireLevel;
        if (lightkeeperLevel) {
            log(`Removing Network Provider prerequisites, player must be at least level ${lightkeeperLevel}.`);
            quests[IDS.networkProviderPart1].conditions.AvailableForStart = [
                this.getLevelCondition(`${IDS.networkProviderPart1}_levelCond`, lightkeeperLevel)
            ];
        }

        const collectorLevel = CONFIG.collectorOnlyRequireLevel;
        if (CONFIG.collectorOnlyRequireLevel) {
            log(`Removing Collector prerequisites, player must be at least level ${collectorLevel}`);
            quests[IDS.collectorQuest].conditions.AvailableForStart = [
                this.getLevelCondition(`${IDS.collectorQuest}_levelCond`, collectorLevel)
            ];
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
                    return cond.conditionType === QuestTypeEnum.WEAPON_ASSEMBLY;
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
