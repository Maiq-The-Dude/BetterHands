using FistVR;
using HarmonyLib;
using UnityEngine;

namespace BetterHands
{
	internal class Patches
	{
		// Allow mags from our hand slots to load into firearms
		[HarmonyPatch(typeof(FVRFireArmReloadTriggerMag), nameof(FVRFireArmReloadTriggerMag.OnTriggerEnter))]
		[HarmonyPostfix]
		private static void PatchMagTrigger(FVRFireArmReloadTriggerMag __instance, Collider collider)
		{
			var mag = __instance.Magazine;
			var qbSlots = GM.CurrentPlayerBody.QuickbeltSlots;
			if (mag.QuickbeltSlot == qbSlots[qbSlots.Count - 1] || mag.QuickbeltSlot == qbSlots[qbSlots.Count - 2])
			{
				if (mag != null && mag.FireArm == null && collider.gameObject.tag == "FVRFireArmReloadTriggerWell")
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
		private static void CurrentInteractablePatch(FVRViveHand __instance)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				var qbSlots = GM.CurrentPlayerBody.QuickbeltSlots;
				if ((__instance.IsThisTheRightHand && qbSlots[qbSlots.Count - 2].CurObject == null) || (qbSlots[qbSlots.Count - 1].CurObject == null))
				{
					Plugin.GetControllerFrom(__instance).SetActive(true);
				}
			}
		}

		// Allow mags in hand slots to hit physical mag releases
		[HarmonyPatch(typeof(PhysicalMagazineReleaseLatch), nameof(PhysicalMagazineReleaseLatch.OnCollisionEnter))]
		[HarmonyPostfix]
		private static void PhysicalMagReleasePatch(PhysicalMagazineReleaseLatch __instance, Collision col)
		{
			if (col.collider.attachedRigidbody != null && col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>() != null)
			{
				var mag = col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
				var qbSlots = GM.CurrentPlayerBody.QuickbeltSlots;
				if (mag == qbSlots[qbSlots.Count - 1].CurObject || mag == qbSlots[qbSlots.Count - 2].CurObject)
				{
					__instance.timeSinceLastCollision = 0f;
				}
			}
		}
	}
}
