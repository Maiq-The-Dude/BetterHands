using BepInEx.Configuration;

namespace BetterHands.Configs
{
	public class CustomizationConfig
	{
		public CustomizationHands Hands { get; }
		public CustomizationQBSlots QBSlots { get; }

		public CustomizationConfig(ConfigFile config, string section)
		{
			Hands = new CustomizationHands(config, section + " " + nameof(Hands));
			QBSlots = new CustomizationQBSlots(config, section + " " + nameof(QBSlots));
		}
	}
}