using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class CustomizationHands
	{
		public ConfigEntry<bool> aEnable { get; }
		public ConfigEntry<Color> MaterialA { get; }
		public ConfigEntry<Color> MaterialB { get; }
		public ConfigEntry<Color> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }
		public ConfigEntry<bool> Scale { get; }

		public CustomizationHands(ConfigFile config, string section)
		{
			aEnable = config.Bind(section, nameof(aEnable), true, "Allows recoloring of the controller geo");

			var i = new Vector4(81 / 255f, 140 / 255f, 255 / 255f, 1 / 1f);
			MaterialA = config.Bind(section, nameof(MaterialA), (Color)i, "Material A of both hands (RGBA)");

			var j = new Vector4(197 / 255f, 120 / 255f, 179 / 255f, 1 / 1f);
			MaterialB = config.Bind(section, nameof(MaterialB), (Color)j, "Material B of both hands (RGBA)");

			InteractSphere = config.Bind(section, nameof(InteractSphere), (Color)i, "Color of interaction spheres (RGBA)");

			Intensity = config.Bind(section, nameof(Intensity), 4f, "Intensity of colors");

			FingerSize = config.Bind(section, nameof(FingerSize), 1f, "Scale the finger interaction radius");
			PalmSize = config.Bind(section, nameof(PalmSize), 1f, "Scale the palm interaction radius");
			Scale = config.Bind(section, nameof(Scale), true, "Visibly scale the interaction spheres to match their adjusted radius");
		}
	}
}
