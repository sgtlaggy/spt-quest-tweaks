import config from "../config/config.json";

class IConfig {
    revealAllQuestObjectives: boolean
    removeTimeGates: boolean
    addMissingSetupShotguns: boolean
    lightkeeperOnlyRequireLevel: number
}

export const CONFIG: IConfig = config;
