import config from "../config/config.json";

class IGunsmithChallengeConfig {
        killsRequired: number
        targetType: string
}

class IConfig {
    gunsmithChallenge: IGunsmithChallengeConfig
}

export const CONFIG: IConfig = config;
