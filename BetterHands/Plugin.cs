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
		private const string COLOR_PROPERTY = "_RimColor";
		public RootConfig Configs { get; }

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
			_magPalming = Configs.MagPalm.Enable.Value;
			if (_magPalming)
			{
				Harmony.CreateAndPatchAll(typeof(Patches));
			}

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
				// Right hand magic numbers for positioning
				if (hand == _rightHand.transform)
				{
					if (cfg.Position.Value == MagPalmConfig.Positions.Outside)
					{
						pose.localPosition = new Vector3(0.035f, 0, 0.035f);
						pose.localRotation = Quaternion.Euler(90f, 85f, 90f);
					}
					else
					{
						pose.localPosition = new Vector3(-0.035f, 0, 0.035f);
						pose.localRotation = Quaternion.Euler(90f, 95f, 90f);
					}
				}
				// Left hand magic numbers for positioning
				else
				{
					if (cfg.Position.Value == MagPalmConfig.Positions.Outside)
					{
						pose.localPosition = new Vector3(-0.035f, 0, 0.035f);
						pose.localRotation = Quaternion.Euler(90f, 95f, 90f);
					}
					else
					{
						pose.localPosition = new Vector3(0.035f, 0, 0.035f);
						pose.localRotation = Quaternion.Euler(90f, 85f, 90f);
					}

				}

				// Create & config our copy
				var newSlot = GameObject.Instantiate(slot, pose);
				newSlot.localPosition = Vector3.zero;
				var geo = newSlot.Find("QB_TransformTarget");
				geo.GetChild(0).gameObject.SetActive(false);
				geo.GetChild(1).gameObject.SetActive(false);

				var newSlotQB = newSlot.GetComponent<FVRQuickBeltSlot>();
				newSlotQB.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
				newSlotQB.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Small;
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
			var cfg = hand.IsThisTheRightHand ? Configs.MagPalm.RightKeybind : Configs.MagPalm.LeftKeybind;
			var value = cfg.Value;
			var handInput = hand.Input;
			var input = value switch
			{
				MagPalmConfig.Keybind.AXButton => handInput.AXButtonDown,
				MagPalmConfig.Keybind.BYButton => handInput.BYButtonDown,
				MagPalmConfig.Keybind.Grip => handInput.GripDown,
				MagPalmConfig.Keybind.Secondary2AxisNorth => handInput.Secondary2AxisNorthDown,
				MagPalmConfig.Keybind.Secondary2AxisSouth => handInput.Secondary2AxisSouthDown,
				MagPalmConfig.Keybind.Secondary2AxisEast => handInput.Secondary2AxisEastDown,
				MagPalmConfig.Keybind.Secondary2AxisWest => handInput.Secondary2AxisWestDown,
				MagPalmConfig.Keybind.TouchpadNorth => handInput.TouchpadNorthDown,
				MagPalmConfig.Keybind.TouchpadSouth => handInput.TouchpadSouthDown,
				MagPalmConfig.Keybind.TouchpadEast => handInput.TouchpadEastDown,
				MagPalmConfig.Keybind.TouchpadWest => handInput.TouchpadWestDown,
				MagPalmConfig.Keybind.Trigger => handInput.TriggerDown,
				_ => false,
			};

			if (input)
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
				if ((hand == _rightHand && qb[i].name == _rightHand.name) || (hand == _leftHand && qb[i].name == _leftHand.name))
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

					// else if it is holding a small magazine, swap the current hand and hand slot items
					else if (hand.CurrentInteractable is FVRFireArmMagazine mag)
					{
						if (mag.Size == FVRPhysicalObject.FVRPhysicalObjectSize.Small)
						{
							mag.ForceBreakInteraction();
							mag.SetAllCollidersToLayer(false, "NoCol");

							if (obj != null)
							{
								mag.transform.position = obj.transform.position;
								hand.RetrieveObject(obj);
							}

							mag.GetComponent<FVRPhysicalObject>().SetQuickBeltSlot(qb[i]);
							mag.SetAllCollidersToLayer(false, "Default");
							if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
							{
								GetControllerFrom(hand).SetActive(false);
							}
						}
					}
					break;
				}
			}
		}
		#endregion

		#region Helpers
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

		private static void ReloadConfig(ConfigFile config)
		{
			config.Reload();
		}
		#endregion
	}
}
