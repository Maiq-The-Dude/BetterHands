using BepInEx.Configuration;

namespace BetterHands.Configs
{
	public class MagPalmControlsConfig
	{
		public enum Keybind
		{
			AXButton,
			BYButton,
			Grip,
			Secondary2AxisNorth,
			Secondary2AxisSouth,
			Secondary2AxisEast,
			Secondary2AxisWest,
			TouchpadClickNorth,
			TouchpadClickSouth,
			TouchpadClickEast,
			TouchpadClickWest,
			TouchpadTapNorth,
			TouchpadTapSouth,
			TouchpadTapEast,
			TouchpadTapWest,
			Trigger
		}

		public ConfigEntry<float> ClickPressure { get; }
		public ConfigEntry<bool> GrabbityProtection { get; }
		public ConfigEntry<bool> Interactable { get; }
		public ConfigEntry<Keybind> LeftKeybind { get; }
		public ConfigEntry<Keybind> RightKeybind { get; }

		public MagPalmControlsConfig(ConfigFile config, string section)
		{
			ClickPressure = config.Bind(section, nameof(ClickPressure), 0.2f, "Pressure needed for TouchpadClick keybinds");
			GrabbityProtection = config.Bind(section, nameof(GrabbityProtection), true, "If mag palm keybind matches grabbity keybind, prioritize grabbity input over mag palm");
			Interactable = config.Bind(section, nameof(Interactable), false, "Allow grabbing items directly from palms with the other hand");

			LeftKeybind = config.Bind(section + " Keybinds", nameof(LeftKeybind), Keybind.Trigger, "Keybind for left hand mag palming");
			RightKeybind = config.Bind(section + " Keybinds", nameof(RightKeybind), Keybind.Trigger, "Keybind for right hand mag palming");
		}
	}
}