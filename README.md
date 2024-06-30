# Gunsmith Challenge

Inspired by [SheefGG](https://www.twitch.tv/SheefGG)'s hardcore gunsmith challenge.

This adds an Elimination objective for each weapon in the Gunsmith questline.
These requirements are rather relaxed and don't *exactly* match the builds.
As a result, ammo weight and stat differences from mods like [Realism](https://hub.sp-tarkov.com/files/file/606-spt-realism-mod/) shouldn't affect viability.
More specifically, it only checks for the correct weapon and any explicitly requested mods. It will not check for presence or absence of a magazine (unless explicitly required) or whether the weapon is folded.

5 PMC kills are required by default. The number and type of target can be changed in `config/config.json`.
Valid target types are `any`, `pmc`, `usec`, `bear`, `scav`, `raider`, `rogue`, `cultist`, and `boss`.
