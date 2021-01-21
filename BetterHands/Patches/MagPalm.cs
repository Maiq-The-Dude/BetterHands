using BetterHands.Configs;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace BetterHands.Patches
{
	internal class MagPalmPatches
	{
		private static RootConfig _configs => Plugin.Instance.Configs;

		#region Quickbelt Creation Patch

		// Add mag palm slots after configuring quickbelt
		[HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.ConfigureQuickbelt))]
		[HarmonyPostfix]
		private static void ConfigureQuickbelt_Patch(FVRPlayerBody __instance, int index)
		{
			ConfigMagPalming(__instance.RightHand);
			ConfigMagPalming(__instance.LeftHand);
		}

		private static void ConfigMagPalming(Transform hand)
		{
			var cfg = _configs.MagPalm;

			// Backpack is a part of all quickbelt layouts, use that to create our slots
			var bod = GM.CurrentPlayerBody;
			var slot = bod.Torso.Find("QuickBeltSlot_Backpack");
			var qb = bod.QuickbeltSlots;

			if (slot != null)
			{
				var fvrhand = hand.GetComponent<FVRViveHand>();
				var pose = new GameObject().transform;
				pose.parent = fvrhand.PoseOverride;

				if (fvrhand.IsThisTheRightHand)
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
		#endregion

		#region Quickbelt Functionality Patches

		// Allow mags from our hand slots to load into firearms
		[HarmonyPatch(typeof(FVRFireArmReloadTriggerMag), nameof(FVRFireArmReloadTriggerMag.OnTriggerEnter))]
		[HarmonyPostfix]
		private static void ReloadTriggerMag_Patch(FVRFireArmReloadTriggerMag __instance, Collider collider)
		{
			var mag = __instance.Magazine;
			var bod = GM.CurrentPlayerBody;
			if (mag != null && mag.QuickbeltSlot != null && (mag.QuickbeltSlot.name == bod.RightHand.name || mag.QuickbeltSlot.name == bod.LeftHand.name))
			{
				if (mag.FireArm == null && collider.gameObject.tag == "FVRFireArmReloadTriggerWell")
				{
					var triggerWell = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
					var firearm = triggerWell.FireArm;
					var magType = firearm.MagazineType;
					if (triggerWell.UsesTypeOverride)
					{
						magType = triggerWell.TypeOverride;
					}
					if (magType == mag.MagazineType && (firearm.EjectDelay <= 0f || mag != firearm.LastEjectedMag) && firearm.Magazine == null)
					{
						mag.SetQuickBeltSlot(null);
						mag.Load(firearm);

						// set hand geo to active
						mag.FireArm.m_hand.OtherHand.UpdateControllerDefinition();
					}
				}
			}
		}

		// Makes controller geo visible if slots & hands empty
		[HarmonyPatch(typeof(FVRViveHand), "CurrentInteractable", MethodType.Setter)]
		[HarmonyPostfix]
		private static void CurrentInteractable_Patch(FVRViveHand __instance)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				var qb = GM.CurrentPlayerBody.QuickbeltSlots;
				for (var i = 0; i < qb.Count; i++)
				{
					if (qb[i].name == __instance.name && qb[i].CurObject == null)
					{
						// set hand geo to active
						__instance.UpdateControllerDefinition();
						break;
					}
				}
			}
		}

		// Allow mags in hand slots to hit physical mag releases
		[HarmonyPatch(typeof(PhysicalMagazineReleaseLatch), nameof(PhysicalMagazineReleaseLatch.OnCollisionEnter))]
		[HarmonyPostfix]
		private static void PhysicalMagRelease_Patch(PhysicalMagazineReleaseLatch __instance, Collision col)
		{
			if (col.collider.attachedRigidbody != null && col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>() != null)
			{
				var mag = col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
				var bod = GM.CurrentPlayerBody;
				var qb = bod.QuickbeltSlots;
				for (var i = 0; i < qb.Count; i++)
				{
					if (qb[i].name == bod.RightHand.name || qb[i].name == bod.LeftHand.name)
					{
						__instance.timeSinceLastCollision = 0f;
						break;
					}
				}
			}
		}
		#endregion
		
		#region Input Patch

		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.Update))]
		[HarmonyPostfix]
		private static void FVRViveHand_Update_Patch(FVRViveHand __instance)
		{
			if (_configs.MagPalm.Enable.Value && GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				// Get input from hand & config
				var cfg = _configs.MagPalm;
				var value = __instance.IsThisTheRightHand ? cfg.RightKeybind.Value : cfg.LeftKeybind.Value;
				var handInput = __instance.Input;
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

				if (input && GrabbityProtection(__instance, value))
				{
					MagPalmInput(__instance, input);
				}
			}
		}

		private static void MagPalmInput(FVRViveHand hand, bool input)
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
							// hide geo
							Plugin.GetControllerFrom(hand).SetActive(false);
						}
					}
					break;
				}
			}
		}

		// If mag palm keybind matches grabbity keybind, suppress mag palm input if grabbity sphere is on an item
		private static bool GrabbityProtection(FVRViveHand hand, MagPalmConfig.Keybind keybind)
		{
			if (_configs.MagPalm.GrabbityProtection.Value)
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
		private static bool AllowPalming(FVRInteractiveObject item)
		{
			var cfg = _configs.zCheat;
			if (item is FVRFireArmMagazine mag && mag.Size <= cfg.SizeLimit.Value || cfg.CursedPalms.Value)
			{
				return true;
			}

			return false;
		}
		#endregion
	}
}
