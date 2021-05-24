using BepInEx.Logging;
using BetterHands.Configs;
using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace BetterHands.Hooks
{
	public class MagPalm
	{
		private readonly int[] _handSlots = new int[2];

		private readonly RootConfig _config;
		private readonly ManualLogSource _loggers;

		private List<FVRQuickBeltSlot> _qbList;

		public MagPalm(RootConfig config, ManualLogSource logger)
		{
			_config = config;
			_loggers = logger;
		}

		#region Hooks

		public void Hook()
		{
			if (_config.MagPalm.Enable.Value)
			{
				// MagPalm Creation
				On.FistVR.FVRPlayerBody.ConfigureQuickbelt += FVRPlayerBody_ConfigureQuickbelt;

				// MagPalm Functionality
				On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
				On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter += FVRFireArmClipTriggerClip_OnTriggerEnter;
				On.FistVR.FVRFireArmRound.FVRFixedUpdate += FVRFireArmRound_FVRFixedUpdate;
				On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay += PhysicalMagazineReleaseLatch_OnCollisionStay;
				On.FistVR.FVRPhysicalObject.ToggleQuickbeltState += FVRPhysicalObject_ToggleQuickbeltState;
				On.FistVR.FVRFireArmMagazine.FVRFixedUpdate += FVRFireArmMagazine_FVRFixedUpdate;

				// Pose Override
				On.FistVR.FVRPhysicalObject.GetGrabPos += FVRPhysicalObject_GetGrabPos;
				On.FistVR.FVRPhysicalObject.GetGrabRot += FVRPhysicalObject_GetGrabRot;

				// Controller Geo
				//On.FistVR.FVRViveHand.EndInteractionIfHeld += FVRViveHand_EndInteractionIfHeld;
				On.FistVR.FVRInteractiveObject.BeginInteraction += FVRInteractiveObject_BeginInteraction;

				// Input
				On.FistVR.FVRViveHand.Update += FVRViveHand_Update;
				On.FistVR.FVRMovementManager.FU += FVRMovementManager_FU;
			}
		}

		public void Unhook()
		{
			// MagPalm Creation
			On.FistVR.FVRPlayerBody.ConfigureQuickbelt -= FVRPlayerBody_ConfigureQuickbelt;

			// MagPalm Functionality
			On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter -= FVRFireArmReloadTriggerMag_OnTriggerEnter;
			On.FistVR.FVRFireArmClipTriggerClip.OnTriggerEnter -= FVRFireArmClipTriggerClip_OnTriggerEnter;
			On.FistVR.FVRFireArmRound.FVRFixedUpdate -= FVRFireArmRound_FVRFixedUpdate;
			On.FistVR.PhysicalMagazineReleaseLatch.OnCollisionStay -= PhysicalMagazineReleaseLatch_OnCollisionStay;
			On.FistVR.FVRPhysicalObject.ToggleQuickbeltState -= FVRPhysicalObject_ToggleQuickbeltState;
			On.FistVR.FVRFireArmMagazine.FVRFixedUpdate -= FVRFireArmMagazine_FVRFixedUpdate;

			// Pose Override
			On.FistVR.FVRPhysicalObject.GetGrabPos -= FVRPhysicalObject_GetGrabPos;
			On.FistVR.FVRPhysicalObject.GetGrabRot -= FVRPhysicalObject_GetGrabRot;

			// Controller Geo
			//On.FistVR.FVRViveHand.EndInteractionIfHeld -= FVRViveHand_EndInteractionIfHeld;
			On.FistVR.FVRInteractiveObject.BeginInteraction -= FVRInteractiveObject_BeginInteraction;

			// Input
			On.FistVR.FVRViveHand.Update -= FVRViveHand_Update;
			On.FistVR.FVRMovementManager.FU -= FVRMovementManager_FU;
		}

		#endregion

		#region MagPalm Creation

		// Add mag palm slots after configuring quickbelt
		private void FVRPlayerBody_ConfigureQuickbelt(On.FistVR.FVRPlayerBody.orig_ConfigureQuickbelt orig, FVRPlayerBody self, int index)
		{
			orig(self, index);

			ConfigMagPalming(self.RightHand);
			ConfigMagPalming(self.LeftHand);

			_qbList = GM.CurrentPlayerBody.QuickbeltSlots;
		}

		private void ConfigMagPalming(Transform hand)
		{
			var cfg = _config.MagPalm;

			// Backpack is a part of all quickbelt layouts, use that to create our slots
			var bod = GM.CurrentPlayerBody;
			var slot = bod.Torso.Find("QuickBeltSlot_Backpack");
			var qb = bod.QuickbeltSlots;

			if (slot != null)
			{
				var fvrhand = hand.GetComponent<FVRViveHand>();
				var pose = new GameObject().transform;
				pose.parent = fvrhand.PoseOverride;

				var isRightHand = true;
				if (fvrhand.IsThisTheRightHand)
				{
					pose.localPosition = cfg.Position.Value;
					pose.localRotation = Quaternion.Euler(cfg.Rotation.Value);
				}
				else
				{
					isRightHand = false;
					// mirror configs for left hand
					pose.localPosition = Vector3.Scale(cfg.Position.Value, new Vector3(-1, 1, 1));
					pose.localRotation = Quaternion.Euler(Vector3.Scale(cfg.Rotation.Value, new Vector3(1, -1, -1)));
				}

				// Create & config our copy
				var newSlot = GameObject.Instantiate(slot, pose);
				newSlot.localPosition = Vector3.zero;
				var geo = newSlot.Find("QB_TransformTarget");
				geo.GetChild(0).localScale = new Vector3(0.001f, 0.001f, 0.001f);
				geo.GetChild(1).localScale = new Vector3(0.001f, 0.001f, 0.001f);

				var newSlotQB = newSlot.GetComponent<FVRQuickBeltSlot>();
				newSlotQB.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
				newSlotQB.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear;
				newSlotQB.name = hand.name;

				if (!cfg.Controls.Interactable.Value)
				{
					geo.GetChild(0).gameObject.SetActive(false);
					geo.GetChild(1).gameObject.SetActive(false);
					newSlotQB.IsSelectable = false;
				}

				qb.Add(newSlotQB);
				if (isRightHand)
				{
					_handSlots[0] = qb.Count - 1;
				}
				else
				{
					_handSlots[1] = qb.Count - 1;
				}

			}
		}

		#endregion

		#region MagPalm Functionality

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

				if (fvrObj != null && ObjInPalmSlot(fvrObj.QuickbeltSlot))
				{
					if (col.gameObject.CompareTag(layer))
					{
						return true;
					}
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
				var hand = GetHandFromSlot(slot);
				hand.Buzz(hand.OtherHand.Buzzer.Buzz_BeginInteraction);
				hand.UpdateControllerDefinition();
			}
		}

		// Allow palmed rounds to load into firearms
		private void FVRFireArmRound_FVRFixedUpdate(On.FistVR.FVRFireArmRound.orig_FVRFixedUpdate orig, FVRFireArmRound self)
		{
			orig(self);

			var qbSlot = self.m_quickbeltSlot;
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && ObjInPalmSlot(qbSlot))
			{
				var chamber = self.HoveredOverChamber;
				if (chamber != null && self.isManuallyChamberable && !chamber.IsFull && chamber.IsAccessible)
				{
					self.Chamber(chamber, true);
					GetHandFromSlot(qbSlot).UpdateControllerDefinition();
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

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && ObjInPalmSlot(self.m_quickbeltSlot))
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
				if ((GM.Options.ControlOptions.UseEasyMagLoading || _config.MagPalm.EasyPalmLoading.Value) && ObjInPalmSlot(qbSlot))
				{
					var bod = GM.CurrentPlayerBody;
					var hand = qbSlot.name == bod.LeftHand.name ? bod.LeftHand.GetComponent<FVRViveHand>() : bod.RightHand.GetComponent<FVRViveHand>();
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

		#endregion

		#region Pose Override

		private Vector3 FVRPhysicalObject_GetGrabPos(On.FistVR.FVRPhysicalObject.orig_GetGrabPos orig, FVRPhysicalObject self)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && ObjInPalmSlot(self.QuickbeltSlot))
			{
				return self.PoseOverride.position;
			}

			return orig(self);
		}

		private Quaternion FVRPhysicalObject_GetGrabRot(On.FistVR.FVRPhysicalObject.orig_GetGrabRot orig, FVRPhysicalObject self)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && ObjInPalmSlot(self.QuickbeltSlot))
			{
				return self.PoseOverride.rotation;
			}

			return orig(self);
		}

		#endregion

		#region Controller Geo

		// Return controller geo if palmed item is removed
		private void FVRInteractiveObject_BeginInteraction(On.FistVR.FVRInteractiveObject.orig_BeginInteraction orig, FVRInteractiveObject self, FVRViveHand hand)
		{
			orig(self, hand);

			if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				var otherHand = self.m_hand.OtherHand;
				if (IsSpecificPalmSlotEmpty(otherHand))
				{
					otherHand.UpdateControllerDefinition();
				}
			}
		}

		#endregion

		#region Input Patches

		// Input hook
		private void FVRViveHand_Update(On.FistVR.FVRViveHand.orig_Update orig, FVRViveHand self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				// Get input from hand & config
				var cfg = _config.MagPalm.Controls;
				var value = self.IsThisTheRightHand ? cfg.RightKeybind.Value : cfg.LeftKeybind.Value;
				var handInput = self.Input;
				var magnitude = handInput.TouchpadAxes.magnitude > cfg.ClickPressure.Value;
				var input = value switch
				{
					MagPalmControlsConfig.Keybind.AXButton => handInput.AXButtonDown,
					MagPalmControlsConfig.Keybind.BYButton => handInput.BYButtonDown,
					MagPalmControlsConfig.Keybind.Grip => handInput.GripDown,
					MagPalmControlsConfig.Keybind.Secondary2AxisNorth => handInput.Secondary2AxisNorthDown,
					MagPalmControlsConfig.Keybind.Secondary2AxisSouth => handInput.Secondary2AxisSouthDown,
					MagPalmControlsConfig.Keybind.Secondary2AxisEast => handInput.Secondary2AxisEastDown,
					MagPalmControlsConfig.Keybind.Secondary2AxisWest => handInput.Secondary2AxisWestDown,
					MagPalmControlsConfig.Keybind.TouchpadClickNorth => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.up) <= 45f,
					MagPalmControlsConfig.Keybind.TouchpadClickSouth => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.down) <= 45f,
					MagPalmControlsConfig.Keybind.TouchpadClickEast => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.right) <= 45f,
					MagPalmControlsConfig.Keybind.TouchpadClickWest => handInput.TouchpadDown && magnitude && Vector2.Angle(handInput.TouchpadAxes, Vector2.left) <= 45f,
					MagPalmControlsConfig.Keybind.TouchpadTapNorth => handInput.TouchpadNorthDown,
					MagPalmControlsConfig.Keybind.TouchpadTapSouth => handInput.TouchpadSouthDown,
					MagPalmControlsConfig.Keybind.TouchpadTapEast => handInput.TouchpadEastDown,
					MagPalmControlsConfig.Keybind.TouchpadTapWest => handInput.TouchpadWestDown,
					MagPalmControlsConfig.Keybind.Trigger => handInput.TriggerDown,
					_ => false,
				};

				if (input && GrabbityProtection(self, value))
				{
					MagPalmInput(self);
				}
			}
		}

		private void MagPalmInput(FVRViveHand hand)
		{
			// Get magpalm index
			var slot = hand.transform == GM.CurrentPlayerBody.RightHand ? _qbList[_handSlots[0]] : _qbList[_handSlots[1]];

			var obj = slot.CurObject;
			var currItem = hand.CurrentInteractable;

			// If current hand is empty, retrieve the object
			if (currItem == null && obj != null)
			{
				hand.RetrieveObject(obj);

				// Make controller geo visible if slots & hands empty
				if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld && IsSpecificPalmSlotEmpty(hand))
				{
					hand.UpdateControllerDefinition();
				}
			}

			// else if palming something & allowed, swap the current hand and hand slot items
			else if (currItem is FVRPhysicalObject item && AllowPalming(item))
			{
				item.ForceBreakInteraction();
				item.SetAllCollidersToLayer(false, "NoCol");

				if (obj != null)
				{
					item.transform.position = obj.transform.position;
					hand.RetrieveObject(obj);
				}

				item.ForceObjectIntoInventorySlot(slot);
				item.SetAllCollidersToLayer(false, "Default");

				if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
				{
					Plugin.GetControllerFrom(hand).SetActive(false);
				}
			}
		}

		// If mag palm keybind matches grabbity keybind, suppress mag palm input if grabbity sphere is on an item
		private bool GrabbityProtection(FVRViveHand hand, MagPalmControlsConfig.Keybind keybind)
		{
			if (_config.MagPalm.Controls.GrabbityProtection.Value)
			{
				var grabbityState = GM.Options.ControlOptions.WIPGrabbityButtonState;
				if (grabbityState == ControlOptions.WIPGrabbityButton.Trigger && (keybind == MagPalmControlsConfig.Keybind.Trigger)
					|| grabbityState == ControlOptions.WIPGrabbityButton.Grab && (keybind == MagPalmControlsConfig.Keybind.Grip))
				{
					return !hand.Grabbity_HoverSphere.gameObject.activeSelf;
				}
			}

			return true;
		}

		// Returns true if the held object is valid for palming
		private bool AllowPalming(FVRPhysicalObject item)
		{
			var cheatCfg = _config.zCheat;
			if (item != null)
			{
				if (item is FVRFireArmMagazine mag && mag.Size <= cheatCfg.SizeLimit.Value || item is FVRFireArmClip || (item is FVRFireArmRound && _config.MagPalm.RoundPalm.Value) || cheatCfg.CursedPalms.Value && !item.m_isHardnessed)
				{
					return true;
				}
			}

			return false;
		}

		// CollisionPrevention patch
		private void FVRMovementManager_FU(On.FistVR.FVRMovementManager.orig_FU orig, FVRMovementManager self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && _config.MagPalm.CollisionPrevention.Value)
			{
				var layerName = (self.m_armSwingerVelocity + self.m_twoAxisVelocity).magnitude >= _config.MagPalm.CollisionPreventionVelocity.Value ? "NoCol" : "Default";

				for (var i = 0; i < _handSlots.Length; i++)
				{
					var obj = _qbList[_handSlots[i]].CurObject;
					if (obj != null)
					{
						// Look for first nontrigger collider and use that for comparison
						foreach (var col in obj.m_colliders)
						{
							if (!col.isTrigger && col.gameObject.layer != LayerMask.NameToLayer(layerName))
							{
								obj.SetAllCollidersToLayer(false, layerName);
								break;
							}
						}
					}
				}
			}
		}

		#endregion

		#region MagPalm Slot Checks

		// Is object in any magpalm slot
		private bool ObjInPalmSlot(FVRQuickBeltSlot slot)
		{
			return (slot != null && (slot == _qbList[_handSlots[0]] || slot == _qbList[_handSlots[1]]));
		}

		private bool IsSpecificPalmSlotEmpty(FVRViveHand hand)
		{
			if (hand != null)
			{
				var qbSlot = hand.IsThisTheRightHand ? _qbList[_handSlots[0]] : _qbList[_handSlots[1]];

				if (qbSlot.CurObject == null)
				{
					return true;
				}
			}

			return false;
		}

		private FVRViveHand GetHandFromSlot(FVRQuickBeltSlot slot)
		{
			var bod = GM.CurrentPlayerBody;

			return slot == _qbList[_handSlots[0]] ? bod.RightHand.GetComponent<FVRViveHand>() : bod.LeftHand.GetComponent<FVRViveHand>();
		}

		#endregion
	}
}
