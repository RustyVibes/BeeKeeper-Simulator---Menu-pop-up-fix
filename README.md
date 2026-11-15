### Main Menu Popup Fix

Makes the main menu's popups mutually exclusive. The game's buttons only ever open their own popup, so
**Play Game**, **Settings** and **Join Game** can currently stack on top of each other. This plugin
closes the others as soon as one opens.

| You open | What closes |
| --- | --- |
| Play Game (Save Slots) | Settings and Join Game popups |
| Settings | Save Slots and Join Game popups |
| Join Game | Save Slots and Settings popups |

- No configuration and no game files changed; it only runs while the main menu exists.
- Keeps the Settings and Join Game button highlights in sync. Closing either popup with its own Back
  button clears the menu highlight instead of leaving the button looking selected.
- The Save Slots popup has no menu toggle, so it is simply hidden when another popup opens.
- Each closure is logged to `BepInEx/LogOutput.log`
  (`Closed 'LoadGamePopup' because 'SettingsPopup (1)' opened.`).

### Install

Copy `MainMenuPopupFix.dll` to `BepInEx/plugins/MainMenuPopupFix/MainMenuPopupFix.dll`. It loads with the
game and needs no restart beyond launching it.

### Uninstall

Delete `BepInEx/plugins/MainMenuPopupFix/`. There is no config file to remove.
