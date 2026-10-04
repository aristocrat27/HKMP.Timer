# HKMP.Timer

A synchronized timer for Hollow Knight multiplayer via HKMP.

## Features
* Shared timer/stopwatch (as setting in menu) for all players connected to the host.
* Start, stop, and change duration via chat commands.
* Real-time tracking of the current status on your screen.
* Dual operating modes: standard Countdown Timer or Stopwatch.
* 8 different interface color variants.
* Customizable keybind.
* Ability to change the layout's location and size by holding the designated keybind.

## Chat Commands
* `/timer start` — Starts the countdown or stopwatch depending on the active mode. *Available only for the admin/host.*
* `/timer stop` — Forcefully stops the current countdown or stopwatch. *Available only for the admin/host.*
* `/timer time <seconds>` — Sets the timer duration in seconds. *Available only for the admin/host. This command is completely disabled while in stopwatch mode.*
* `/timer status` — Displays the current working status of the timer or stopwatch in chat. *Available for all players.*

*Note: You can use the short alias `/tm` instead of the full `/timer` command.*

---

## Time Display & Statuses
The remaining or elapsed time is displayed on the screen in `MM:SS` format.  
When the countdown time completely expires, a notification is displayed on the screen:
```text
TIME'S UP
```

---

## Community & Credits
`HKMP.Timer` is a proud part of the official RU Hollow Knight PvP-Events Community.  
Announcements, tournaments, custom PvP modes, and additional info:
* **[Events HK on Telegram](https://t.me)**

> *A portion of this project's code was optimized and generated using AI. The final codebase is fully maintained, integrated, and thoroughly tested by the project author.*
