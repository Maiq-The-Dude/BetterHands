using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class ColorConfig
	{
		public ConfigEntry<Vector4> MaterialA { get; }
		public ConfigEntry<Vector4> MaterialB { get; }
		public ConfigEntry<Vector4> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }

		public ColorConfig(ConfigFile config, string section)
		{
			var i = new Vector4(81, 140, 255, 1);
			MaterialA = config.Bind(section, nameof(MaterialA), i, "Material A of both hands");

			var j = new Vector4(197, 120, 179, 1);
			MaterialB = config.Bind(section, nameof(MaterialB), j, "Material B of both hands");

			InteractSphere = config.Bind(section, nameof(InteractSphere), i, "Color of interaction spheres");

			Intensity = config.Bind(section, nameof(Intensity), 4f, "Intensity of colors");
		}
	}
}
