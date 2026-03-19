# Once Upon an Archipelago

An [Archipelago](https://archipelago.gg) randomizer for Once Upon a KATAMARI.

## Randomization
The goal is to collect a certain number of Planets to unlock the final level "That Hole...". Plug the hole in the Cosmos Scroll to then goal the game.
Depending on your settings, planets are obtained by clearing levels, players finding them in the multiworld, or both.  
  
The following items are shuffled into the item pool:
- Level Unlocks
- Cousins
- Presents
- Planets
- Stardust (filler)

The following locations are checks:
- Level Clears
- Cousins
- Presents
- Crowns

## Mod Installation
1. Download the Bleeding Edge build of [BepInEx 6](https://builds.bepinex.dev/projects/bepinex_be).
Choose **"BepInEx Unity (IL2CPP) for Windows (x64) game"**. You should still select the Windows version, even if you're not using Windows.
2. Extract the contents into the game's root folder. By default, this is `C:\Program Files (x86)\Steam\steamapps\common\OnceUponaKATAMARI`.
3. Download the Archipelago plugin from [releases](https://github.com/ItsSeafoamy/Once-Upon-an-Archipelago/releases), and extract its contents into `BepInEx/plugins`.
4. For **Unix** (Linux, Mac, SteamOS) users only: add `WINEDLLOVERRIDES="winhttp.dll=n,b" %command%` to the launch options on Steam, under properties.
5. Start Once Upon a Katamari to generate the necessary configuration files, and then close the game.

## Joining a Multiworld
1. Open `BepInEx/config/OnceUponAnArchipelago.cfg`, and enter the correct server address, slot name and, if applicable,
password.
2. Make sure the Archipelago server is running before launching the game. 
3. Launch Once Upon a Katamari.
4. The game will automatically connect on startup. If successful, you will see a message saying "Archipelago: Connected"
at the bottom of the screen.
5. If you see a red "Archipelago: Not Connected", close the game, check your settings and that the server is running, and relaunch the game.
There is currently no way to reconnect whilst the game is running.
6. Start a brand new save file.

## Additional Info
- The apworld can be found in [releases](https://github.com/ItsSeafoamy/Once-Upon-an-Archipelago/releases), alongside a default YAML. You can also use the Options Creator in the
Archipelago Client to create your player YAML.
- The source code for the apworld can be found [here](https://github.com/ItsSeafoamy/Archipelago-OUAKatamari/tree/ouakatamari/worlds/ouakatamari).
- A setting exists in the plugin's config called "easyFinale", which makes the final level "That Hole..." clearable by only rolling up one object. This bypasses the vanilla requirement
to clear lots of levels first. This setting is enabled by default, and logic assumes it remains enabled.
- In vanilla, only one cousin appears at a time in a stage, requiring the player to play through the stage multiple times. In Archipelago, all cousins will appear in the same playthrough.

## Known Issues
- Sometimes, when levels are unlocked, they can get stuck in the sky. This is just a visual bug, you can still play the level by going to the location the level should be. 
This can also be fixed by reloading the scene (by selecting a stage in this era using the Select Scroll).
- The game cannot (re)connect to the server after launch. You must restart the game to (re)connect.
