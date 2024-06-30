import config from "../config/config.json";

class IGunsmithChallengeConfig {
        killsRequired: number
        targetType: string
}

class IConfig {
    gunsmithChallenge: IGunsmithChallengeConfig
    revealAllQuestObjectives: boolean
}

export const CONFIG: IConfig = config;
