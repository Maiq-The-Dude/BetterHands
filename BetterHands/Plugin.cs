using BetterHands.Configs;
using BetterHands.Hooks;
using Deli.H3VR.Api;
using Deli.Setup;
using FistVR;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterHands
{
	public class Plugin : DeliBehaviour
	{
		private readonly H3Api _api = H3Api.Instance;

		private RootConfig _config;

		private HandCustomization _handCustomization;
		private MagPalm _magPalm;

		public Plugin()
		{
			Init();
		}

		private void OnDestroy()
		{
			_handCustomization.Unhook();
			_magPalm.Unhook();

			SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
		}

		private void Init()
		{
			_config = new RootConfig(Config);

			_config.MagPalm.Enable.SettingChanged += MagPalmEnable_SettingChanged;
			_config.MagPalm.Enable.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.CursedPalms.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.SizeLimit.SettingChanged += Cheat_SettingChanged;

			_handCustomization = new HandCustomization(_config);
			_handCustomization.Hook();

			_magPalm = new MagPalm(_config, Logger);
			_magPalm.Hook();

			ScoreSubmissionManager();

			SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		}

		#region Hook Events

		private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Config.Reload();
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

		private void Cheat_SettingChanged(object sender, System.EventArgs e)
		{
			ScoreSubmissionManager();
		}

		private void ScoreSubmissionManager()
		{	
			var cfg = _config.zCheat;
			if (_config.MagPalm.Enable.Value && cfg.CursedPalms.Value || cfg.SizeLimit.Value > FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
			{
				_api.RequestLeaderboardDisable(Source, true);
			}
			else
			{
				_api.RequestLeaderboardDisable(Source, false);
			}
		}
		#endregion

		#region Helpers

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
		#endregion
	}
}
