using BepInEx.Configuration;

namespace BetterHands.Configs
{
	public class RootConfig
	{
		public CustomizationConfig Customization { get; }

		public MagPalmConfig MagPalm { get; }
		public CheatConfig zCheat { get; }

		public RootConfig(ConfigFile config)
		{
			Customization = new CustomizationConfig(config, nameof(Customization));
			MagPalm = new MagPalmConfig(config, nameof(MagPalm));
			zCheat = new CheatConfig(config, nameof(zCheat));
		}
	}
}