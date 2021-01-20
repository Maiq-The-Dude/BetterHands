using BepInEx.Configuration;
using FistVR;

namespace BetterHands.Configs
{
	public class CheatConfig
	{
		public ConfigEntry<bool> CursedPalm { get; }
		public ConfigEntry<FVRPhysicalObject.FVRPhysicalObjectSize> SizeLimit { get; }

		public CheatConfig(ConfigFile config, string section)
		{
			CursedPalm = config.Bind(section + "." + nameof(CursedPalm), nameof(CursedPalm), false, "Disregards SizeLimit and allows mag palming to be used on anything. Not supported, use at your own risk. Disables TNH leaderboard submissions until disabled and scene reloaded");
			SizeLimit = config.Bind(section + "." + nameof(SizeLimit), nameof(SizeLimit), FVRPhysicalObject.FVRPhysicalObjectSize.Small, "Mag palm magazine size limit. Greater than medium disables TNH leaderboard submissions until disabled and scene reloaded");
		}
	}
}
