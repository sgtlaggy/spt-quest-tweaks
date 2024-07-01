# sgtlaggy's Quest Tweaks

## Reveal All Quest Objectives

This mod will reveal all objectives that are hidden by default and only show up after completing other objectives.

One example is Broadcast - Part 1, the objective to place the Signal Jammer doesn't appear until you've entered the room.

## Remove Time Gates

Remove waiting periods after some quests like Gunsmith.

## Add Missing Setup Shotguns

The Setup quest requires an MP-series shotgun but this does not include the MP-43 sawed-off double barrel or MP-18 by default.

Rant: The MP-18 is classified as a shotgun in-game due to Russian law classifying all smooth-bore rifles as such. By that logic, the .366 rifles (VPO-209 and VPO-215) should also be classified as shotguns but they are not.

## Gunsmith Challenge

Inspired by [SheefGG](https://www.twitch.tv/SheefGG)'s hardcore gunsmith challenge.

This adds an Elimination objective for each weapon in the Gunsmith questline.
These requirements are rather relaxed and don't *exactly* match the builds.
As a result, ammo weight and stat differences from mods like [Realism](https://hub.sp-tarkov.com/files/file/606-spt-realism-mod/) shouldn't affect viability.
More specifically, it only checks for the correct weapon and any explicitly requested mods. It will not check for presence or absence of a magazine (unless explicitly required) or whether the weapon is folded.

The number and type of target can be changed in the config. Set number to 0 to disable.
Valid target types are `any`, `pmc`, `usec`, `bear`, `scav`, `raider`, `rogue`, `cultist`, and `boss`.
