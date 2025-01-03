import { DependencyContainer } from "tsyringe";

import { ILocation } from "@spt/models/eft/common/ILocation";
import { IQuestCondition } from "@spt/models/eft/common/tables/IQuest";
import { Weapons } from "@spt/models/enums/Weapons";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseService } from "@spt/services/DatabaseService";

import CONFIG from "../config/config.json";


export const IDS = {
    setupQuest: "5c1234c286f77406fa13baeb",
    networkProviderPart1: "625d6ff5ddc94657c21a1625",
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
            log("Removing weapon/mod/caliber restrictions from elimination requirements.");
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

        if (CONFIG.removeConditions.zone) {
            log("Removing specific areas from elimination requirements.");
        }

        if (CONFIG.removeConditions.map) {
            log("Removing map restrictions from objective requirements.");
        }

        if (CONFIG.removeConditions.findInRaid) {
            log("Removing found in raid requirement for item hand-ins.");
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
        if (lightkeeperLevel > 0) {
            log(`Removing Network Provider prerequisites, player must be at least level ${lightkeeperLevel}.`);
            const conditions = quests[IDS.networkProviderPart1].conditions;
            const conditionId = conditions.AvailableForStart[0].id;
            conditions.AvailableForStart = [
                this.getLevelCondition(conditionId, lightkeeperLevel)
            ];
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

                    const killCond = conditions.find((cond) => cond.conditionType === "Kills");
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
                }
            }
        }
    }
}

export const mod = new Mod();
