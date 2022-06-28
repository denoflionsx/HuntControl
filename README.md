# HuntControl
 V Rising mod that makes missions progress when the game is closed

## Info
This is a server side mod for V Rising that allows for missions to progress while the server is offline. This means missions are no longer nearly useless in single player. It also allows missions to be sped up by a multiplier.

This is accomplished by saving a timestamp when the game is shut down. When the mod boots up it checks the saved timestamp against the local machine time and fast forwards any active mission to account for the missing time.

The mission speedup multiplier option works by polling all active missions every 60 seconds and fast forwarding them.

## Configuration
This mod generates a config file in BepInEx's config folder. HuntControl.cfg.

missionsProgressOffline - Keep mission time running even if offline.

missionTimeMultiplier   - Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.