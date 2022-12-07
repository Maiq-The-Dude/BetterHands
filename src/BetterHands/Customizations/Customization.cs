using BepInEx.Configuration;
using BepInEx.Logging;
using BetterHands.Configs;
using FistVR;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace BetterHands.Customizations
{
	public class Customization
	{
		private const string COLOR_PROPERTY = "_RimColor";

		private readonly RootConfig _config;

		private enum ColorOption
		{
			Hands,
			QBSlots
		}

		public Customization(RootConfig config)
		{
			_config = config;
			_config.Customization.QBSlots.aEnable.SettingChanged += QBSlot_SettingChanged;
		}

		#region Hooks

		public void HandsHook()
		{
			var cfg = _config.Customization.Hands;
			cfg.MaterialA.SettingChanged += Hands_SettingChanged;
			cfg.MaterialB.SettingChanged += Hands_SettingChanged;
			cfg.Intensity.SettingChanged += Hands_SettingChanged;
			cfg.InteractSphere.SettingChanged += Hands_SettingChanged;

			On.FistVR.FVRViveHand.DoInitialize += FVRViveHand_DoInitialize;
		}

		public void QBSlotsHook()
		{
			var cfg = _config.Customization.QBSlots;
			cfg.BaseColor.SettingChanged += QBSlot_SettingChanged;
			cfg.BaseIntensity.SettingChanged += QBSlot_SettingChanged;
			cfg.HarnessColor.SettingChanged += QBSlot_SettingChanged;
			cfg.HarnessIntensity.SettingChanged += QBSlot_SettingChanged;
			cfg.SpawnlockColor.SettingChanged += QBSlot_SettingChanged;
			cfg.SpawnlockIntensity.SettingChanged += QBSlot_SettingChanged;

			On.FistVR.FVRQuickBeltSlot.Awake += FVRQuickBeltSlot_Awake;
			IL.FistVR.FVRQuickBeltSlot.Update += FVRQuickBeltSlot_Update;
		}

		public void HandsUnhook()
		{
			var cfg = _config.Customization.Hands;
			cfg.MaterialA.SettingChanged -= Hands_SettingChanged;
			cfg.MaterialB.SettingChanged -= Hands_SettingChanged;
			cfg.Intensity.SettingChanged -= Hands_SettingChanged;
			cfg.InteractSphere.SettingChanged -= Hands_SettingChanged;

			On.FistVR.FVRViveHand.DoInitialize -= FVRViveHand_DoInitialize;
		}

		public void QBSlotsUnhook()
		{
			var cfg = _config.Customization.QBSlots;
			cfg.BaseColor.SettingChanged -= QBSlot_SettingChanged;
			cfg.BaseIntensity.SettingChanged -= QBSlot_SettingChanged;
			cfg.HarnessColor.SettingChanged -= QBSlot_SettingChanged;
			cfg.HarnessIntensity.SettingChanged -= QBSlot_SettingChanged;
			cfg.SpawnlockColor.SettingChanged -= QBSlot_SettingChanged;
			cfg.SpawnlockIntensity.SettingChanged -= QBSlot_SettingChanged;

			On.FistVR.FVRQuickBeltSlot.Awake -= FVRQuickBeltSlot_Awake;
			IL.FistVR.FVRQuickBeltSlot.Update -= FVRQuickBeltSlot_Update;
		}

		#endregion Hooks

		#region SettingsChanged

		private void Hands_SettingChanged(object sender, System.EventArgs e)
		{
			CustomizeHand(GM.CurrentPlayerBody.RightHand);
			CustomizeHand(GM.CurrentPlayerBody.LeftHand);
		}

		private void QBSlot_SettingChanged(object sender, System.EventArgs e)
		{
			foreach (var qbSlot in GM.CurrentPlayerBody.QBSlots_Internal)
			{
				RecolorChildrenRecursive(qbSlot.gameObject, ColorOption.QBSlots);
			}
			foreach (var qbSlot in GM.CurrentPlayerBody.QuickbeltSlots)
			{
				RecolorChildrenRecursive(qbSlot.gameObject, ColorOption.QBSlots);
			}
			foreach (var qbSlot in GM.CurrentPlayerBody.QBSlots_Added)
			{
				RecolorChildrenRecursive(qbSlot.gameObject, ColorOption.QBSlots);
			}
		}

		#endregion SettingsChanged

		#region Patches

		private void FVRViveHand_DoInitialize(On.FistVR.FVRViveHand.orig_DoInitialize orig, FVRViveHand self)
		{
			orig(self);

			CustomizeHand(self);
		}

		private void FVRQuickBeltSlot_Awake(On.FistVR.FVRQuickBeltSlot.orig_Awake orig, FVRQuickBeltSlot self)
		{
			orig(self);

			RecolorChildrenRecursive(self.gameObject, ColorOption.QBSlots);
		}

		private void FVRQuickBeltSlot_Update(MonoMod.Cil.ILContext il)
		{
			var cfg = _config.Customization.QBSlots;

			ILCursor c = new ILCursor(il);

			var counter = 0;
			while (DoesAnotherQBSlotSetColorExist(c) && counter <= 4)
			{
				Color colorCfg;
				float intensityCfg;

				c.RemoveRange(4);

				switch (counter)
				{
					// Spawnlock QB
					case 0:
						colorCfg = cfg.SpawnlockColor.Value;
						intensityCfg = cfg.SpawnlockIntensity.Value;
						break;

					// Harness QB
					case 1:
						colorCfg = cfg.HarnessColor.Value;
						intensityCfg = cfg.HarnessIntensity.Value;
						break;

					// Base QB
					default:
						colorCfg = cfg.BaseColor.Value;
						intensityCfg = cfg.BaseIntensity.Value;
						break;
				}

				// New RGBA values
				c.Emit(OpCodes.Ldc_R4, colorCfg.r * intensityCfg);
				c.Emit(OpCodes.Ldc_R4, colorCfg.g * intensityCfg);
				c.Emit(OpCodes.Ldc_R4, colorCfg.b * intensityCfg);
				c.Emit(OpCodes.Ldc_R4, colorCfg.a);

				counter++;
			}
		}

		#endregion Patches

		private void CustomizeHand(Transform hand)
		{
			CustomizeHand(hand.GetComponent<FVRViveHand>());
		}

		// Customizes hand spheres & colors
		private void CustomizeHand(FVRViveHand fvrhand)
		{
			// Set the idle sphere to our color
			var cfg = _config.Customization.Hands;
			fvrhand.TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, RecolorHDR(cfg.InteractSphere, cfg.Intensity.Value));

			// Resize interaction spheres & colliders
			var scale = new float[] { cfg.FingerSize.Value, cfg.PalmSize.Value };
			SphereCollider[] collider = fvrhand.transform.GetComponents<SphereCollider>();
			Transform[] vis = new Transform[]
			{
				fvrhand.TouchSphere.transform,
				fvrhand.TouchSphere_Palm.transform
			};
			for (var i = 0; i < vis.Length; i++)
			{
				collider[i].radius *= scale[i];

				if (cfg.Scale.Value)
				{
					var visScale = vis[i].localScale.x * scale[i];
					vis[i].localScale = new Vector3(visScale, visScale, visScale);
				}
			}

			RecolorChildrenRecursive(Plugin.GetControllerFrom(fvrhand), ColorOption.Hands);
		}

		// Recursively traverses either hand or qbslot children
		private void RecolorChildrenRecursive(GameObject obj, ColorOption opt)
		{
			int numOfChildren = obj.transform.childCount;
			for (int i = 0; i < numOfChildren; i++)
			{
				GameObject child = obj.transform.GetChild(i).gameObject;
				var rend = child.GetComponent<Renderer>();
				if (rend != null)
				{
					if (opt == ColorOption.Hands)
					{
						ColorHands(rend.material);
					}
					else
					{
						ColorQBSlot(rend.material);
					}
				}

				RecolorChildrenRecursive(obj.transform.GetChild(i).gameObject, opt);
			}
		}

		// Try to find next "_RimColor" string
		private bool DoesAnotherQBSlotSetColorExist(ILCursor c)
		{
			return c.TryGotoNext(
			  MoveType.After,
			  i => i.MatchLdstr(COLOR_PROPERTY)
			);
		}

		// Colors constant glow qbSlots
		private void ColorQBSlot(Material mat)
		{
			if (mat.name.IndexOf("constant", System.StringComparison.OrdinalIgnoreCase) != -1)
			{
				var cfg = _config.Customization.QBSlots;
				if (!cfg.aEnable.Value)
				{
					mat.SetColor(COLOR_PROPERTY, new Color(1f, 1f, 1f, 1f));
				}
				else
				{
					mat.SetColor(COLOR_PROPERTY, RecolorHDR(cfg.BaseColor, cfg.BaseIntensity.Value));
				}
			}
		}

		// Colors both hand materials
		private void ColorHands(Material mat)
		{
			var cfg = _config.Customization.Hands;
			if (mat.name.IndexOf("blue", System.StringComparison.OrdinalIgnoreCase) != -1)
			{
				mat.SetColor(COLOR_PROPERTY, RecolorHDR(cfg.MaterialA, cfg.Intensity.Value));
			}
			else
			{
				mat.SetColor(COLOR_PROPERTY, RecolorHDR(cfg.MaterialB, cfg.Intensity.Value));
			}
		}

		// Format the human readable RGBA to what unity hdr wants
		private Vector4 RecolorHDR(ConfigEntry<Color> cfg, float intensity)
		{
			var color = new Vector4(intensity * cfg.Value.r, intensity * cfg.Value.g, intensity * cfg.Value.b, cfg.Value.a);
			return color;
		}
	}
}