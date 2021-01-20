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
		public static void ReloadTriggerMag_Patch(FVRFireArmReloadTriggerMag __instance, Collider collider)
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
		public static void CurrentInteractable_Patch(FVRViveHand __instance)
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
		public static void PhysicalMagRelease_Patch(PhysicalMagazineReleaseLatch __instance, Collision col)
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
}
