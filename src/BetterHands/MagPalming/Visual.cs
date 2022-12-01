using FistVR;
using UnityEngine;

namespace BetterHands.MagPalming
{
	public class MagPalmVisual
	{
		private readonly MagPalm MP;

		public MagPalmVisual(MagPalm mp)
		{
			MP = mp;
		}

		public void Hook()
		{
			// Pose Override
			On.FistVR.FVRPhysicalObject.GetGrabPos			+= FVRPhysicalObject_GetGrabPos;
			On.FistVR.FVRPhysicalObject.GetGrabRot			+= FVRPhysicalObject_GetGrabRot;

			// Controller Geo
			On.FistVR.FVRInteractiveObject.BeginInteraction += FVRInteractiveObject_BeginInteraction;
		}

		public void Unhook()
		{
			// Pose Override
			On.FistVR.FVRPhysicalObject.GetGrabPos			-= FVRPhysicalObject_GetGrabPos;
			On.FistVR.FVRPhysicalObject.GetGrabRot			-= FVRPhysicalObject_GetGrabRot;

			// Controller Geo
			On.FistVR.FVRInteractiveObject.BeginInteraction -= FVRInteractiveObject_BeginInteraction;
		}

		private Vector3 FVRPhysicalObject_GetGrabPos(On.FistVR.FVRPhysicalObject.orig_GetGrabPos orig, FVRPhysicalObject self)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && MP.ObjInPalmSlot(self.QuickbeltSlot))
			{
				return self.PoseOverride.position;
			}

			return orig(self);
		}

		private Quaternion FVRPhysicalObject_GetGrabRot(On.FistVR.FVRPhysicalObject.orig_GetGrabRot orig, FVRPhysicalObject self)
		{
			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && MP.ObjInPalmSlot(self.QuickbeltSlot))
			{
				return self.PoseOverride.rotation;
			}

			return orig(self);
		}

		// Return controller geo if palmed item is removed
		private void FVRInteractiveObject_BeginInteraction(On.FistVR.FVRInteractiveObject.orig_BeginInteraction orig, FVRInteractiveObject self, FVRViveHand hand)
		{
			orig(self, hand);

			if (GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled && GM.Options.QuickbeltOptions.HideControllerGeoWhenObjectHeld)
			{
				var otherHand = self.m_hand.OtherHand;
				if (otherHand != null && MP.IsSpecificPalmSlotEmpty(otherHand))
				{
					otherHand.UpdateControllerDefinition();
				}
			}
		}
	}
}