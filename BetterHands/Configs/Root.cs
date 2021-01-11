using BepInEx.Configuration;

namespace BetterHands.Configs
{
	public class RootConfig
	{
		public ColorConfig Color { get; }
		public ConfigEntry<bool> MagPalming { get; }
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }

		public RootConfig(ConfigFile config)
		{
			Color = new ColorConfig(config, nameof(Color));

			FingerSize = config.Bind("Interaction Sizes", nameof(FingerSize), 1f, "Scale of the finger interaction sphere");
			PalmSize = config.Bind("Interaction Sizes", nameof(PalmSize), 1f, "Scale of the palm interaction sphere");
			MagPalming = config.Bind("Magazine Palming", nameof(MagPalming), true, "Allow holding two magazines via palming");
		}
	}
}
