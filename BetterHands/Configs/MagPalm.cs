using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class MagPalmConfig
	{
		public ConfigEntry<bool> Enable { get; }
		public ConfigEntry<bool> GrabbityProtection { get; }
		public ConfigEntry<Keybind> LeftKeybind { get; }
		public ConfigEntry<Keybind> RightKeybind { get; }
		public ConfigEntry<Positions> Position { get; }

		public MagPalmConfig(ConfigFile config, string section)
		{
			Enable = config.Bind(section+".Enable", nameof(Enable), true, "Allow holding two magazines via palming");
			GrabbityProtection = config.Bind(section + ".Grabbity Protection", nameof(GrabbityProtection), true, "If mag palm keybind matches grabbity keybind, prioritize grabbity input over mag palm");
			LeftKeybind = config.Bind(section + ".Keybind", nameof(LeftKeybind), Keybind.Trigger, "Keybind for left hand mag palming");
			RightKeybind = config.Bind(section + ".Keybind", nameof(RightKeybind), Keybind.Trigger, "Keybind for right hand mag palming");
			Position = config.Bind(section+".Position", nameof(Position), Positions.Outside, "Position of the palmed mag");
		}
		public enum Positions
		{
			Inside,
			Outside
		}
		public enum Keybind
		{
			AXButton,
			BYButton,
			Grip,
			Secondary2AxisNorth,
			Secondary2AxisSouth,
			Secondary2AxisEast,
			Secondary2AxisWest,
			TouchpadNorth,
			TouchpadSouth,
			TouchpadEast,
			TouchpadWest,
			Trigger
		}
	}
}
