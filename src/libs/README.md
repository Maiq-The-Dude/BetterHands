# To update MMHOOK_Assembly-CSharp.dll
1. Download and extract the net35 binaries [the latest MonoMod release](https://github.com/MonoMod/MonoMod/releases/latest) to a temporary directory
2. Run `MonoMod.RuntimeDetour.HookGen.exe --private [PATH TO ORIGINAL Assembly-CSharp.dll]` within the temporary directory
