using BepInEx.Configuration;
using BetterHands.Configs;
using FistVR;
using UnityEngine;

namespace BetterHands.Customization
{
	public class HandsRecolor
	{
		private const string COLOR_PROPERTY = "_RimColor";

		private readonly RootConfig _config;

		public HandsRecolor(RootConfig config)
		{
			_config = config;
		}

		public void Hook()
		{
			On.FistVR.FVRViveHand.DoInitialize += FVRViveHand_DoInitialize;
		}

		public void Unhook()
		{
			On.FistVR.FVRViveHand.DoInitialize -= FVRViveHand_DoInitialize;
		}

		private void FVRViveHand_DoInitialize(On.FistVR.FVRViveHand.orig_DoInitialize orig, FVRViveHand self)
		{
			orig(self);

			CustomizeHand(self);
		}

		private void CustomizeHand(FVRViveHand fvrhand)
		{
			var hand = fvrhand.transform;

			// Set the idle sphere to our color
			var cfg = _config.Customization;
			fvrhand.TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, Recolor(cfg.InteractSphere, cfg.Intensity.Value));

			// Resize interaction spheres & colliders
			var scale = new float[] { cfg.FingerSize.Value, cfg.PalmSize.Value };
			SphereCollider[] collider = hand.GetComponents<SphereCollider>();
			Transform[] vis = new Transform[]
			{
				fvrhand.TouchSphere.transform,
				fvrhand.TouchSphere_Palm.transform
			};
			for (var i = 0; i < vis.Length; i++)
			{
				collider[i].radius = collider[i].radius * scale[i];

				if (cfg.Scale.Value)
				{
					var visScale = vis[i].localScale.x * scale[i];
					vis[i].localScale = new Vector3(visScale, visScale, visScale);
				}
			}

			ColorHandRecursive(Plugin.GetControllerFrom(fvrhand));
		}

		// Color the hands various geo children
		private void ColorHandRecursive(GameObject obj)
		{
			var cfg = _config.Customization;
			var intensity = cfg.Intensity.Value;
			if (null == obj)
			{
				return;
			}

			foreach (Transform child in obj.transform)
			{
				if (child == null)
				{
					continue;
				}

				var rend = child.GetComponent<Renderer>();
				if (rend != null)
				{
					var mat = rend.material;

					// All controller geo have two materials, blue & purple
					if (mat.name.ToLower().Contains("blue"))
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.MaterialA, intensity));
					}
					else
					{
						mat.SetColor(COLOR_PROPERTY, Recolor(cfg.MaterialB, intensity));
					}
				}

				ColorHandRecursive(child.gameObject);
			}
		}

		// Format the human readable RGBA to what unity wants
		private Vector4 Recolor(ConfigEntry<Vector4> cfg, float intensity)
		{
			var color = new Vector4(intensity * (cfg.Value[0] / 255), intensity * (cfg.Value[1] / 255), intensity * (cfg.Value[2] / 255), cfg.Value[3] / 1);
			return color;
		}
	}
}