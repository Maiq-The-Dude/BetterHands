using BepInEx.Configuration;
using BetterHands.Configs;
using Deli;
using FistVR;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;

namespace BetterHands
{
	public class Plugin : DeliBehaviour
	{
		private const string HARMONY_GUID_CHEAT = "betterhands.h3vr.cheats";
		private const string COLOR_PROPERTY = "_RimColor";

		public RootConfig Configs { get; }

		// Harmony patcher
		private Harmony _harmonyCheat;

		// Wurstmod 2 compat
		private float _ogRadius;
		private float _ogScale;

		private FVRViveHand _rightHand;
		private FVRViveHand _leftHand;
		private bool _magPalming;
		private bool _quickbeltsEnabled;

		public Plugin()
		{
			Configs = new RootConfig(Config);

			// Only patch if mag palming is enabled
			_magPalming = Configs.MagPalm.Enable.Value;
			if (_magPalming)
			{
				Harmony.CreateAndPatchAll(typeof(Patches));
			}

			_harmonyCheat = new Harmony(HARMONY_GUID_CHEAT);

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			ReloadConfig(Config);

			_quickbeltsEnabled = GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled;

			_rightHand = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
			_leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();

			CustomizeHands();
		}

		private void CustomizeHands()
		{
			var hands = new Transform[]
			{
				_rightHand.transform,
				_leftHand.transform
			};

			// Set the idle sphere to our color
			var cfg = Configs.Color;
			hands[0].GetComponent<FVRViveHand>().TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, Recolor(cfg.InteractSphere, cfg.Intensity.Value));

			// Loop through the hands
			foreach (Transform hand in hands)
			{
				// Resize interaction spheres & colliders
				var scale = new float[] { Configs.FingerSize.Value, Configs.PalmSize.Value };
				SphereCollider[] collider = hand.GetComponents<SphereCollider>();
				var fvrhand = hand.GetComponent<FVRViveHand>();
				Transform[] vis = new Transform[]
				{
					fvrhand.TouchSphere.transform,
					fvrhand.TouchSphere_Palm.transform
				};
				for (var i = 0; i < vis.Length; i++)
				{
					// Wurstmod2 compat
					if (_ogRadius == 0.0)
					{
						_ogRadius = collider[i].radius;
						_ogScale = vis[i].localScale.x;
					}
					collider[i].radius = _ogRadius * scale[i];

					var visScale = _ogScale * scale[i];
					vis[i].localScale = new Vector3(visScale, visScale, visScale);
				}

				ColorHandRecursive(GetControllerFrom(hand));
				if (_magPalming)
				{
					ConfigMagPalming(hand);
				}
			}
		}

		// Color the hands various geo children
		private void ColorHandRecursive(GameObject obj)
		{
			var cfg = Configs.Color;
			var intensity = cfg.Intensity.Value;
			if (null == obj)
			{
				return;
			}

			foreach (Transform child in obj.transform)
			{
				if (null == child)
				{
					continue;
				}

				var rend = child.GetComponent<Renderer>();
				if (rend != null)
				{
					var mat = rend.material;

					// All controller geo have two materials, blue & purple
					if (mat.name.ToLower().Contains("blue"))
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.MaterialA, intensity));
					}
					else
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.MaterialB, intensity));
					}
				}

				ColorHandRecursive(child.gameObject);
			}
		}

		// Adds quickslot for palming magazines
		private void ConfigMagPalming(Transform hand)
		{
			var cfg = Configs.MagPalm;

			// Backpack is a part of all quickbelt layouts, use that to create our slots
			var slot = GM.CurrentPlayerBody.Torso.Find("QuickBeltSlot_Backpack");
			var qb = GM.CurrentPlayerBody.QuickbeltSlots;

			if (slot != null)
			{
				var pose = new GameObject().transform;
				pose.parent = hand.GetComponent<FVRViveHand>().PoseOverride;

				if (hand == _rightHand.transform)
				{

					pose.localPosition = cfg.Position.Value;
					pose.localRotation = Quaternion.Euler(cfg.Rotation.Value);
				}
				else
				{
					// mirror configs for left hand
					var rot = cfg.Rotation.Value;
					var tilt = 90 - (rot.y - 90);
					pose.localPosition = Vector3.Scale(cfg.Position.Value, new Vector3(-1, 1, 1));
					pose.localRotation = Quaternion.Euler(rot.x, tilt, rot.z);
				}

				// Create & config our copy
				var newSlot = GameObject.Instantiate(slot, pose);
				newSlot.localPosition = Vector3.zero;
				var geo = newSlot.Find("QB_TransformTarget");
				geo.GetChild(0).gameObject.SetActive(false);
				geo.GetChild(1).gameObject.SetActive(false);

				var newSlotQB = newSlot.GetComponent<FVRQuickBeltSlot>();
				newSlotQB.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
				newSlotQB.IsSelectable = false;
				newSlotQB.name = hand.name;

				qb.Add(newSlotQB);
			}
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
			if (item is FVRFireArmMagazine mag && mag.Size <= cfg.SizeLimit.Value || cfg.CursedPalm.Value)
			{
				return true;
			}

			return false;
		}

		// Format the human readable RGBA to what unity wants
		private Vector4 Recolor(ConfigEntry<Vector4> cfg, float intensity)
		{
			var color = new Vector4(intensity * (cfg.Value[0] / 255), intensity * (cfg.Value[1] / 255), intensity * (cfg.Value[2] / 255), cfg.Value[3] / 1);
			return color;
		}

		// Return the gameobject geo we are using
		public static GameObject GetControllerFrom(Transform hand)
		{
			return GetControllerFrom(hand.GetComponent<FVRViveHand>());
		}
		public static GameObject GetControllerFrom(FVRViveHand hand)
		{
			var id = hand.Pose[hand.HandSource].trackedDeviceIndex;
			var model = SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_ModelNumber_String, id).ToLower();
			if (model.Contains("cosmos"))
			{
				return hand.Display_Controller_Cosmos;
			}
			else if (model.Contains("cv1") || model.Contains("oculus"))
			{
				return hand.Display_Controller_Touch;
			}
			else if (model.Contains("hpmotion"))
			{
				return hand.Display_Controller_HPR2;
			}
			else if (model.Contains("miramar"))
			{
				return hand.Display_Controller_Quest2;
			}
			else if (model.Contains("rift s") || model.Contains("quest"))
			{
				return hand.Display_Controller_RiftS;
			}
			else if (model.Contains("index") || model.Contains("utah") || model.Contains("knuckles"))
			{
				return hand.Display_Controller_Index;
			}
			else if (model.Contains("vive") || model.Contains("nolo"))
			{
				return hand.Display_Controller_Vive;
			}
			else
			{
				return hand.Display_Controller_WMR;
			}
		}

		// Reload config & patch/unpatch tnh score submission
		private void ReloadConfig(ConfigFile config)
		{
			config.Reload();

			// Only patch on state change
			var cfg = Configs.zCheat;
			if (cfg.CursedPalm.Value || cfg.SizeLimit.Value > FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
			{
				if (!Harmony.HasAnyPatches(HARMONY_GUID_CHEAT))
				{
					Logger.LogDebug("TNH score submission disabled");
					_harmonyCheat.PatchAll(typeof(ScorePatch));
				}
			}
			else if (Harmony.HasAnyPatches(HARMONY_GUID_CHEAT))
			{
				Logger.LogDebug("TNH score submission reenabled");
				_harmonyCheat.UnpatchSelf();
			}
		}
		#endregion
	}
}
