# 1.0.8
* Updated for the Valheim 1.0.7 release.
* Completed the migration to the standalone ConditionalConfigSync dependency.
* Updated required dependencies to BepInExPack Valheim 5.4.2350 and ConditionalConfigSync 1.0.5.
* Keep damage-protection templates intact when an active protection effect expires.

# 1.0.7
* new config option to set current health percentage to proc Cheat Death only in one-hit situations

# 1.0.6
* configurable post protection period (by default it is less powerful buff for 1 min after protection is over)
* option to apply other status effect when protection is over
* default configs changed to increase healing power of main protection

# 1.0.5
* configurable chance to reproc effect
* option to override vfx for effect proc (vfx should be preconfigured)

# 1.0.4
* patch 0.220.3
* ServerSync updated

# 1.0.3
* fix for NRE on startup
* status effect icon could be replaced by putting CheatDeath.png file next to plugin dll
* new options to set custom text on fatal blow both server synced and client specific

# 1.0.2
* fix for consecutive effect proc not applying protection effects

# 1.0.1
* visual and sound effects on activation

# 1.0.0
* Initial release