# HKMP.Timer

A synchronized timer for Hollow Knight multiplayer via HKMP.

## Features
* Shared timer/stopwatch for all players connected to the host.
* Configurable keybinds for toggling the timer, checking status, and showing or editing it.
* Real-time tracking of the current status on your screen.
* Dual operating modes: standard Countdown Timer or Stopwatch.
* 8 different interface color variants.
* Configurable timer position and size.

## Keybinds
Default keybinds (all can be changed in Options → HKMP.Timer):
* `F8` — Show/hide the timer; hold to enter position and size editing mode.
* `F9` — Start the timer or stopwatch if stopped; stop it if running. *Host/admin only.*
* `F7` — Display the current timer or stopwatch status in chat. *Available to all players.*

To leave an action unbound, start assigning its key and press `Esc`.

## Chat Commands
The existing commands remain available to authorized players and the host:
* `/timer start` — Starts the countdown or stopwatch depending on the active mode.
* `/timer stop` — Forcefully stops the current countdown or stopwatch.
* `/timer time <seconds>` — Sets the timer duration in seconds. *Unavailable while in stopwatch mode.*
* `/timer status` — Displays the current working status of the timer or stopwatch in chat.

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
* **[Events HK on Telegram](https://t.me/Events_HK)**

> *A portion of this project's code was optimized and generated using AI. The final codebase is fully maintained, integrated, and thoroughly tested by the project author.*
