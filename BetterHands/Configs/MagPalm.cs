using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class MagPalmConfig
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

		public ConfigEntry<bool> Enable { get; }
		public ConfigEntry<float> ClickPressure { get; }
		public ConfigEntry<bool> GrabbityProtection { get; }
		public ConfigEntry<Keybind> LeftKeybind { get; }
		public ConfigEntry<Keybind> RightKeybind { get; }
		public ConfigEntry<Vector3> Position { get; }
		public ConfigEntry<Vector3> Rotation { get; }

		public MagPalmConfig(ConfigFile config, string section)
		{
			Enable = config.Bind(section + ".Enable", nameof(Enable), true, "Allow holding two magazines via palming");
			ClickPressure = config.Bind(section + ".Click Pressure", nameof(ClickPressure), 0.2f, "Amount of pressure needed for TouchpadClick keybinds");
			GrabbityProtection = config.Bind(section + ".Grabbity Protection", nameof(GrabbityProtection), true, "If mag palm keybind matches grabbity keybind, prioritize grabbity input over mag palm");
			LeftKeybind = config.Bind(section + ".Keybind", nameof(LeftKeybind), Keybind.Trigger, "Keybind for left hand mag palming");
			RightKeybind = config.Bind(section + ".Keybind", nameof(RightKeybind), Keybind.Trigger, "Keybind for right hand mag palming");
			Position = config.Bind(section + ".Position", nameof(Position), new Vector3(0.035f, 0, 0.035f), "Position of the palmed mag for the right hand. Mirrored for left hand");
			Rotation = config.Bind(section + ".Rotation", nameof(Rotation), new Vector3(90f, 85f, 90f), "Rotation of the palmed mag for the right hand. Mirrored for left hand");
		}
	}
}
