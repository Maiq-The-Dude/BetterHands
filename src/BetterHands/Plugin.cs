using BepInEx;
using BetterHands.Configs;
using BetterHands.Customizations;
using BetterHands.MagPalming;
using FistVR;
using Sodalite.Api;
using System;
using UnityEngine;

namespace BetterHands
{
	[BepInPlugin("maiq.BetterHands", "BetterHands", "1.8.0")]
	[BepInDependency("nrgill28.Sodalite")]
	[BepInProcess("h3vr.exe")]
	public class Plugin : BaseUnityPlugin
	{
		private readonly RootConfig _config;

		private readonly Customization _customization;
		private readonly MagPalm _magPalm;

		private IDisposable _leaderboardLock;

		public Plugin()
		{
			_config = new RootConfig(Config);

			_config.MagPalm.aEnable.SettingChanged += MagPalm_SettingChanged;
			_config.Customization.Hands.aEnable.SettingChanged += Customization_SettingChanged;
			_config.Customization.QBSlots.aEnable.SettingChanged += Customization_SettingChanged;

			_config.MagPalm.aEnable.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.CursedPalms.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.SizeLimit.SettingChanged += Cheat_SettingChanged;
			_config.zCheat.CursedTriggers.SettingChanged += Cheat_SettingChanged;

			_customization = new Customization(_config);
			_magPalm = new MagPalm(_config, Logger);

			On.FistVR.FVRPlayerBody.Init += (orig, self, SceneSettings) =>
			{
				Config.Reload();
				orig(self, SceneSettings);
			};
		}

		private void Awake()
		{
			if (_config.Customization.Hands.aEnable.Value) _customization.HandsHook();

			if (_config.Customization.QBSlots.aEnable.Value) _customization.QBSlotsHook();

			if (_config.MagPalm.aEnable.Value) _magPalm.Hook();
		}

		private void Start()
		{
			ScoreSubmissionManager();
		}

		private void OnDestroy()
		{
			_customization.HandsUnhook();
			_customization.QBSlotsUnhook();
			_magPalm.Unhook();
		}

		private void ScoreSubmissionManager()
		{
			var cfg = _config.zCheat;
			if ((_config.MagPalm.aEnable.Value && (cfg.CursedPalms.Value || cfg.SizeLimit.Value > FVRPhysicalObject.FVRPhysicalObjectSize.Medium)) || cfg.CursedTriggers.Value)
			{
				Logger.LogDebug("TNH scoring is disabled");
				_leaderboardLock ??= LeaderboardAPI.LeaderboardDisabled.TakeLock();
			}
			else
			{
				Logger.LogDebug("TNH scoring is enabled");
				_leaderboardLock?.Dispose();
				_leaderboardLock = null;
			}
		}

		#region Hook Events

		private void Cheat_SettingChanged(object sender, System.EventArgs e)
		{
			ScoreSubmissionManager();
		}

		private void MagPalm_SettingChanged(object sender, System.EventArgs e)
		{
			if (_config.MagPalm.aEnable.Value) _magPalm.Hook(); else _magPalm.Unhook();
		}
		private void Customization_SettingChanged(object sender, System.EventArgs e)
		{
			if (_config.Customization.Hands.aEnable.Value) _customization.HandsHook(); else _customization.HandsUnhook();

			if (_config.Customization.QBSlots.aEnable.Value) _customization.QBSlotsHook(); else _customization.QBSlotsUnhook();
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

			var geo = Array.Find(controllerGeos, g => g.activeSelf);

			return geo ?? hand.Display_Controller;
		}

		#endregion Shared
	}
}