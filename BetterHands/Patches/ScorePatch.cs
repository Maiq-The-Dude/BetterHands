using HarmonyLib;
using RUST.Steamworks;
using Steamworks;
using System;

namespace BetterHands
{
	internal class ScorePatch
	{
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(string), typeof(int), typeof(Action<int, int>) })]
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(SteamLeaderboard_t), typeof(int) })]
		[HarmonyPrefix]
		public static void HSM_UpdateScore()
		{
			return;
		}
	}
}
