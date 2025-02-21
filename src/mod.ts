import { DependencyContainer } from "tsyringe";

import { ILocation } from "@spt/models/eft/common/ILocation";
import { IQuestCondition } from "@spt/models/eft/common/tables/IQuest";
import { ItemTpl } from "@spt/models/enums/ItemTpl";
import { Weapons } from "@spt/models/enums/Weapons";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseService } from "@spt/services/DatabaseService";

import CONFIG from "../config/config.json";


export const IDS = {
    networkProviderPart1: "625d6ff5ddc94657c21a1625",
    claustrophobia: "669fa3979b0ce3feae01a130",
    grenadier: "5c0d190cd09282029f5390d8",
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
            visibilityConditions: []
        };
    }

    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const log = (msg: string) => logger.info(`[QuestTweaks] ${msg}`);

        const db = container.resolve<DatabaseService>("DatabaseService");
        const quests = db.getQuests();
        const enLocale = db.getLocales().global.en;
        const locations = Object.values(db.getLocations())
            .filter((loc) => loc.base?.Enabled)
            .map((loc: ILocation) => [enLocale[loc.base.Id], loc.base.Id, loc.base._Id]);

        if (CONFIG.revealAllQuestObjectives) {
            log("Revealing hidden/conditional objectives.");
        }

        if (CONFIG.revealUnknownRewards) {
            log("Revealing unknown rewards.");
        }

        if (CONFIG.removeTimeGates) {
            log("Removing time gates from all quests.");
        }

        const shouldRemoveSomeConditions = Object.values(CONFIG.removeConditions).some((enabled) => enabled);

        if (CONFIG.removeConditions.target) {
            log("Removing target restrictions from elimination requirements.")
        }

        if (CONFIG.removeConditions.weapon) {
            log("Removing weapon/caliber restrictions from elimination requirements.");
        }

        if (CONFIG.removeConditions.weaponMods) {
            log("Removing weapon mod restrictions from elimination requirements.");
        }

        if (CONFIG.removeConditions.selfGear) {
            log("Removing equipment restrictions from elimination requirements.");
        }

        if (CONFIG.removeConditions.enemyGear) {
            log("Removing enemy equipment restrictions from elimination requirements.");
        }

        if (CONFIG.removeConditions.selfHealthEffect) {
            log("Removing status effects from elimination requirements.");
        }

        if (CONFIG.removeConditions.enemyHealthEffect) {
            log("Removing enemy status effects from elimination requirements.");
        }

        if (CONFIG.removeConditions.bodyPart) {
            log("Removing body part elimniation requirement.")
        }

        if (CONFIG.removeConditions.distance) {
            log("Removing distance elimniation requirement.")
        }

        if (CONFIG.removeConditions.time) {
            log("Removing time from elimniation requirement.")
        }

        if (CONFIG.removeConditions.zone && CONFIG.removeConditions.map) {
            log("Removing zone and map elimination requirements.");
        } else if (CONFIG.removeConditions.zone) {
            log("Replacing zone elimination requirements with location.");
        } else if (CONFIG.removeConditions.map) {
            log("Removing map objective requirements.");
        }

        if (CONFIG.removeConditions.findInRaid) {
            log("Removing found in raid requirement for item hand-ins.");
        }

        const lightkeeperLevel = CONFIG.lightkeeperOnlyRequireLevel;
        if (lightkeeperLevel > 0) {
            log(`Removing Network Provider prerequisites, player must be at least level ${lightkeeperLevel}.`);
            const conditions = quests[IDS.networkProviderPart1].conditions;
            const conditionId = conditions.AvailableForStart[0].id;
            conditions.AvailableForStart = [
                this.getLevelCondition(conditionId, lightkeeperLevel)
            ];
        }

        if (CONFIG.claustrophobiaAA12) {
            log('Adding AA12 to shotguns allowed for Claustrophobia.');
            const conditions = quests[IDS.claustrophobia].conditions;
            conditions.AvailableForFinish[0].counter.conditions[0].weapon.push(
                Weapons.SHOTGUN_12G_AA_12_GEN_1,
                Weapons.SHOTGUN_12G_AA_12_GEN_2
            );
        }

        if (CONFIG.grenadierLaunchers) {
            log('Adding FN40, MSGL, GP-25, and M203 to Grenadier weapons.');
            const conditions = quests[IDS.grenadier].conditions;
            conditions.AvailableForFinish[0].counter.conditions[0].weapon.push(
                Weapons.GRENADELAUNCHER_40X46_FN40GL,
                Weapons.REVOLVER_40X46_MSGL,
                ItemTpl.LAUNCHER_GP25_KOSTYOR_40MM_UNDERBARREL_GRENADE,
                ItemTpl.LAUNCHER_M203_40MM_UNDERBARREL_GRENADE
            );
        }

        for (const quest of Object.values(quests)) {
            const objectives = quest.conditions.AvailableForFinish;

            if (CONFIG.revealAllQuestObjectives) {
                for (const objective of objectives) {
                    objective.visibilityConditions = [];
                }
            }

            if (CONFIG.revealUnknownRewards) {
                for (const reward of quest.rewards.Success) {
                    reward.unknown = false;
                }
            }

            if (CONFIG.removeTimeGates) {
                for (const prereq of quest.conditions.AvailableForStart) {
                    if (prereq.availableAfter) {
                        prereq.availableAfter = 0;
                    }
                }
            }

            if (shouldRemoveSomeConditions) {
                const remove = CONFIG.removeConditions;

                for (const objective of objectives) {
                    if (remove.findInRaid
                        && (objective.conditionType === "HandoverItem"
                            || objective.conditionType === "FindItem")) {
                        objective.onlyFoundInRaid = false;
                    }

                    if (objective.conditionType !== "CounterCreator") {
                        continue;
                    }

                    // convert any zone conditions to map conditions
                    // checks both quest location and objective text to get map
                    if (remove.zone && !remove.map) {
                        const zoneCond = objective.counter.conditions.find(
                            (cond) => cond.conditionType === "InZone");

                        if (zoneCond) {
                            for (const [name, id, mongoId] of locations) {
                                if (quest.location === mongoId || enLocale[objective.id].includes(name)) {
                                    // @ts-ignore   zoneIds is missing from interface
                                    delete zoneCond.zoneIds;
                                    zoneCond.conditionType = "Location";
                                    zoneCond.target = [id];
                                    break;
                                }
                            }
                        }
                    }

                    // removing any straggler InZone requirements that
                    // couldn't be converted to Location above
                    let conditions = objective.counter.conditions.filter(
                        (cond) => {
                            return !((remove.selfHealthEffect && cond.conditionType === "HealthEffect")
                                || (remove.selfGear && cond.conditionType === "Equipment")
                                || (remove.map && cond.conditionType === "Location")
                                || (remove.zone && cond.conditionType === "InZone"));
                        }
                    );

                    objective.counter.conditions = conditions;

                    // make the objective instantly complete if it has no "action"
                    // conditions like kills dehydration for X time
                    const onlyRestrictiveConditions = conditions.every((cond) =>
                        ["Equipment", "Location", "InZone"].includes(cond.conditionType));

                    if (onlyRestrictiveConditions) {
                        objective.value = 0;
                        continue;
                    }

                    const killCond = conditions.find((cond) =>
                        ["Shots", "Kills"].includes(cond.conditionType));
                    if (!killCond) {
                        continue;
                    }

                    if (remove.target) {
                        killCond.savageRole = [];
                        killCond.target = "Any";
                    }

                    if (remove.weapon) {
                        killCond.weapon = [];
                        killCond.weaponCaliber = [];
                    }

                    if (remove.weaponMods) {
                        killCond.weaponModsExclusive = [];
                        killCond.weaponModsInclusive = [];
                    }

                    if (remove.enemyHealthEffect) {
                        killCond.enemyHealthEffects = [];
                    }

                    if (remove.enemyGear) {
                        killCond.enemyEquipmentExclusive = [];
                        killCond.enemyEquipmentInclusive = [];
                    }

                    if (remove.bodyPart) {
                        killCond.bodyPart = [];
                    }

                    if (remove.distance) {
                        killCond.distance = {
                            compareMethod: ">=",
                            value: 0
                        }
                    }

                    if (remove.time) {
                        killCond.daytime = {
                            from: 0,
                            to: 0
                        }
                    }
                }
            }
        }
    }
}

export const mod = new Mod();
