using BepInEx.Configuration;
using UnityEngine;
using Valve.VR.InteractionSystem;

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
		public ConfigEntry<bool> Interactable { get; }
		public ConfigEntry<Keybind> LeftKeybind { get; }
		public ConfigEntry<Keybind> RightKeybind { get; }
		public ConfigEntry<Vector3> Position { get; }
		public ConfigEntry<Vector3> Rotation { get; }

		public MagPalmConfig(ConfigFile config, string section)
		{
			Enable = config.Bind(section + "." + nameof(Enable), nameof(Enable), true, "Allow holding two magazines via palming");
			ClickPressure = config.Bind(section + "." + nameof(ClickPressure), nameof(ClickPressure), 0.2f, "Amount of pressure needed for TouchpadClick keybinds");
			GrabbityProtection = config.Bind(section + "." + nameof(GrabbityProtection), nameof(GrabbityProtection), true, "If mag palm keybind matches grabbity keybind, prioritize grabbity input over mag palm");
			Interactable = config.Bind(section + "." + nameof(Interactable), nameof(Interactable), false, "Allow grabbing items directly from palms with the other hand");
			LeftKeybind = config.Bind(section + ".Keybinds", nameof(LeftKeybind), Keybind.Trigger, "Keybind for left hand mag palming");
			RightKeybind = config.Bind(section + ".Keybinds", nameof(RightKeybind), Keybind.Trigger, "Keybind for right hand mag palming");
			Position = config.Bind(section + "." + nameof(Position), nameof(Position), new Vector3(0.035f, 0, 0.035f), "Position of the palmed mag for the right hand. Mirrored for left hand");
			Rotation = config.Bind(section + "." + nameof(Rotation), nameof(Rotation), new Vector3(90f, 85f, 90f), "Rotation of the palmed mag for the right hand. Mirrored for left hand");
		}
	}
}
