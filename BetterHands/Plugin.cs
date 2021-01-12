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

		private FVRViveHand _leftHand;
		private FVRViveHand _rightHand;
		private bool _magPalming;

		public Plugin()
		{
			Configs = new RootConfig(Config);
			_magPalming = Configs.MagPalming.Value;
			if (_magPalming)
			{
				Harmony.CreateAndPatchAll(typeof(Patches));
			}

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			ReloadConfig(Config);

			_leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
			_rightHand = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();

			CustomizeHands();
		}

		private void CustomizeHands()
		{
			var hands = new Transform[]
			{
				GM.CurrentPlayerBody.RightHand,
				GM.CurrentPlayerBody.LeftHand
			};

			// Set the idle sphere to our color
			var cfg = Configs.Color;
			var fvrHand = hands[0].GetComponent<FVRViveHand>();
			fvrHand.TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, Recolor(cfg.InteractSphere, cfg.Intensity.Value));

			// Resize interaction spheres & colliders
			var scale = new float[] { Configs.FingerSize.Value, Configs.PalmSize.Value };
			Transform[] vis = null;
			SphereCollider[] collider;
			foreach (Transform hand in hands)
			{
				collider = hand.GetComponents<SphereCollider>();
				vis = new Transform[] { hand.Find("ControllerModel/_TouchIndication_Fingers"), hand.Find("ControllerModel/_TouchIndication_Palm") };
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
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.HandA, intensity));
					}
					else
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.HandB, intensity));
					}
				}

				ColorHandRecursive(child.gameObject);
			}
		}

		// Adds quickslot for palming magazines
		private void ConfigMagPalming(Transform hand)
		{
			var slot = GM.CurrentPlayerBody.Torso.Find("QuickBeltSlot_Backpack");
			var qb = GM.CurrentPlayerBody.QuickbeltSlots;

			if (slot != null)
			{
				var pose = new GameObject().transform;
				pose.parent = hand.GetComponent<FVRViveHand>().PoseOverride;
				if (hand == GM.CurrentPlayerBody.RightHand)
				{
					pose.localPosition = new Vector3(0.035f, 0, 0.035f);
					pose.localRotation = Quaternion.Euler(90f, 85f, 90f);
				}
				else
				{
					pose.localPosition = new Vector3(-0.035f, 0, 0.035f);
					pose.localRotation = Quaternion.Euler(90f, 95f, 90f);
				}

				var newSlot = GameObject.Instantiate(slot, pose);
				newSlot.localPosition = Vector3.zero;

				// Disable mag palm geo
				var geo = newSlot.Find("QB_TransformTarget");
				geo.GetChild(0).gameObject.SetActive(false);
				geo.GetChild(1).gameObject.SetActive(false);

				var newSlotQB = newSlot.GetComponent<FVRQuickBeltSlot>();
				qb.Add(newSlotQB);
				qb[qb.Count - 1].IsSelectable = false;
			}
		}

		#region Input
		private void Update()
		{
			if (_magPalming && GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				PollInput(_rightHand);
				PollInput(_leftHand);
			}
		}

		private void PollInput(FVRViveHand hand)
		{
			var input = _rightHand.Input.TouchpadEastDown;
			var handSlot = 2;

			if (hand == _leftHand)
			{
				input = _leftHand.Input.TouchpadWestDown;
				handSlot = 1;
			}

			if (input && !hand.IsInStreamlinedMode)
			{
				var qb = GM.CurrentPlayerBody.QuickbeltSlots;
				var beltSlots = qb.Count;
				var obj = qb[beltSlots - handSlot].CurObject;
				if (hand.m_state == FVRViveHand.HandState.Empty)
				{
					if (obj != null)
					{
						hand.RetrieveObject(obj);
					}
				}
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

						mag.GetComponent<FVRPhysicalObject>().SetQuickBeltSlot(qb[beltSlots - handSlot]);
						mag.SetAllCollidersToLayer(false, "Default");
						if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
						{
							GetControllerFrom(hand).SetActive(false);
						}
					}
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

		// Return what geo we are using
		public static GameObject GetControllerFrom(Transform hand)
		{
			return GetControllerFrom(hand.GetComponent<FVRViveHand>());
		}
		public static GameObject GetControllerFrom(FVRViveHand hand)
		{
			uint id = hand.Pose[hand.HandSource].trackedDeviceIndex;
			string model = SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_ModelNumber_String, id).ToLower();
			if (model.Contains("index") || model.Contains("utah") || model.Contains("knuckles"))
			{
				return hand.Display_Controller_Index;
			}
			else if (model.Contains("cosmos"))
			{
				return hand.Display_Controller_Cosmos;
			}
			else if (model.Contains("vive") || model.Contains("nolo"))
			{
				return hand.Display_Controller_Vive;
			}
			else if (model.Contains("miramar"))
			{
				return hand.Display_Controller_Touch;
			}
			else if (model.Contains("rift s") || model.Contains("quest"))
			{
				return hand.Display_Controller_RiftS;
			}
			else if (model.Contains("oculus") || model.Contains("cv1"))
			{
				return hand.Display_Controller_RiftS;
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
