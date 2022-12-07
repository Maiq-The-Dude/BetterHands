using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class CustomizationQBSlots
	{
		public ConfigEntry<bool> aEnable { get; }
		public ConfigEntry<Color> BaseColor { get; }
		public ConfigEntry<float> BaseIntensity { get; }
		public ConfigEntry<Color> HarnessColor { get; }
		public ConfigEntry<float> HarnessIntensity { get; }
		public ConfigEntry<Color> SpawnlockColor { get; }
		public ConfigEntry<float> SpawnlockIntensity { get; }

		public CustomizationQBSlots(ConfigFile config, string section)
		{
			aEnable = config.Bind(section, nameof(aEnable), true, "Allows recoloring of the qbslot geo");

			var a = new Vector4(1f, 1f, 1f, 1f);
			BaseColor = config.Bind(section, nameof(BaseColor), (Color)a, "Color of quickbelts (RGBA)");
			BaseIntensity = config.Bind(section, nameof(BaseIntensity), 1f, "Intensity of colors");

			var b = new Vector4(0.3f, 1f, 0.3f, 1f);
			HarnessColor = config.Bind(section, nameof(HarnessColor), (Color)b, "Color of harnessed quickbelts (RGBA)");
			HarnessIntensity = config.Bind(section, nameof(HarnessIntensity), 1f, "Intensity of colors");

			var c = new Vector4(0.3f, 0.3f, 1f, 1f);
			SpawnlockColor = config.Bind(section, nameof(SpawnlockColor), (Color)c, "Color of spawnlocked quickbelts (RGBA)");
			SpawnlockIntensity = config.Bind(section, nameof(SpawnlockIntensity), 1f, "Intensity of colors");
		}
	}
}
