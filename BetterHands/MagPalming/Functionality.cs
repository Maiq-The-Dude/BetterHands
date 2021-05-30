using BetterHands.Configs;
using FistVR;
using UnityEngine;

namespace BetterHands.MagPalming
{
	public class MagPalmFunctionality
	{
		private readonly MagPalm MP;
		private readonly RootConfig _config;

		public MagPalmFunctionality(MagPalm mp)
		{
			MP = mp;
			_config = MP.Config;
		}

		public void Hook()
		{
			On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
			On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter += FVRFireArmClipTriggerClip_OnTriggerEnter;
			On.FistVR.FVRFireArmRound.FVRFixedUpdate += FVRFireArmRound_FVRFixedUpdate;
			On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay += PhysicalMagazineReleaseLatch_OnCollisionStay;
			On.FistVR.FVRPhysicalObject.ToggleQuickbeltState += FVRPhysicalObject_ToggleQuickbeltState;
			On.FistVR.FVRFireArmMagazine.FVRFixedUpdate += FVRFireArmMagazine_FVRFixedUpdate;
		}

		public void Unhook()
		{
			On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter -= FVRFireArmReloadTriggerMag_OnTriggerEnter;
			On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter -= FVRFireArmClipTriggerClip_OnTriggerEnter;
			On.FistVR.FVRFireArmRound.FVRFixedUpdate -= FVRFireArmRound_FVRFixedUpdate;
			On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay -= PhysicalMagazineReleaseLatch_OnCollisionStay;
			On.FistVR.FVRPhysicalObject.ToggleQuickbeltState -= FVRPhysicalObject_ToggleQuickbeltState;
			On.FistVR.FVRFireArmMagazine.FVRFixedUpdate -= FVRFireArmMagazine_FVRFixedUpdate;
		}

		// Allow palmed mags to load into firearms
		private void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
		{
			orig(self, collider);

			var mag = self.Magazine;
			if (FirearmLoadCheck(mag, collider, "FVRFireArmReloadTriggerWell"))
			{
				var triggerWell = collider.GetComponent<FVRFireArmReloadTriggerWell>();
				var firearm = triggerWell.FireArm;
				if (mag.MagazineType == triggerWell.TypeOverride || mag.MagazineType == firearm.MagazineType && (firearm.EjectDelay <= 0f || mag != firearm.LastEjectedMag) && firearm.Magazine == null)
				{
					// Remove mag from qb, load, buzz loading hand, and unhide controller geo
					mag.SetQuickBeltSlot(null);
					mag.Load(firearm);
					BuzzAndUpdateControllerDef(mag.QuickbeltSlot, mag.FireArm);
				}
			}
		}

		// Allow palmed clips to load into firearms
		private void FVRFireArmClipTriggerClip_OnTriggerEnter(On.FistVR.FVRFireArmClipTriggerClip.orig_OnTriggerEnter orig, FVRFireArmClipTriggerClip self, Collider collider)
		{
			orig(self, collider);

			var clip = self.Clip;
			if (FirearmLoadCheck(clip, collider, "FVRFireArmClipReloadTriggerWell"))
			{
				var triggerWell = collider.gameObject.GetComponent<FVRFireArmClipTriggerWell>();
				var firearm = triggerWell.FireArm;
				if (triggerWell != null && triggerWell.FireArm != null && firearm.ClipType == clip.ClipType && firearm.ClipEjectDelay <= 0f && firearm.Clip == null)
				{
					// Remove from qb, load, buzz loading hand, and unhide controller geo
					clip.SetQuickBeltSlot(null);
					clip.Load(firearm);
					BuzzAndUpdateControllerDef(clip.QuickbeltSlot, clip.FireArm);
				}
			}
		}

		// Shared load checks between mag & clip
		private bool FirearmLoadCheck(FVRPhysicalObject obj, Collider col, string layer)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				FVRPhysicalObject fvrObj = null;

				if (obj is FVRFireArmMagazine mag && mag.FireArm == null)
				{
					fvrObj = mag;
				}
				else if (obj is FVRFireArmClip clip && clip.FireArm == null)
				{
					fvrObj = clip;
				}

				if (fvrObj != null && MP.ObjInPalmSlot(fvrObj.QuickbeltSlot))
				{
					return (col.gameObject.CompareTag(layer));
				}
			}

			return false;
		}

		private void BuzzAndUpdateControllerDef(FVRQuickBeltSlot slot, FVRFireArm fireArm)
		{
			if (fireArm.IsHeld)
			{
				fireArm.m_hand.OtherHand.Buzz(fireArm.m_hand.Buzzer.Buzz_BeginInteraction);
				fireArm.m_hand.OtherHand.UpdateControllerDefinition();
			}
			else if (fireArm.QuickbeltSlot != null)
			{
				var hand = MP.GetHandFromSlot(slot);
				hand.Buzz(hand.OtherHand.Buzzer.Buzz_BeginInteraction);
				hand.UpdateControllerDefinition();
			}
		}

		// Allow palmed rounds to load into firearms
		private void FVRFireArmRound_FVRFixedUpdate(On.FistVR.FVRFireArmRound.orig_FVRFixedUpdate orig, FVRFireArmRound self)
		{
			orig(self);

			var qbSlot = self.m_quickbeltSlot;
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && MP.ObjInPalmSlot(qbSlot))
			{
				var chamber = self.HoveredOverChamber;
				if (chamber != null && self.isManuallyChamberable && !chamber.IsFull && chamber.IsAccessible)
				{
					self.Chamber(chamber, true);
					MP.GetHandFromSlot(qbSlot).UpdateControllerDefinition();
					UnityEngine.Object.Destroy(self.gameObject);
				}
			}
		}

		// Allow palmed mags to hit physical mag releases
		private void PhysicalMagazineReleaseLatch_OnCollisionStay(On.FistVR.PhysicalMagazineReleaseLatch.orig_OnCollisionStay orig, PhysicalMagazineReleaseLatch self, Collision col)
		{
			orig(self, col);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				if (col.collider.attachedRigidbody != null && col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>() != null)
				{
					self.timeSinceLastCollision = 0f;
				}
			}
		}

		// Disable spawnlocking on magpalm slots
		private void FVRPhysicalObject_ToggleQuickbeltState(On.FistVR.FVRPhysicalObject.orig_ToggleQuickbeltState orig, FVRPhysicalObject self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && MP.ObjInPalmSlot(self.m_quickbeltSlot))
			{
				self.m_isSpawnLock = false;
				self.m_isHardnessed = false;
			}
		}

		// Easy mag load support
		private void FVRFireArmMagazine_FVRFixedUpdate(On.FistVR.FVRFireArmMagazine.orig_FVRFixedUpdate orig, FVRFireArmMagazine self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				var qbSlot = self.QuickbeltSlot;
				if ((GM.Options.ControlOptions.UseEasyMagLoading || _config.MagPalm.EasyPalmLoading.Value) && MP.ObjInPalmSlot(qbSlot))
				{
					var bod = GM.CurrentPlayerBody;
					var hand = qbSlot.name == bod.RightHand.name ? bod.RightHand.GetComponent<FVRViveHand>() : bod.LeftHand.GetComponent<FVRViveHand>();
					if (hand.OtherHand.CurrentInteractable is FVRFireArm firearm && firearm != null && firearm.MagazineType == self.MagazineType && firearm.GetMagMountPos(self.IsBeltBox) != null)
					{
						if (Vector3.Distance(self.RoundEjectionPos.position, firearm.GetMagMountPos(self.IsBeltBox).position) <= 0.15f)
						{
							self.SetAllCollidersToLayer(false, "NoCol");
							self.IsNonPhysForLoad = true;
						}
						else
						{
							self.SetAllCollidersToLayer(false, "Default");
							self.IsNonPhysForLoad = false;
						}
					}
				}
			}
		}
	}
}