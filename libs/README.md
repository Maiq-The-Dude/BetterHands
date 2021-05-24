# To update these libraries
## Assembly-CSharp_publicized.dll
1. Download and extract [Reinms/Stubber-Publicizer](https://github.com/Reinms/Stubber-Publicizer)
2. Drag `Assembly-CSharp.dll` onto the publicizer, and follow the prompts to both stub and publicize
## Assembly-CSharp-firstpass.dll
1. Do the same as above, but skip publicizing
## MMHOOK_Assembly-CSharp.dll
1. Download and extract the net35 binaries [the latest MonoMod release](https://github.com/MonoMod/MonoMod/releases/latest) to a temporary directory
2. Run `MonoMod.RuntimeDetour.HookGen.exe --private [PATH TO ORIGINAL Assembly-CSharp.dll]` within the temporary directory
