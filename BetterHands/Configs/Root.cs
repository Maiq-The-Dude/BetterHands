using BepInEx.Configuration;

namespace BetterHands.Configs
{
	public class RootConfig
	{
		public ColorConfig Color { get; }
		public ConfigEntry<float> FingerSize { get; }
		public ConfigEntry<float> PalmSize { get; }
		public MagPalmConfig MagPalm { get; }
		public CheatConfig zCheat { get; }

		public RootConfig(ConfigFile config)
		{
			Color = new ColorConfig(config, nameof(Color));

			FingerSize = config.Bind("Interaction Sizes", nameof(FingerSize), 1f, "Scale of the finger interaction sphere");
			PalmSize = config.Bind("Interaction Sizes", nameof(PalmSize), 1f, "Scale of the palm interaction sphere");

			MagPalm = new MagPalmConfig(config, nameof(MagPalm));
			zCheat = new CheatConfig(config, nameof(zCheat));
		}
	}
}
