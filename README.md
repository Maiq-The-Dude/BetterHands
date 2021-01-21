# BetterHands
Mod for H3VR to recolor controller geo, resize interaction spheres, and add mag palming.

## Features
All features listed below are optional and configured via `mods/configs/betterhands.cfg`
- Recolor controller geo
- Recolor interaction spheres
- Resize individual interaction spheres
- Enable mag palming

## Mag Palming
Mag palming allows you to carry two small sized magazines in one hand. To do this click the trigger when holding a magazine in your hand.

### Mag Palming Configs
Configs can be edited mid-game, but will require a scene reload to take effect.
- Change mag palm pose position and rotation
- Change click pressure required to mag palm
- Change priority of Grabbity Gloves vs mag palming
- Change palmable mag size limit. *Greater than `Medium` will disable TNH score submission*
- Toggle `CursedPalms` which allows any interactive object to be palmed. Use at your own risk, bugs stemming from this will not be investigated. *Disables TNH score submission*
- Rebind mag palm key
  - AX/BY buttons
  - Grip
  - Joystick
  - Touchpad directional taps
  - Touchpad directional clicks
  - Trigger (default)

## Installation
Place `betterhands.deli` into `h3vr/mods` folder.

## Uninstallation
Delete `betterhands.deli` in the `h3vr/mods` folder and `betterhands.cfg` in the `h3vr/mods/configs` folder.
