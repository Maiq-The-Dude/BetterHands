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

						// very cool code to set return the hand to visible
						Plugin.GetControllerFrom(mag.FireArm.m_hand.OtherHand).SetActive(true);
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
						Plugin.GetControllerFrom(__instance).SetActive(true);
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
	}
	#endregion
}
