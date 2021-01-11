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

		public float OGRadius;
		public float OGScale;

		private FVRViveHand _leftHand;
		private FVRViveHand _rightHand;

		private bool _magPalming;

		public Plugin()
		{
			Configs = new RootConfig(Config);
			_magPalming = Configs.MagPalming.Value;
			if (_magPalming)
			{
				Harmony.CreateAndPatchAll(typeof(Plugin));
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
			fvrHand.TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, Recolor(cfg.InteractSphere));

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
					if (OGRadius == 0.0)
					{
						OGRadius = collider[i].radius;
						OGScale = vis[i].localScale.x;
					}
					collider[i].radius = OGRadius * scale[i];

					var visScale = OGScale * scale[i];
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
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.HandA));
					}
					else
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.HandB));
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
					//pose.localPosition = new Vector3(-0.05f, 0, -0.14f);
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

		#region Patches
		// Let our quickslots load guns
		[HarmonyPatch(typeof(FVRFireArmReloadTriggerMag), "OnTriggerEnter")]
		[HarmonyPostfix]
		private static void PatchMagTrigger(FVRFireArmReloadTriggerMag __instance, Collider collider)
		{
			if (__instance.Magazine != null && __instance.Magazine.FireArm == null && collider.gameObject.tag == "FVRFireArmReloadTriggerWell")
			{
				if (__instance.Magazine.QuickbeltSlot == GM.CurrentPlayerBody.QuickbeltSlots[GM.CurrentPlayerBody.QuickbeltSlots.Count - 1] || __instance.Magazine.QuickbeltSlot == GM.CurrentPlayerBody.QuickbeltSlots[GM.CurrentPlayerBody.QuickbeltSlots.Count - 2])
				{
					FVRFireArmReloadTriggerWell component = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
					FireArmMagazineType fireArmMagazineType = component.FireArm.MagazineType;
					if (component.UsesTypeOverride)
					{
						fireArmMagazineType = component.TypeOverride;
					}
					if (fireArmMagazineType == __instance.Magazine.MagazineType && (component.FireArm.EjectDelay <= 0f || __instance.Magazine != component.FireArm.LastEjectedMag) && component.FireArm.Magazine == null)
					{
						__instance.Magazine.SetQuickBeltSlot(null);
						__instance.Magazine.Load(component.FireArm);

						// very cool code to set return the hand to visible
						GetControllerFrom(__instance.Magazine.FireArm.m_hand.OtherHand).SetActive(true);

					}
				}
			}
		}

		// Returns controller geo if slots/hands empty
		[HarmonyPatch(typeof(FVRViveHand), "CurrentInteractable", MethodType.Setter)]
		[HarmonyPostfix]
		private static void CurrentInteractablePatch(FVRViveHand __instance)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				if (__instance.IsThisTheRightHand && GM.CurrentPlayerBody.QuickbeltSlots[GM.CurrentPlayerBody.QuickbeltSlots.Count - 2].CurObject == null)
				{
					GetControllerFrom(__instance).SetActive(true);
				}
				else if (GM.CurrentPlayerBody.QuickbeltSlots[GM.CurrentPlayerBody.QuickbeltSlots.Count - 1].CurObject == null)
				{
					GetControllerFrom(__instance).SetActive(true);
				}
			}
		}

		#endregion

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

			if (input)
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
		private Vector4 Recolor(ConfigEntry<Vector4> cfg)
		{
			var color = new Vector4(4 * (cfg.Value[0] / 255), 4 * (cfg.Value[1] / 255), 4 * (cfg.Value[2] / 255), cfg.Value[3] / 1);
			return color;
		}

		// Return what geo we are using
		private static GameObject GetControllerFrom(Transform hand)
		{
			return GetControllerFrom(hand.GetComponent<FVRViveHand>());
		}
		private static GameObject GetControllerFrom(FVRViveHand hand)
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
