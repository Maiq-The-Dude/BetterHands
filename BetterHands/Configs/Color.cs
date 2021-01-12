using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class ColorConfig
	{
		public ConfigEntry<Vector4> HandA { get; }
		public ConfigEntry<Vector4> HandB { get; }
		public ConfigEntry<Vector4> InteractSphere { get; }
		public ConfigEntry<float> Intensity { get; }
		public ColorConfig(ConfigFile config, string section)
		{
			var i = new Vector4(81, 140, 255, 1);
			HandA = config.Bind(section, nameof(HandA), i, "Color A of hands");

			var j = new Vector4(197, 120, 179, 1);
			HandB = config.Bind(section, nameof(HandB), j, "Color B of hands");

			InteractSphere = config.Bind(section, nameof(InteractSphere), i, "Color interaction spheres");

			Intensity = config.Bind(section, nameof(Intensity), 4f, "Intensity of colors (0-4)");
		}
	}
}
