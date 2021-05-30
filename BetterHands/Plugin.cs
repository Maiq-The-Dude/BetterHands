using BetterHands.Configs;
using BetterHands.Customization;
using BetterHands.MagPalming;
using Deli.H3VR.Api;
using Deli.Setup;
using FistVR;
using System.Linq;
using UnityEngine;

namespace BetterHands
{
	public class Plugin : DeliBehaviour
	{
		private readonly H3Api _api = H3Api.Instance;

		private readonly RootConfig _config;

		private readonly HandsRecolor _handCustomization;
		private readonly MagPalm _magPalm;

		public Plugin()
		{
			_config = new RootConfig(Config);

			_config.MagPalm.Enable.SettingChanged += MagPalmEnable_SettingChanged;
			_config.MagPalm.Enable.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.CursedPalms.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.SizeLimit.SettingChanged += Cheat_SettingChanged;

			_handCustomization = new HandsRecolor(_config);
			_magPalm = new MagPalm(_config, Logger);

			On.FistVR.FVRPlayerBody.Init += (orig, self, SceneSettings) =>
			{
				Config.Reload();
				orig(self, SceneSettings);
			};
		}

		private void Awake()
		{
			_handCustomization.Hook();

			if (_config.MagPalm.Enable.Value)
			{
				_magPalm.Hook();
			}

			ScoreSubmissionManager();
		}

		private void OnDestroy()
		{
			_handCustomization.Unhook();
			_magPalm.Unhook();
		}

		private void ScoreSubmissionManager()
		{
			var cfg = _config.zCheat;
			if (_config.MagPalm.Enable.Value && cfg.CursedPalms.Value || cfg.SizeLimit.Value > FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
			{
				Logger.LogDebug("TNH scoring is disabled");
				_api.RequestLeaderboardDisable(Source, true);
			}
			else
			{
				Logger.LogDebug("TNH scoring is enabled");
				_api.RequestLeaderboardDisable(Source, false);
			}
		}

		#region Hook Events

		private void Cheat_SettingChanged(object sender, System.EventArgs e)
		{
			ScoreSubmissionManager();
		}

		private void MagPalmEnable_SettingChanged(object sender, System.EventArgs e)
		{
			if (_config.MagPalm.Enable.Value)
			{
				_magPalm.Hook();
			}
			else
			{
				_magPalm.Unhook();
			}
		}

		#endregion Hook Events

		#region Shared

		// Return the gameobject geo we are using
		public static GameObject GetControllerFrom(FVRViveHand hand)
		{
			var controllerGeos = new GameObject[]
			{
				hand.Display_Controller_Cosmos,
				hand.Display_Controller_HPR2,
				hand.Display_Controller_Index,
				hand.Display_Controller_Quest2,
				hand.Display_Controller_RiftS,
				hand.Display_Controller_Touch,
				hand.Display_Controller_Vive,
				hand.Display_Controller_WMR
			};

			var geo = controllerGeos.FirstOrDefault(g => g.activeSelf);

			return geo ?? hand.Display_Controller;
		}

		#endregion Shared
	}
}