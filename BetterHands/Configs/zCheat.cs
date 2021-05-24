using BepInEx.Configuration;
using FistVR;

namespace BetterHands.Configs
{
	public class CheatConfig
	{
		public ConfigEntry<bool> CursedPalms { get; }
		public ConfigEntry<FVRPhysicalObject.FVRPhysicalObjectSize> SizeLimit { get; }

		public CheatConfig(ConfigFile config, string section)
		{
			CursedPalms = config.Bind(section, nameof(CursedPalms), false, "Disregards SizeLimit and allows mag palming to be used on anything. Generally not supported, use at your own risk. Disables TNH leaderboard submissions until disabled and scene reloaded");
			SizeLimit = config.Bind(section, nameof(SizeLimit), FVRPhysicalObject.FVRPhysicalObjectSize.Small, "Mag palm magazine size limit. Greater than medium disables TNH leaderboard submissions until disabled and scene reloaded");
		}
	}
}
