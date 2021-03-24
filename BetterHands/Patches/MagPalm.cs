using BetterHands.Configs;
using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BetterHands.Patches
{
	internal class MagPalmPatches
	{
		private static RootConfig _configs => Plugin.Instance.Configs;

		private static readonly int[] _handSlots = new int[2];

		private static List<FVRQuickBeltSlot> _qbList;

		#region MagPalm Creation Patch

		// Add mag palm slots after configuring quickbelt
		[HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.ConfigureQuickbelt))]
		[HarmonyPostfix]
		private static void ConfigureQuickbelt_Patch(FVRPlayerBody __instance, int index)
		{
			ConfigMagPalming(__instance.RightHand);
			ConfigMagPalming(__instance.LeftHand);

			_qbList = GM.CurrentPlayerBody.QuickbeltSlots;
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

		#region MagPalm Functionality Patches

		// Allow palmed mags to load into firearms
		[HarmonyPatch(typeof(FVRFireArmReloadTriggerMag), nameof(FVRFireArmReloadTriggerMag.OnTriggerEnter))]
		[HarmonyPostfix]
		private static void FVRFireArmReloadTriggerMag_Patch(FVRFireArmReloadTriggerMag __instance, Collider collider)
		{
			var mag = __instance.Magazine;
			if (FirearmLoadCheck(mag, collider, "FVRFireArmReloadTriggerWell"))
			{
				var triggerWell = collider.GetComponent<FVRFireArmReloadTriggerWell>();
				var firearm = triggerWell.FireArm;
				if (mag.MagazineType == triggerWell.TypeOverride || mag.MagazineType == firearm.MagazineType && (firearm.EjectDelay <= 0f || mag != firearm.LastEjectedMag) && firearm.Magazine == null)
				{
					// in case firearm is in a qb slot
					var slot = mag.QuickbeltSlot;

					// Remove mag from qb, load, buzz loading hand, and unhide controller geo
					mag.m_isSpawnLock = false;
					mag.SetQuickBeltSlot(null);
					mag.Load(firearm);
					if (firearm.QuickbeltSlot != null)
					{
						var hand = GetHandFromSlot(slot);
						hand.Buzz(hand.OtherHand.Buzzer.Buzz_BeginInteraction);
						hand.UpdateControllerDefinition();
					}
					else
					{
						mag.FireArm.m_hand.OtherHand.Buzz(mag.FireArm.m_hand.Buzzer.Buzz_BeginInteraction);
						mag.FireArm.m_hand.OtherHand.UpdateControllerDefinition();
					}
				}
			}
		}

		// Allow palmed clips to load into firearms
		[HarmonyPatch(typeof(FVRFireArmClipTriggerClip), nameof(FVRFireArmClipTriggerClip.OnTriggerEnter))]
		[HarmonyPostfix]
		private static void FVRFireArmClipTriggerClip_Patch(FVRFireArmClipTriggerClip __instance, Collider collider)
		{
			var clip = __instance.Clip;
			if (FirearmLoadCheck(clip, collider, "FVRFireArmClipReloadTriggerWell"))
			{
				var triggerWell = collider.gameObject.GetComponent<FVRFireArmClipTriggerWell>();
				var firearm = triggerWell.FireArm;
				if (triggerWell != null && triggerWell.FireArm != null && firearm.ClipType == clip.ClipType && firearm.ClipEjectDelay <= 0f && firearm.Clip == null)
				{
					// Remove from qb, load, buzz loading hand, and unhide controller geo
					clip.m_isSpawnLock = false;
					clip.SetQuickBeltSlot(null);
					clip.Load(firearm);
					clip.FireArm.m_hand.OtherHand.Buzz(clip.FireArm.m_hand.Buzzer.Buzz_BeginInteraction);
					clip.FireArm.m_hand.OtherHand.UpdateControllerDefinition();
				}
			}
		}

		// Shared load checks between mag & clip
		private static bool FirearmLoadCheck(FVRPhysicalObject obj, Collider col, string layer)
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

				if (fvrObj != null && fvrObj.QuickbeltSlot != null && (fvrObj.QuickbeltSlot == _qbList[_handSlots[0]] || fvrObj.QuickbeltSlot == _qbList[_handSlots[1]]))
				{
					if (col.gameObject.CompareTag(layer))
					{
						return true;
					}
				}
			}

			return false;
		}

		// Allow palmed rounds to load into firearms
		[HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.FVRFixedUpdate))]
		[HarmonyPostfix]
		private static void FVRFireArmRound_FVRFixedUpdate_Patch(FVRFireArmRound __instance)
		{
			var qbSlot = __instance.m_quickbeltSlot;
			if (ObjInPalmSlot(qbSlot))
			{
				var chamber = __instance.HoveredOverChamber;
				if (chamber != null && __instance.isManuallyChamberable && !chamber.IsFull && chamber.IsAccessible)
				{
					__instance.Chamber(chamber, true);
					GetHandFromSlot(qbSlot).UpdateControllerDefinition();
					UnityEngine.Object.Destroy(__instance.gameObject);
				}
			}
		}

		// Allow palmed mags to hit physical mag releases
		[HarmonyPatch(typeof(PhysicalMagazineReleaseLatch), nameof(PhysicalMagazineReleaseLatch.OnCollisionEnter))]
		[HarmonyPostfix]
		private static void PhysicalMagRelease_Patch(PhysicalMagazineReleaseLatch __instance, Collision col)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && _configs.MagPalm.Enable.Value)
			{
				if (col.collider.attachedRigidbody != null && col.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>() != null)
				{

					__instance.timeSinceLastCollision = 0f;

				}
			}
		}

		// Disable spawnlocking on magpalm slots
		[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.ToggleQuickbeltState))]
		[HarmonyPostfix]
		private static void ToggleQuickbeltState_Patch(FVRPhysicalObject __instance)
		{
			if (ObjInPalmSlot(__instance.m_quickbeltSlot))
			{
				__instance.m_isSpawnLock = false;
				__instance.m_isHardnessed = false;
			}
		}

		// Easy mag load support
		[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.FVRFixedUpdate))]
		[HarmonyPostfix]
		private static void FVRFireArmMagazine_FVRFixedUpdate_Patch(FVRFireArmMagazine __instance)
		{
			var qbSlot = __instance.QuickbeltSlot;
			if ((GM.Options.ControlOptions.UseEasyMagLoading || _configs.MagPalm.EasyPalmLoading.Value) && ObjInPalmSlot(qbSlot))
			{
				var bod = GM.CurrentPlayerBody;
				var hand = bod.RightHand.GetComponent<FVRViveHand>();
				if (qbSlot.name == bod.LeftHand.name)
				{
					hand = bod.LeftHand.GetComponent<FVRViveHand>();
				}
				if (hand.OtherHand.CurrentInteractable is FVRFireArm firearm)
				{
					if (firearm != null && firearm.MagazineType == __instance.MagazineType && firearm.GetMagMountPos(__instance.IsBeltBox) != null)
					{
						if (Vector3.Distance(__instance.RoundEjectionPos.position, firearm.GetMagMountPos(__instance.IsBeltBox).position) <= 0.15f)
						{
							__instance.SetAllCollidersToLayer(false, "NoCol");
							__instance.IsNonPhysForLoad = true;
						}
						else
						{
							__instance.SetAllCollidersToLayer(false, "Default");
							__instance.IsNonPhysForLoad = false;
						}
					}
				}
			}
		}
		#endregion

		#region MagPalm Mirror Patches

		// Generic fire
		[HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.Fire))]
		[HarmonyPostfix]
		private static void FVRFireArm_Fire_Patch(FVRFireArm __instance)
		{
			var obj = MirrorGunChecks(__instance);
			if (obj != null)
			{
				if (obj is Handgun handgun)
				{
					handgun.Fire();
				}
				else if (obj is ClosedBoltWeapon closedBoltWeapon)
				{
					closedBoltWeapon.Fire();
					closedBoltWeapon.Bolt.m_isBoltLocked = false;
				}
				else if (obj is SimpleLauncher simpleLauncher)
				{
					simpleLauncher.Fire();
				}
			}
		}

		// Generic mag releases
		[HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.EjectMag))]
		[HarmonyPostfix]
		private static void FVRFireArm_EjectMag_Patch(FVRFireArm __instance)
		{
			var obj = MirrorGunChecks(__instance);
			if (obj != null)
			{
				if (obj is Handgun handgun && handgun.HasMagReleaseInput)
				{
					handgun.ReleaseMag();
				}
				else if (obj is ClosedBoltWeapon closedBoltWeapon && closedBoltWeapon.HasMagReleaseButton)
				{
					closedBoltWeapon.ReleaseMag();
				}
			}
		}

		// Mirrored guns are always two hand stabilized
		[HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.IsTwoHandStabilized))]
		[HarmonyPrefix]
		private static bool FVRFireArm_IsForegripStabilized_Patch(FVRFireArm __instance, ref bool __result)
		{
			var cfg = _configs.zCheat;
			if (cfg.CursedPalms.Value && cfg.MirroredGuns.Value)
			{
				var qbSlot = __instance.QuickbeltSlot;

				// Held gun, stabilize if palmed gun exists
				if (qbSlot == null)
				{
					var obj = MirrorGunChecks(__instance);
					if (obj != null)
					{
						__result = true;
						return false;
					}
				}

				// Palmed gun, always stabilize
				else if (qbSlot == _qbList[_handSlots[0]] || qbSlot == _qbList[_handSlots[1]])
				{
					__result = true;
					return false;
				}
			}

			return true;
		}

		// Handgun slide release
		[HarmonyPatch(typeof(Handgun), nameof(Handgun.DropSlideRelease))]
		[HarmonyPrefix]
		private static bool Handgun_EngageSlideRelease_Patch(Handgun __instance)
		{
			var obj = MirrorGunChecks(__instance);
			if (obj != null && obj is Handgun handgun && __instance.IsSlideLockUp)
			{
				handgun.DropSlideRelease();
			}
			return true;
		}

		#endregion

		#region Pose Override Patches

		[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.GetGrabPos))]
		[HarmonyPrefix]
		private static bool FVRPhysicalObject_GetGrabPos_Patch(FVRPhysicalObject __instance, ref Vector3 __result)
		{
			if (ObjInPalmSlot(__instance.QuickbeltSlot))
			{
				__result = __instance.PoseOverride.position;
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.GetGrabRot))]
		[HarmonyPrefix]
		private static bool FVRPhysicalObject_GetGrabRot_Patch(FVRPhysicalObject __instance, ref Quaternion __result)
		{
			if (ObjInPalmSlot(__instance.QuickbeltSlot))
			{
				__result = __instance.PoseOverride.rotation;
				return false;
			}
			return true;
		}
		#endregion

		#region Controller Geo Patches

		// Makes controller geo visible if slots & hands empty
		[HarmonyPatch(typeof(FVRViveHand), "CurrentInteractable", MethodType.Setter)]
		[HarmonyPostfix]
		private static void CurrentInteractable_Patch(FVRViveHand __instance)
		{
			if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				if (IsSpecificPalmSlotEmpty(__instance))
				{
					__instance.UpdateControllerDefinition();
				}
			}
		}

		// Return controller geo if palmed item is removed
		[HarmonyPatch(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.BeginInteraction))]
		[HarmonyPostfix]
		private static void FVRInteractiveObject_BeginInteraction_Patch(FVRInteractiveObject __instance)
		{
			if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				var otherHand = __instance.m_hand.OtherHand;
				if (IsSpecificPalmSlotEmpty(otherHand))
				{
					otherHand.UpdateControllerDefinition();
				}
			}
		}
		#endregion

		#region Input Patches

		// Input hook
		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.Update))]
		[HarmonyPostfix]
		private static void FVRViveHand_Update_Patch(FVRViveHand __instance)
		{
			if (_configs.MagPalm.Enable.Value && GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled)
			{
				// Get input from hand & config
				var cfg = _configs.MagPalm.Controls;
				var value = __instance.IsThisTheRightHand ? cfg.RightKeybind.Value : cfg.LeftKeybind.Value;
				var handInput = __instance.Input;
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

				if (input && GrabbityProtection(__instance, value))
				{
					MagPalmInput(__instance);
				}
			}
		}

		private static void MagPalmInput(FVRViveHand hand)
		{
			// Get magpalm index here so quickbelt layout doesn't break retrieval mid-scene work
			var slot = _qbList[_handSlots[0]];

			if (hand.transform == GM.CurrentPlayerBody.LeftHand)
			{
				slot = _qbList[_handSlots[1]];
			}

			var obj = slot.CurObject;
			var currItem = hand.CurrentInteractable;

			// If current hand is empty, retrieve the object
			if (currItem == null && obj != null)
			{
				hand.RetrieveObject(obj);
			}

			// else if palming something & not harnessed, swap the current hand and hand slot items
			else if (currItem is FVRPhysicalObject item && AllowPalming(item))
			{
				item.ForceBreakInteraction();
				item.SetAllCollidersToLayer(false, "NoCol");

				if (obj != null)
				{
					item.transform.position = obj.transform.position;
					hand.RetrieveObject(obj);
				}

				//item.QBPoseOverride = item.PoseOverride;
				item.ForceObjectIntoInventorySlot(slot);
				item.SetAllCollidersToLayer(false, "Default");

				if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
				{
					Plugin.GetControllerFrom(hand).SetActive(false);
				}
			}
		}

		// If mag palm keybind matches grabbity keybind, suppress mag palm input if grabbity sphere is on an item
		private static bool GrabbityProtection(FVRViveHand hand, MagPalmControlsConfig.Keybind keybind)
		{
			if (_configs.MagPalm.Controls.GrabbityProtection.Value)
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
		private static bool AllowPalming(FVRPhysicalObject item)
		{
			var cheatCfg = _configs.zCheat;
			if (item != null)
			{
				if (item is FVRFireArmMagazine mag && mag.Size <= cheatCfg.SizeLimit.Value || item is FVRFireArmClip || (item is FVRFireArmRound && _configs.MagPalm.RoundPalm.Value) || cheatCfg.CursedPalms.Value && !item.m_isHardnessed)
				{
					return true;
				}
			}

			return false;
		}

		// CollisionPrevention patch
		[HarmonyPatch(typeof(FVRMovementManager), nameof(FVRMovementManager.FU))]
		[HarmonyPostfix]
		private static void FVRMovementManager_FU_Patch(FVRMovementManager __instance)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && _configs.MagPalm.CollisionPrevention.Value)
			{
				var vel = __instance.m_armSwingerVelocity + __instance.m_twoAxisVelocity;
				var speedlim = _configs.MagPalm.CollisionPreventionVelocity.Value;
				for (var i = 0; i < _handSlots.Length; i++)
				{
					var obj = _qbList[_handSlots[i]].CurObject;

					if (obj != null)
					{
						// Look for first nontrigger collider and use that for comparison
						foreach (var col in obj.m_colliders)
						{
							if (!col.isTrigger)
							{
								if (vel.magnitude >= speedlim && col.gameObject.layer != LayerMask.NameToLayer("NoCol"))
								{
									obj.SetAllCollidersToLayer(false, "NoCol");
									break;
								}
								else if (vel.magnitude < speedlim && col.gameObject.layer != LayerMask.NameToLayer("Default"))
								{
									obj.SetAllCollidersToLayer(false, "Default");
									break;
								}
							}
						}
					}
				}
			}
		}
		#endregion

		#region MagPalm Slot Checks

		// Is object in any magpalm slot
		private static bool ObjInPalmSlot(FVRQuickBeltSlot slot)
		{
			if (slot != null && (slot == _qbList[_handSlots[0]] || slot == _qbList[_handSlots[1]]))
			{
				return true;
			}

			return false;
		}

		// Is object in specific magpalm slot
		private static bool ObjInSpecificPalmSlot(FVRViveHand hand, FVRPhysicalObject obj)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && hand != null && obj != null)
			{
				var objQB = obj.QuickbeltSlot;

				var qbSlot = _qbList[_handSlots[0]];
				if (hand.IsThisTheRightHand)
				{
					qbSlot = _qbList[_handSlots[1]];
				}

				if (objQB != null && objQB == qbSlot)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsSpecificPalmSlotEmpty(FVRViveHand hand)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && hand != null)
			{
				var qbSlot = _qbList[_handSlots[0]];
				if (!hand.IsThisTheRightHand)
				{
					qbSlot = _qbList[_handSlots[1]];
				}

				if (qbSlot.CurObject == null)
				{
					return true;
				}
			}

			return false;
		}

		private static FVRViveHand GetHandFromSlot(FVRQuickBeltSlot slot)
		{
			var bod = GM.CurrentPlayerBody;
			var hand = bod.RightHand.GetComponent<FVRViveHand>();
			if (slot == _qbList[_handSlots[1]])
			{
				hand = bod.LeftHand.GetComponent<FVRViveHand>();
			}

			return hand;
		}

		// Generic checks to see if there is a gun in the relevant handslot
		private static FVRFireArm MirrorGunChecks(FVRFireArm gun)
		{
			var cfg = _configs.zCheat;
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && cfg.CursedPalms.Value && cfg.MirroredGuns.Value)
			{
				if (gun.m_hand != null)
				{
					var slot = _qbList[_handSlots[0]];
					if (!gun.m_hand.IsThisTheRightHand)
					{
						slot = _qbList[_handSlots[1]];
					}

					var obj = slot.CurObject;
					if (obj != null && obj is FVRFireArm fvrGun)
					{
						return fvrGun;
					}
				}
			}

			return null;
		}
		#endregion
	}
}
