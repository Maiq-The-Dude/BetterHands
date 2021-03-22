using BetterHands.Configs;
using BetterHands.Patches;
using Deli.Setup;
using FistVR;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace BetterHands
{
	public class Plugin : DeliBehaviour
	{
		public readonly string GUID;
		public readonly string HARMONY_GUID_HANDS;
		public readonly string HARMONY_GUID_MAGPALM;
		public readonly string HARMONY_GUID_CHEAT;

		public static Plugin Instance { get; private set; }

		public FVRPlayerBody PlayerBody;
		public Transform RightHand;
		public Transform LeftHand;

		public RootConfig Configs { get; }

		// Harmony patchers
		private Harmony _harmonyHands;
		private Harmony _harmonyMagPalm;
		private Harmony _harmonyCheat;

		public Plugin()
		{
			GUID = Info.Guid;
			HARMONY_GUID_HANDS = GUID + ".hands";
			HARMONY_GUID_MAGPALM = GUID + ".magpalm";
			HARMONY_GUID_CHEAT = GUID + ".cheats";

			Instance = this;

			Configs = new RootConfig(Config);
			PatchInit();
		}

		private void OnLevelWasLoaded(int index)
		{
			PlayerBody = GM.CurrentPlayerBody;
			RightHand = PlayerBody.RightHand;
			LeftHand = PlayerBody.LeftHand;

			Config.Reload();
		}

		#region PatchInit

		// Subscribes to config change events & inits harmony patches 
		private void PatchInit()
		{
			Configs.MagPalm.Enable.SettingChanged += MagPalmEnable_SettingChanged;
			Configs.MagPalm.Enable.SettingChanged += Cheat_SettingChanged;
			Configs.zCheat.CursedPalms.SettingChanged += Cheat_SettingChanged;
			Configs.zCheat.SizeLimit.SettingChanged += Cheat_SettingChanged;

			_harmonyHands = new Harmony(HARMONY_GUID_HANDS);
			_harmonyHands.PatchAll(typeof(HandCustomizationPatches));

			_harmonyMagPalm = new Harmony(HARMONY_GUID_MAGPALM);
			PatchIfMagPalm();

			_harmonyCheat = new Harmony(HARMONY_GUID_CHEAT);
			PatchIfCheatsExist();
		}

		private void MagPalmEnable_SettingChanged(object sender, System.EventArgs e)
		{
			PatchIfMagPalm();
		}

		private void Cheat_SettingChanged(object sender, System.EventArgs e)
		{
			PatchIfCheatsExist();
		}

		private void PatchIfMagPalm()
		{
			if (Configs.MagPalm.Enable.Value)
			{
				Logger.LogDebug("Mag palming enabled");
				_harmonyMagPalm.PatchAll(typeof(MagPalmPatches));
			}
			else
			{
				Logger.LogDebug("Mag palming disabled");
				_harmonyMagPalm.UnpatchSelf();
			}
		}

		private void PatchIfCheatsExist()
		{
			// Only patch on state change
			var cfg = Configs.zCheat;
			if (Configs.MagPalm.Enable.Value && (cfg.CursedPalms.Value || cfg.SizeLimit.Value > FVRPhysicalObject.FVRPhysicalObjectSize.Medium))
			{
				Logger.LogDebug("TNH score submission disabled");
				_harmonyCheat.PatchAll(typeof(ScorePatches));
			}
			else if (Harmony.HasAnyPatches(HARMONY_GUID_CHEAT))
			{
				Logger.LogDebug("TNH score submission enabled");
				_harmonyCheat.UnpatchSelf();
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

			return geo == null ? hand.Display_Controller : geo;
		}
		#endregion
	}
}
