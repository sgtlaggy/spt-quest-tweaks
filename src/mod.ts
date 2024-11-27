import { DependencyContainer } from "tsyringe";

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

        if (CONFIG.revealAllQuestObjectives) {
            log("Revealing hidden/conditional objectives.");
        }

        if (CONFIG.revealUnknownRewards) {
            log("Revealing unknown rewards.");
        }

        if (CONFIG.removeTimeGates) {
            log("Removing time gates from all quests.");
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
            const conditions = quests[IDS.networkProviderPart1].conditions;
            const conditionId = conditions.AvailableForStart[0].id;
            conditions.AvailableForStart = [
                this.getLevelCondition(conditionId, lightkeeperLevel)
            ];
        }

        for (const quest of Object.values(quests)) {
            if (CONFIG.revealAllQuestObjectives) {
                for (const objective of quest.conditions.AvailableForFinish) {
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
        }
    }
}

export const mod = new Mod();
