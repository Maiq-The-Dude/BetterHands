using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class CustomizationConfig
	{
		public ConfigEntry<Color> MaterialA { get; }
		public ConfigEntry<Color> MaterialB { get; }
		public ConfigEntry<Color> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }
		public ConfigEntry<bool> Scale { get; }

		public CustomizationConfig(ConfigFile config, string section)
		{
			var i = new Vector4(81/255f, 140/255f, 255/255f, 1/1f);
			MaterialA = config.Bind(section + " Color", nameof(MaterialA), (Color)i, "Material A of both hands (RGBA)");

			var j = new Vector4(197/255f, 120/255f, 179/255f, 1/1f);
			MaterialB = config.Bind(section + " Color", nameof(MaterialB), (Color)j, "Material B of both hands (RGBA)");

			InteractSphere = config.Bind(section + " Color", nameof(InteractSphere), (Color)i, "Color of interaction spheres (RGBA)");

			Intensity = config.Bind(section + " Color", nameof(Intensity), 4f, "Intensity of colors");

			FingerSize = config.Bind(section + " Interaction Sizes", nameof(FingerSize), 1f, "Scale the finger interaction radius");
			PalmSize = config.Bind(section + " Interaction Sizes", nameof(PalmSize), 1f, "Scale the palm interaction radius");
			Scale = config.Bind(section + " Interaction Sizes", nameof(Scale), true, "Visibly scale the interaction spheres to match their adjusted radius");
		}
	}
}