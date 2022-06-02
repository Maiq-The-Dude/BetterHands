using BepInEx.Logging;
using BetterHands.Configs;
using FistVR;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterHands.MagPalming
{
	public class MagPalm
	{
		public readonly RootConfig Config;

		private readonly ManualLogSource _loggers;

		private readonly MagPalmFunctionality _functionality;
		private readonly MagPalmVisual _visual;
		private readonly MagPalmInput _input;

		public readonly int[] HandSlots = new int[2];

		public List<FVRQuickBeltSlot> QBList;

		public MagPalm(RootConfig config, ManualLogSource logger)
		{
			Config = config;

			_loggers = logger;

			_functionality = new MagPalmFunctionality(this);
			_visual = new MagPalmVisual(this);
			_input = new MagPalmInput(this);
		}

		#region Hooks

		public void Hook()
		{
			_loggers.LogDebug("Mag palming is enabled");

			On.FistVR.FVRPlayerBody.ConfigureQuickbelt += FVRPlayerBody_ConfigureQuickbelt;

			_functionality.Hook();
			_visual.Hook();
			_input.Hook();
		}

		public void Unhook()
		{
			_loggers.LogDebug("Mag palming is disabled");

			On.FistVR.FVRPlayerBody.ConfigureQuickbelt -= FVRPlayerBody_ConfigureQuickbelt;

			_functionality.Unhook();
			_visual.Unhook();
			_input.Unhook();
		}

		#endregion Hooks

		#region MagPalm Creation

		// Add mag palm slots after configuring quickbelt
		private void FVRPlayerBody_ConfigureQuickbelt(On.FistVR.FVRPlayerBody.orig_ConfigureQuickbelt orig, FVRPlayerBody self, int index)
		{
			orig(self, index);

			if (!self.QuickbeltSlots.Any(slot => slot.name.Contains(self.LeftHand.name)))
			{
				ConfigMagPalming(self.RightHand);
				ConfigMagPalming(self.LeftHand);

				QBList = self.QuickbeltSlots;
			}
		}

		private void ConfigMagPalming(Transform hand)
		{
			var cfg = Config.MagPalm;

			// Backpack is a part of all quickbelt layouts, use that to create our slots
			var bod = GM.CurrentPlayerBody;
			var slot = bod.Torso.Find("QuickBeltSlot_Backpack");
			var qb = bod.QuickbeltSlots;

			if (slot != null)
			{
				var fvrhand = hand.GetComponent<FVRViveHand>();
				var pose = new GameObject().transform;
				pose.parent = fvrhand.PoseOverride;

				var palmIndex = 0;
				if (fvrhand.IsThisTheRightHand)
				{
					pose.localPosition = cfg.Position.Value;
					pose.localRotation = Quaternion.Euler(cfg.Rotation.Value);
				}
				else
				{
					// mirror configs for left hand
					palmIndex = 1;
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
				HandSlots[palmIndex] = qb.Count - 1;
			}
		}

		#endregion MagPalm Creation

		#region Helpers

		// Is object in any magpalm slot
		public bool ObjInPalmSlot(FVRQuickBeltSlot slot)
		{
			return slot != null && (slot == QBList[HandSlots[0]] || slot == QBList[HandSlots[1]]);
		}

		public bool IsSpecificPalmSlotEmpty(FVRViveHand hand)
		{
			var qbSlot = hand.IsThisTheRightHand ? QBList[HandSlots[0]] : QBList[HandSlots[1]];

			return qbSlot.CurObject == null;
		}

		public FVRViveHand GetHandFromSlot(FVRQuickBeltSlot slot)
		{
			var bod = GM.CurrentPlayerBody;

			return slot == QBList[HandSlots[0]] ? bod.RightHand.GetComponent<FVRViveHand>() : bod.LeftHand.GetComponent<FVRViveHand>();
		}

		#endregion Helpers
	}
}