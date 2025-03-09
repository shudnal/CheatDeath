# Cheat Death
Fatal attacks instead reduce your health to set %. Afterwards you take reduced damage. Triggers once per 10 min. For hardcore or sloppy players.
## Features

Fatal blow calculated as your health reaches 0 no matter what action caused the effect.

You can configure:
* cooldown
* status effect name
* ability to cleanse burning, poison and puke on effect proc
* duration of protection
* health threshold (percent or fixed amount) to which your hp will be saved/restored on proc
* fatal blow message picked from config value

Temporarty protection config:
* damage modifiers type (resistant, very resistant, immune, etc.)
* damage resistance type (blunt, pierce, etc.)
* fall damage modifier and max fall speed
* stamina consumption on jump and run
* max carry weight added
* health over time (how much health will be restored in protection duration)
* health per second (while protection is active)

All config values are server synced.

## Cooldown

Cheat Death ability takes some time to recover. This time is tied to the world where you died. It means player will have different cooldown in different worlds.

There is console command `setcheatdeathcooldown [seconds]` to manually set cooldown.

Cooldown is based on a world time by default. It could be changed to use global real time instead.

## Icon replace

Put file CheatDeath.png next to plugin main file CheatDeath.dll and it will be loaded as status effect icon instead of regular skull icon.

## Installation (manual)
copy CheatDeath.dll to your BepInEx\Plugins\ folder.

## Incompatibility
Mod should be compatible with anything.

## Configurating
The best way to handle configs is [Configuration Manager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/).

Or [Official BepInEx Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/).

## Mirrors
[Nexus](https://www.nexusmods.com/valheim/mods/2854)

## Donation
[Buy Me a Coffee](https://buymeacoffee.com/shudnal)