using BetterHands.Configs;
using FistVR;
using UnityEngine;

namespace BetterHands.MagPalming
{
	public class MagPalmInput
	{
		private readonly MagPalm MP;
		private readonly RootConfig _config;

		private bool _isArmSwingerTurning;

		public MagPalmInput(MagPalm mp)
		{
			MP = mp;
			_config = MP.Config;
		}

		public void Hook()
		{
			On.FistVR.FVRViveHand.Update += FVRViveHand_Update;
			On.FistVR.FVRPhysicalObject.FVRUpdate += FVRPhysicalObject_FVRUpdate;
			On.FistVR.FVRMovementManager.UpdateModeArmSwinger += FVRMovementManager_UpdateModeArmSwinger;
		}

		public void Unhook()
		{
			On.FistVR.FVRViveHand.Update -= FVRViveHand_Update;
			On.FistVR.FVRPhysicalObject.FVRUpdate -= FVRPhysicalObject_FVRUpdate;
			On.FistVR.FVRMovementManager.UpdateModeArmSwinger -= FVRMovementManager_UpdateModeArmSwinger;
		}

		#region Input Hooks

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
					// Get magpalm index
					var slot = self.transform == GM.CurrentPlayerBody.RightHand ? MP.QBList[MP.HandSlots[0]] : MP.QBList[MP.HandSlots[1]];

					var obj = slot.CurObject;
					var currItem = self.CurrentInteractable;

					// If current hand is empty, retrieve the object
					if (currItem == null && obj != null)
					{
						self.RetrieveObject(obj);

						// Make controller geo visible if slots & hands empty
						if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld && MP.IsSpecificPalmSlotEmpty(self))
						{
							self.UpdateControllerDefinition();
						}
					}

					// else if palming something & allowed, swap the current hand and hand slot items
					else if (currItem is FVRPhysicalObject item && AllowPalming(item))
					{
						item.ForceBreakInteraction();

						if (obj != null)
						{
							item.transform.position = obj.transform.position;
							self.RetrieveObject(obj);
						}

						item.ForceObjectIntoInventorySlot(slot);

						if (GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
						{
							Plugin.GetControllerFrom(self).SetActive(false);
						}
					}
				}
			}
		}

		// Disable palmed item collision if going too fast or rotating
		private void FVRPhysicalObject_FVRUpdate(On.FistVR.FVRPhysicalObject.orig_FVRUpdate orig, FVRPhysicalObject self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && _config.MagPalm.CollisionPrevention.Value && self.QuickbeltSlot != null)
			{
				if (MP.ObjInPalmSlot(self.QuickbeltSlot))
				{
					var moveMan = GM.CurrentMovementManager;
					var isMovingFast = (moveMan.m_isTwinStickSmoothTurningClockwise || moveMan.m_isTwinStickSmoothTurningCounterClockwise ||
						_isArmSwingerTurning ||
						(moveMan.m_armSwingerVelocity + moveMan.m_twoAxisVelocity).magnitude >= _config.MagPalm.CollisionPreventionVelocity.Value);

					var layer = isMovingFast ? "NoCol" : "Default";

					foreach (var col in self.m_colliders)
					{
						if (!col.isTrigger && col.gameObject.layer != LayerMask.NameToLayer(layer))
						{
							self.SetAllCollidersToLayer(false, layer);
							break;
						}
					}
				}
			}
		}

		// Check if smooth turning on ArmSwinger loco
		private void FVRMovementManager_UpdateModeArmSwinger(On.FistVR.FVRMovementManager.orig_UpdateModeArmSwinger orig, FVRMovementManager self)
		{
			orig(self);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && _config.MagPalm.CollisionPrevention.Value)
			{
				if (GM.Options.MovementOptions.ArmSwingerSnapTurnState == MovementOptions.ArmSwingerSnapTurnMode.Smooth)
				{
					foreach (var hand in self.Hands)
					{
						if (hand.CMode == ControlMode.Index || hand.CMode == ControlMode.WMR)
						{
							_isArmSwingerTurning = hand.Input.Secondary2AxisWestPressed || hand.Input.Secondary2AxisEastPressed;
							break;
						}
						else if (hand.IsInStreamlinedMode)
						{
							_isArmSwingerTurning = hand.Input.TouchpadWestDown || hand.Input.TouchpadEastDown;
							break;
						}
					}
				}
			}
		}

		#endregion Input Hooks

		// Returns true if the held object is valid for palming
		private bool AllowPalming(FVRPhysicalObject item)
		{
			var cheatCfg = _config.zCheat;

			return (item is FVRFireArmMagazine mag && mag.Size <= cheatCfg.SizeLimit.Value ||
				item is FVRFireArmClip ||
				item is FVRFireArmRound && _config.MagPalm.RoundPalm.Value ||
				cheatCfg.CursedPalms.Value && !item.m_isHardnessed);
		}

		// If mag palm keybind matches grabbity keybind, suppress mag palm input if grabbity sphere is on an item
		private bool GrabbityProtection(FVRViveHand hand, MagPalmControlsConfig.Keybind keybind)
		{
			if (_config.MagPalm.Controls.GrabbityProtection.Value)
			{
				var grabbityState = GM.Options.ControlOptions.WIPGrabbityButtonState;
				var grabbityConflict = (grabbityState == ControlOptions.WIPGrabbityButton.Trigger && (keybind == MagPalmControlsConfig.Keybind.Trigger)
										|| grabbityState == ControlOptions.WIPGrabbityButton.Grab && (keybind == MagPalmControlsConfig.Keybind.Grip));
				if (grabbityConflict)
				{
					return !hand.Grabbity_HoverSphere.gameObject.activeSelf;
				}
			}

			return true;
		}
	}
}