# Support
I played this game for like a week and got bored of it. As such I don't support this mod anymore. If anyone wants to continue it feel free to fork.

# HuntControl
 V Rising mod that makes missions progress when the game is closed

## Info
This is a server side mod for V Rising that adds the following features:

* Mission and Injury timers progress while the game is closed.
* Mission and Injury timers are configurable.

This is accomplished by saving a timestamp when the game is shut down. When the mod boots up it checks the saved timestamp against the local machine time and fast forwards any active mission or injury to account for the missing time.

The speedup multiplier options works by polling all active missions/injuries every 60 seconds and fast forwarding them.

## Configuration
This mod generates a config file in BepInEx's config folder. HuntControl.cfg.

missionsProgressOffline - Keep mission time running even if offline.

missionTimeMultiplier   - Multiply how fast missions progress. Example: 2 = 24h becomes 12h. Updates in bulk once a minute.

injuriesProgressOffline - Let injuries heal while offline.

injuryTimeMultiplier - Speed up injury healing. 0 = disabled. 2 = 2x speed, etc.