## Reveal All Quest Objectives

⚠️ **This may lead to completing objectives out of order, Survive and Extract can be completed before the normally prerequisite objectives. Any currently active quests will have their objectives still revealed if the setting is disabled or the mod is removed.**

Reveal all objectives that are hidden by default and only show up after completing other objectives.

One example is [Broadcast - Part 1](https://escapefromtarkov.fandom.com/wiki/Broadcast_-_Part_1), the objective to place the Signal Jammer doesn't appear until you've entered the room.

## Reveal Unknown Quest Rewards

Replace all "Unknown Reward" quest rewards with the actual items.

## Remove Time Gates

Remove waiting periods after some quests like Gunsmith.

## Remove Tedious Conditions

ℹ️ This is similar to [kiki-RemoveTediousQuestConditions](https://forge.sp-tarkov.com/mod/336/kiki-removetediousquestconditions).

The following objective conditions can be removed.
Any marked with 🔃 also will also apply to repeatable quests by defaults.

- 🔃 Elimination target (PMC, scav, boss, etc)
- 🔃 Weapon and mods
- Equipment
- Health/status effects (stun, dehydration)
- 🔃 Body parts
- 🔃 Distance
- Time
- 🔃 Map/location
- Zone
  - Removing zone but not map conditions will expand it to the map.
- 🔃 Item found-in-raid status

An additional setting toggles whether these also apply to repeatable quests.

The `exemptQuests` setting lets you specify a list of quests conditions will *not* be removed from. This should be a list of quest IDs, which can be found by searching the quest name in `SPT_Data/Server/database/locales/global/en.json` and looking for a matching line starting with `"<QUEST ID> name"`. For example, the ID of the dehydration quest "The Survivalist Path - Zhivchik" is `5d25bfd086f77442734d3007`. This can be added to the list like `["5d25bfd086f77442734d3007"]`. To add multiple quests, add a comma between ids like `["...", "..."]`.
Modded quests can also be added. Their quest IDs can be found in some file in their own mod folder in `user/mods` or, if they use VCQL, `user/mods/Virtual's Custom Quest Loader/database/locales/en/THAT_MOD.json`.

## Set Number For Eliminations and Items to Hand Over

These options will set a specific value for all quests to use when requiring kills or item turn-ins, respected by ‘exemptQuests‘ list.

The item setting does not apply to quest items like the Bronze Pocket Watch or keys/keycards.

## Only Require Level to start Lightkeeper

ℹ️ This is the same feature provided by [Lightkeeper Questline Patch](https://forge.sp-tarkov.com/mod/1521/lightkeeper-questline-patch).

This option will remove all the prerequisite quests to start `Network Provider - Part 1` and will only require a specific level. A value of `0` will disable this feature and leave the prerequisites in place.

## Add Sako TRG M10 to Tarkov Shooter 1-6

BSG only added it to 7 and 8, this option makes it work for 1-6 as well.
