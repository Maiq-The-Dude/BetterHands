using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class CustomizationConfig
	{
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }
		public ConfigEntry<Vector4> MaterialA { get; }
		public ConfigEntry<Vector4> MaterialB { get; }
		public ConfigEntry<Vector4> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }

		public CustomizationConfig(ConfigFile config, string section)
		{
			var i = new Vector4(81, 140, 255, 1);
			MaterialA = config.Bind(section + " Color", nameof(MaterialA), i, "Material A of both hands (RGBA)");

			var j = new Vector4(197, 120, 179, 1);
			MaterialB = config.Bind(section + " Color", nameof(MaterialB), j, "Material B of both hands (RGBA)");

			InteractSphere = config.Bind(section + " Color", nameof(InteractSphere), i, "Color of interaction spheres (RGBA)");

			Intensity = config.Bind(section + " Color", nameof(Intensity), 4f, "Intensity of colors");

			FingerSize = config.Bind(section + " Interaction Sizes", nameof(FingerSize), 1f, "Scale of the finger interaction sphere");
			PalmSize = config.Bind(section + " Interaction Sizes", nameof(PalmSize), 1f, "Scale of the palm interaction sphere");
		}
	}
}
