# BetterHands
[![version](https://img.shields.io/github/v/release/Maiq-The-Dude/BetterHands?&label=version&style=flat-square)](https://github.com/Maiq-The-Dude/BetterHands/releases/latest) [![discord](https://img.shields.io/discord/777351065950879744?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2&style=flat-square)](https://discord.gg/g8xeFyt42j)

A mod to recolor controller geo, resize interaction spheres, and add mag palming.
All features are optional and configured via the config file. Configs can be edited mid-game, but will require a scene reload to take effect.

## Mag Palming
Mag palming allows you to carry two small sized magazines in one hand. To do this click the trigger when holding a magazine in your hand.

### Mag Palming Configs
- Change mag palm pose position and rotation
- Change click pressure required to mag palm when using touchpad click keybinds
- Change priority of Grabbity Gloves vs mag palming when Grabbity and mag palming share the same input
- `Interactable` allows palmed items to be directly grabbed by the other hand
- `CollisionPrevention` to alleviate physics clanking by disabling palmed item collision above a set velocity
- `EasyPalmLoading` to enable Easy Mag Loading on only the palmed item. Not necessary if Easy Mag Loading is enabled via the ingame options panel already
- Change palmable mag size limit. **Greater than `Medium` will disable TNH score submission**
- `CursedPalms` allows any interactive object to be palmed. Use at your own risk, bugs stemming from this will rarely be investigated. **Disables TNH score submission**
- Rebind mag palm key
  - AX/BY buttons
  - Grip
  - Joystick
  - Touchpad directional taps
  - Touchpad directional clicks
  - Trigger (default)

## Hand Customization
- Recolor controller geo
- Recolor interaction spheres
- Resize individual interaction area radius
- Resize individual interaction visual spheres

## Manual Installation
Requires
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases)
- [Sodalite](https://github.com/H3VR-Modding/Sodalite/releases)


Download and open [BetterHands.zip](https://github.com/Maiq-The-Dude/BetterHands/releases/latest), then drag `BetterHands.dll` within into the `h3vr/bepinex/plugins/ directory`
