using BepInEx.Configuration;
using UnityEngine;

namespace BetterHands.Configs
{
	public class MagPalmConfig
	{
		public ConfigEntry<bool> Enable { get; }
		public ConfigEntry<bool> CollisionPrevention { get; }
		public ConfigEntry<float> CollisionPreventionVelocity { get; }
		public ConfigEntry<bool> EasyPalmLoading { get; }
		public ConfigEntry<Vector3> Position { get; }
		public ConfigEntry<Vector3> Rotation { get; }
		public MagPalmControlsConfig Controls { get; }

		public MagPalmConfig(ConfigFile config, string section)
		{
			Enable = config.Bind(section + "..." + nameof(Enable), nameof(Enable), true, "Allow holding two magazines via palming");

			CollisionPrevention = config.Bind(section + ".." + nameof(CollisionPrevention), nameof(CollisionPrevention), true, "Prevents physics issues & clanking by disabling palmed item collision while moving above CollisionPreventionVelocity. Only for smooth locomotion");
			CollisionPreventionVelocity = config.Bind(section + ".." + nameof(CollisionPreventionVelocity), nameof(CollisionPreventionVelocity), 1.5f, "Threshold for CollisionPrevention to kick in for palmed items");
			EasyPalmLoading = config.Bind(section + ".." + nameof(EasyPalmLoading), nameof(EasyPalmLoading), false, "Enables easy mag loading for only the palmed mag. Not necessary if Easy Mag Reloading is already turned on ingame");

			Position = config.Bind(section + ".." + nameof(Position), nameof(Position), new Vector3(0.035f, 0, 0.035f), "Position of the palmed item for the right hand. Mirrored for left hand");
			Rotation = config.Bind(section + ".." + nameof(Rotation), nameof(Rotation), new Vector3(90f, 85f, 90f), "Rotation of the palmed item for the right hand. Mirrored for left hand");

			Controls = new MagPalmControlsConfig(config, nameof(Controls));
		}
	}
}
