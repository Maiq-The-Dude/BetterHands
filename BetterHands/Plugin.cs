using BetterHands.Configs;
using BetterHands.Patches;
using Deli;
using FistVR;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterHands
{
	public class Plugin : DeliBehaviour
	{
		public string GUID => Info.Guid;
		public readonly string HARMONY_GUID_HANDS;
		public readonly string HARMONY_GUID_MAGPALM;
		public readonly string HARMONY_GUID_CHEAT;

		public static Plugin Instance { get; private set; }

		public RootConfig Configs { get; }

		// Harmony patchers
		private Harmony _harmonyHands;
		private Harmony _harmonyMagPalm;
		private Harmony _harmonyCheat;

		private bool _magPalming;
		private bool _quickbeltsEnabled;
		private FVRViveHand _rightHand;
		private FVRViveHand _leftHand;

		public Plugin()
		{
			HARMONY_GUID_HANDS = GUID + ".hands";
			HARMONY_GUID_MAGPALM = GUID + ".magpalm";
			HARMONY_GUID_CHEAT = GUID + ".cheats";

			Instance = this;

			Configs = new RootConfig(Config);
			PatchInit(Configs);

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Config.Reload();

			_magPalming = Configs.MagPalm.Enable.Value;
			_quickbeltsEnabled = GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled;

			_rightHand = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
			_leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
		}

		#region Input
		private void Update()
		{
			if (_magPalming && _quickbeltsEnabled)
			{
				PollInput(_rightHand);
				PollInput(_leftHand);
			}
		}

		private void PollInput(FVRViveHand hand)
		{
			// Get input from hand & config
			var cfg = Configs.MagPalm;
			var value = hand.IsThisTheRightHand ? cfg.RightKeybind.Value : cfg.LeftKeybind.Value;
			var handInput = hand.Input;
			var magnitude = handInput.TouchpadAxes.magnitude > cfg.ClickPressure.Value;
			var input = value switch
			{
				MagPalmConfig.Keybind.AXButton => handInput.AXButtonDown,
				MagPalmConfig.Keybind.BYButton => handInput.BYButtonDown,
				MagPalmConfig.Keybind.Grip => handInput.GripDown,
				MagPalmConfig.Keybind.Secondary2AxisNorth => handInput.Secondary2AxisNorthDown,
				MagPalmConfig.Keybind.Secondary2AxisSouth => handInput.Secondary2AxisSouthDown,
				MagPalmConfig.Keybind.Secondary2AxisEast => handInput.Secondary2AxisEastDown,
				MagPalmConfig.Keybind.Secondary2AxisWest => handInput.Secondary2AxisWestDown,
				MagPalmConfig.Keybind.TouchpadClickNorth => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.up) <= 45f,
				MagPalmConfig.Keybind.TouchpadClickSouth => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.down) <= 45f,
				MagPalmConfig.Keybind.TouchpadClickEast => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.right) <= 45f,
				MagPalmConfig.Keybind.TouchpadClickWest => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.left) <= 45f,
				MagPalmConfig.Keybind.TouchpadTapNorth => handInput.TouchpadNorthDown,
				MagPalmConfig.Keybind.TouchpadTapSouth => handInput.TouchpadSouthDown,
				MagPalmConfig.Keybind.TouchpadTapEast => handInput.TouchpadEastDown,
				MagPalmConfig.Keybind.TouchpadTapWest => handInput.TouchpadWestDown,
				MagPalmConfig.Keybind.Trigger => handInput.TriggerDown,
				_ => false,
			};

			if (input && GrabbityProtection(hand, value))
			{
				MagPalmInput(hand, input);
			}
		}

		private void MagPalmInput(FVRViveHand hand, bool input)
		{
			// Get handslot index here so quickbelt layout doesn't break retrieval mid-scene work
			var qb = GM.CurrentPlayerBody.QuickbeltSlots;
			for (var i = 0; i < qb.Count; i++)
			{
				if (qb[i].name == hand.name)
				{
					var obj = qb[i].CurObject;

					// If current hand is empty, retrieve the object
					if (hand.m_state == FVRViveHand.HandState.Empty)
					{
						if (obj != null)
						{
							hand.RetrieveObject(obj);
						}
					}

					// else if it is holding something, swap the current hand and hand slot items
					else if (AllowPalming(hand.CurrentInteractable))
					{
						var item = hand.CurrentInteractable;
						item.ForceBreakInteraction();
						item.SetAllCollidersToLayer(false, "NoCol");

						if (obj != null)
						{
							item.transform.position = obj.transform.position;
							hand.RetrieveObject(obj);
						}

						item.GetComponent<FVRPhysicalObject>().SetQuickBeltSlot(qb[i]);
						item.SetAllCollidersToLayer(false, "Default");
						if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
						{
							GetControllerFrom(hand).SetActive(false);
						}
					}
					break;
				}
			}
		}
		#endregion

		#region PatchInit

		// Subscribes to config change events & inits harmony patches 
		private void PatchInit(RootConfig config)
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

		// If mag palm keybind matches grabbity keybind, suppress mag palm input if grabbity sphere is on an item
		private bool GrabbityProtection(FVRViveHand hand, MagPalmConfig.Keybind keybind)
		{
			if (Configs.MagPalm.GrabbityProtection.Value)
			{
				var grabbityState = GM.Options.ControlOptions.WIPGrabbityButtonState;
				if (grabbityState == ControlOptions.WIPGrabbityButton.Trigger && (keybind == MagPalmConfig.Keybind.Trigger)
					|| grabbityState == ControlOptions.WIPGrabbityButton.Grab && (keybind == MagPalmConfig.Keybind.Grip))
				{
					return !hand.Grabbity_HoverSphere.gameObject.activeSelf;
				}
			}

			return true;
		}

		// Returns true if the held object is valid for palming
		private bool AllowPalming(FVRInteractiveObject item)
		{
			var cfg = Configs.zCheat;
			if (item is FVRFireArmMagazine mag && mag.Size <= cfg.SizeLimit.Value || cfg.CursedPalms.Value)
			{
				return true;
			}

			return false;
		}

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

			foreach (var geo in controllerGeos)
			{
				if (geo.activeSelf)
				{
					return geo;
				}
			}

			return hand.Display_Controller;
		}
		#endregion
	}
}
