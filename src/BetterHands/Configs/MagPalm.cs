using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class MagPalmConfig
	{
		public ConfigEntry<bool> aEnable { get; }
		public ConfigEntry<bool> RoundPalm { get; }
		public ConfigEntry<bool> CollisionPrevention { get; }
		public ConfigEntry<float> CollisionPreventionVelocity { get; }
		public ConfigEntry<bool> EasyPalmLoading { get; }
		public ConfigEntry<Vector3> Position { get; }
		public ConfigEntry<Vector3> Rotation { get; }
		public MagPalmControlsConfig Controls { get; }

		public MagPalmConfig(ConfigFile config, string section)
		{
			var header = section + " Options";
			aEnable = config.Bind(header, nameof(aEnable), true, "Allow holding two magazines via palming");

			EasyPalmLoading = config.Bind(header, nameof(EasyPalmLoading), false, "Enables easy mag loading for only the palmed mag. Not necessary if Easy Mag Reloading is already turned on ingame");
			RoundPalm = config.Bind(header, nameof(RoundPalm), false, "Allow palming a single round in the mag palm slots. Primarily envisioned for breakaction reloads");

			CollisionPrevention = config.Bind(header, nameof(CollisionPrevention), true, "Prevents physics clanking by disabling palmed item collision while moving above CollisionPreventionVelocity. Only for smooth locomotion");
			CollisionPreventionVelocity = config.Bind(header, nameof(CollisionPreventionVelocity), 1.5f, "Threshold for CollisionPrevention to kick in for palmed items. Lower to 0 to never have collision on the palmed item");

			header = section + " Position & Rotation";
			Position = config.Bind(header, nameof(Position), new Vector3(0.04f, 0, 0), "Position. X is Outward/Inward, Y is Rearward/Forward, Z is Up/Down");
			Rotation = config.Bind(header, nameof(Rotation), Vector3.zero, "Rotation. X is Forward/Rearward tilt, Y is Outward/Inward tilt, Z is Spin");

			Controls = new MagPalmControlsConfig(config, section + " " + nameof(Controls));
		}
	}
}