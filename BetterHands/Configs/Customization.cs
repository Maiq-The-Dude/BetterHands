using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class CustomizationConfig
	{
		public ConfigEntry<Vector4> MaterialA { get; }
		public ConfigEntry<Vector4> MaterialB { get; }
		public ConfigEntry<Vector4> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }
		public ConfigEntry<bool> Scale { get; }

		public CustomizationConfig(ConfigFile config, string section)
		{
			var i = new Vector4(81, 140, 255, 1);
			MaterialA = config.Bind(section + " Color", nameof(MaterialA), i, "Material A of both hands (RGBA)");

			var j = new Vector4(197, 120, 179, 1);
			MaterialB = config.Bind(section + " Color", nameof(MaterialB), j, "Material B of both hands (RGBA)");

			InteractSphere = config.Bind(section + " Color", nameof(InteractSphere), i, "Color of interaction spheres (RGBA)");

			Intensity = config.Bind(section + " Color", nameof(Intensity), 4f, "Intensity of colors");

			FingerSize = config.Bind(section + " Interaction Sizes", nameof(FingerSize), 1f, "Scale the finger interaction radius");
			PalmSize = config.Bind(section + " Interaction Sizes", nameof(PalmSize), 1f, "Scale the palm interaction radius");
			Scale = config.Bind(section + " Interaction Sizes", nameof(Scale), true, "Visibly scale the interaction spheres to match their adjusted radius");
		}
	}
}
