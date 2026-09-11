⚠️ Removing this mod should be safe but there may be lingering effects applied to in-progress quests.

# {.tabset}

## QOL Settings

### Reveal All Quest Objectives

⚠️ **This may lead to completing objectives out of order, Survive and Extract can be completed before the normally prerequisite objectives. Any currently active quests will have their objectives still revealed if the setting is disabled or mod is removed.**

Reveal all objectives that are hidden by default and only show up after completing other objectives.

One example is [Broadcast - Part 1](https://escapefromtarkov.fandom.com/wiki/Broadcast_-_Part_1), the objective to place the Signal Jammer doesn't appear until you've entered the room.

### Reveal Unknown Quest Rewards

Replace all "Unknown Reward" quest rewards with the actual items.

### Remove Time Gates

Remove waiting periods after some quests like Gunsmith.

## Conditions

### Remove Tedious Conditions

ℹ️ This is similar to [kiki-RemoveTediousQuestConditions](https://forge.sp-tarkov.com/mod/336/kiki-removetediousquestconditions) ([github](https://github.com/kieran-boyle/Mods/tree/master/Kiki-RemoveTediousQuestConditions)).

The following objective conditions can be removed.
Any marked with 🔃 also will also apply to repeatable quests by default.

- 🔃 Elimination target (PMC, scav, boss, etc)
- 🔃 Weapon and mods
- Equipment
- Health/status effects (stun, dehydration)
- 🔃 Body parts
- 🔃 Distance
- Time
- Map/location
- Zone
  - Removing zone but not map conditions will expand it to the map.
- 🔃 Item found-in-raid status
- In One Raid

In overrides a value of `true` will remove the condition, `false` will keep the default condition, and `null` or omitting will use the global value.

An additional setting toggles whether these also apply to repeatable quests.

### Set Number For Eliminations and Items to Hand Over

These options will set a percent of original value or flat value across the board for all quests to use when requiring kills or item turn-ins, respected by ‘exemptQuests‘ list.

Negative values indicate not to change the value in global settings and overrides. `null` or omitting in an override will use the global value.

The item setting does not apply to quest items like the Bronze Pocket Watch or keys/keycards.

### Overrides

Each of these options can be overridden for individual quests using the `questOverrides` setting.
It should be in the format `{"questId": {"option": true, "otherOption": false}}`, using the option names in `removeConditions` for reference. These will always be handled regardless of `onlyQuests` and `exemptQuests`.
As an example, to backport the expansion of [Forester's Duty](https://escapefromtarkov.fandom.com/wiki/Forester%27s_Duty), you could set `{"66ab9da7eb102b9bcd08591c": {"removeZone": true}}` and this would leave other zones like Capturing Outposts intact.
The `onlyQuests` setting lets you specify an exclusive list of quests that will be modified.
The `exemptQuests` setting lets you specify a list of quests that will be skipped entirely.

Quest IDs for these settings can be found using [Tarkynator](https://tarkynator.com/quests?scope=global). For example, the ID of the dehydration quest "The Survivalist Path - Zhivchik" is `5d25bfd086f77442734d3007`. This can be added to lists like `["5d25bfd086f77442734d3007"]`. To add multiple quests, add a comma between IDs like `["...", "..."]`.
Modded quests can also be added. Their quest IDs can be found in some file in their own mod folder in `user/mods` or, if they use VCQL, `user/mods/Virtual's Custom Quest Loader/database/locales/en/THAT_MOD.json`.
To set overrides for repeatable quests use `615ffc701c97c768137e719b` for PMC dailies, `618035d38012292db3081bf0` for weeklies, and `62825ef60e88d037dc1eb426` for scav dailies.

## Special Cases

### Only Require Level to start Lightkeeper

ℹ️ This is the same feature provided by [Lightkeeper Questline Patch](https://forge.sp-tarkov.com/mod/1521/lightkeeper-questline-patch) ([github](https://github.com/aadithpm/spt-lightkeeper)).

This option will remove all the prerequisite quests to start `Network Provider - Part 1` and will only require a specific level. A value of `0` will disable this feature and leave the prerequisites in place.

### Add Sako TRG M10 to Tarkov Shooter 1-6

BSG only added it to 7 and 8, this option makes it work for 1-6 as well.

### Backport Collector Prerequisites from EFT 1.1.0.0

This will completely replace the prerequisites, so is incompatible with other mods such as [Start Collector Early](https://forge.sp-tarkov.com/mod/1675/start-collector-early) ([github](https://github.com/m-barneto/StartCollectorEarly)) but should be compatible with [Updated collector quest and streamer case](https://forge.sp-tarkov.com/mod/2615/updated-collector-quest-and-streamer-case-eft-10-backport) ([github](https://github.com/GarfieldMD/CollectorBackportPatch)).

New prerequisites:
- Loyalty level 4👑 Prapor
- Loyalty level 4👑 Therapist
- Loyalty level 4👑 Skier
- Loyalty level 4👑 Peacekeeper
- Loyalty level 4👑 Mechanic
- Loyalty level 4👑 Ragman
- Loyalty level 4👑 Jaeger
- Fence reputation 3+
- PMC Level 40
- A Shooter Born in Heaven
- The Tarkov Shooter - Part 4
- Sew It Good - Part 4
- Chemical - Part 4 OR Big Customer OR Out of Curiosity
