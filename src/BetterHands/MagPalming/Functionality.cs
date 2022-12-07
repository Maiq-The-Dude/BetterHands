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

		#region Hooks

		public void Hook()
		{
			On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
			On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter += FVRFireArmClipTriggerClip_OnTriggerEnter;
			On.FistVR.FVRFireArmRound.FVRFixedUpdate += FVRFireArmRound_FVRFixedUpdate;
			On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay += PhysicalMagazineReleaseLatch_OnCollisionStay;
			On.FistVR.FVRPhysicalObject.ToggleQuickbeltState += FVRPhysicalObject_ToggleQuickbeltState;
			On.FistVR.FVRFireArmMagazine.FVRFixedUpdate += FVRFireArmMagazine_FVRFixedUpdate;
			On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
		}

		public void Unhook()
		{
			On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter -= FVRFireArmReloadTriggerMag_OnTriggerEnter;
			On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter -= FVRFireArmClipTriggerClip_OnTriggerEnter;
			On.FistVR.FVRFireArmRound.FVRFixedUpdate -= FVRFireArmRound_FVRFixedUpdate;
			On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay -= PhysicalMagazineReleaseLatch_OnCollisionStay;
			On.FistVR.FVRPhysicalObject.ToggleQuickbeltState -= FVRPhysicalObject_ToggleQuickbeltState;
			On.FistVR.FVRFireArmMagazine.FVRFixedUpdate -= FVRFireArmMagazine_FVRFixedUpdate;
			On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire;
		}

		#endregion Hooks

		#region Patches

		// Allow palmed mags to load into firearms
		private void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
		{
			orig(self, collider);

			var mag = self.Magazine;
			if (FirearmLoadCheck(mag, collider, "FVRFireArmReloadTriggerWell"))
			{
				var triggerWell = collider.GetComponent<FVRFireArmReloadTriggerWell>();
				var firearm = triggerWell.FireArm;
				var afirearm = triggerWell.AFireArm;
				if (triggerWell.UsesSecondaryMagSlots && firearm != null && triggerWell.FireArm.SecondaryMagazineSlots[triggerWell.SecondaryMagSlotIndex].Magazine == null)
				{
					// Remove mag from qb, load, buzz loading hand, and unhide controller geo
					mag.SetQuickBeltSlot(null);
					mag.LoadIntoSecondary(firearm, triggerWell.SecondaryMagSlotIndex);
					BuzzAndUpdateControllerDef(mag.QuickbeltSlot, mag.FireArm);
				}
				else if (firearm != null && (mag.MagazineType == triggerWell.TypeOverride || (mag.MagazineType == firearm.MagazineType && (firearm.EjectDelay <= 0f || mag != firearm.LastEjectedMag) && firearm.Magazine == null)))
				{
					mag.SetQuickBeltSlot(null);
					mag.Load(firearm);
					BuzzAndUpdateControllerDef(mag.QuickbeltSlot, mag.FireArm);
				}
				else if (afirearm != null && (mag.MagazineType == triggerWell.TypeOverride || mag.MagazineType == afirearm.MagazineType && (afirearm.EjectDelay <= 0f || mag != afirearm.LastEjectedMag) && afirearm.Magazine == null))
				{
					mag.SetQuickBeltSlot(null);
					mag.Load(afirearm);
					BuzzAndUpdateControllerDef();
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
				if (triggerWell?.FireArm != null && firearm.ClipType == clip.ClipType && firearm.ClipEjectDelay <= 0f && firearm.Clip == null)
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
					return col.gameObject.CompareTag(layer);
				}
			}

			return false;
		}

		// CursedTriggers
		private void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
		{
			orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);

			var config = _config.zCheat;
			if (config.CursedTriggers.Value)
			{
				foreach (var attachment in self.AttachmentsList)
				{
					FireChildren(attachment);
				}
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
				else if (self.m_hoverOverReloadTrigger != null)
				{
					if (self.m_hoverOverReloadTrigger.Magazine != null)
					{
						var mag = self.m_hoverOverReloadTrigger.Magazine;
						if (mag.RoundType == self.RoundType)
						{
							mag.AddRound(self, true, true, true);
							DestroyRound(self, qbSlot);
						}
					}
					else if (self.m_hoverOverReloadTrigger.Clip != null)
					{
						var clip = self.m_hoverOverReloadTrigger.Clip;
						if (clip.RoundType == self.RoundType)
						{
							clip.AddRound(self, true, true);
							DestroyRound(self, qbSlot);
						}
					}
				}
			}
		}

		// Allow palmed mags to hit physical mag releases
		private void PhysicalMagazineReleaseLatch_OnCollisionStay(On.FistVR.PhysicalMagazineReleaseLatch.orig_OnCollisionStay orig, PhysicalMagazineReleaseLatch self, Collision col)
		{
			orig(self, col);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				if (col.collider.attachedRigidbody?.gameObject.GetComponent<FVRPhysicalObject>() != null)
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

		#endregion Patches

		private void BuzzAndUpdateControllerDef()
		{
			GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>().UpdateControllerDefinition();
			GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>().UpdateControllerDefinition();
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

		private void DestroyRound(FVRFireArmRound round, FVRQuickBeltSlot qbSlot)
		{
			Object.Destroy(round.gameObject);
			MP.GetHandFromSlot(qbSlot).UpdateControllerDefinition();
		}

		private void FireChildren(FVRFireArmAttachment attachment)
		{
			if (attachment is AttachableFirearmPhysicalObject attachableFPO && attachableFPO?.FA != null)
			{
				var attachableGun = attachableFPO.FA;
				if (attachableGun is AttachableClosedBoltWeapon cbWeapon)
				{
					cbWeapon.Fire(true);
				}
				else if (attachableGun is AttachableTubeFed tubeFed)
				{
					tubeFed.Fire(true);
				}
				else if (attachableGun is AttachableBreakActions breakAction)
				{
					breakAction.Fire(true);
				}
			}
		}
	}
}