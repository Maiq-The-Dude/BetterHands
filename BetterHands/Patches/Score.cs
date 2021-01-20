using HarmonyLib;
using RUST.Steamworks;
using Steamworks;
using System;

namespace BetterHands.Patches
{
	internal class ScorePatches
	{
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(SteamLeaderboard_t), typeof(int) })]
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(string), typeof(int), typeof(Action<int, int>) })]
		[HarmonyPrefix]
		private static bool SubmitTNHScore()
		{
			return false;
		}
	}
}
