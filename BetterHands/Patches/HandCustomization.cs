using BepInEx.Configuration;
using BetterHands.Configs;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace BetterHands.Patches
{
	internal class HandCustomizationPatches
	{
		private const string COLOR_PROPERTY = "_RimColor";

		private static RootConfig _configs => Plugin.Instance.Configs;

		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.DoInitialize))]
		[HarmonyPostfix]
		private static void FVRViveHand_DoInitialize_Patch(FVRViveHand __instance)
		{
			CustomizeHand(__instance);
		}

		private static void CustomizeHand(FVRViveHand fvrhand)
		{
			var hand = fvrhand.transform;

			// Set the idle sphere to our color
			var cfg = _configs.Color;
			fvrhand.TouchSphereMat_NoInteractable.SetColor(COLOR_PROPERTY, Recolor(cfg.InteractSphere, cfg.Intensity.Value));

			// Resize interaction spheres & colliders
			var scale = new float[] { _configs.FingerSize.Value, _configs.PalmSize.Value };
			SphereCollider[] collider = hand.GetComponents<SphereCollider>();
			Transform[] vis = new Transform[]
			{
				fvrhand.TouchSphere.transform,
				fvrhand.TouchSphere_Palm.transform
			};
			for (var i = 0; i < vis.Length; i++)
			{
				collider[i].radius = collider[i].radius * scale[i];

				var visScale = vis[i].localScale.x * scale[i];
				vis[i].localScale = new Vector3(visScale, visScale, visScale);
			}

			ColorHandRecursive(Plugin.GetControllerFrom(fvrhand));
		}

		// Color the hands various geo children
		private static void ColorHandRecursive(GameObject obj)
		{
			var cfg = _configs.Color;
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
		private static Vector4 Recolor(ConfigEntry<Vector4> cfg, float intensity)
		{
			var color = new Vector4(intensity * (cfg.Value[0] / 255), intensity * (cfg.Value[1] / 255), intensity * (cfg.Value[2] / 255), cfg.Value[3] / 1);
			return color;
		}
	}
}
