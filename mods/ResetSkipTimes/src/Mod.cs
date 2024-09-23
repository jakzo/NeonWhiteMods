using System;
using HarmonyLib;

namespace Jakzo.NeonWhiteMods;

public static class BuildInfo
{
    public const string NAME = "ResetSkipTimes";
    public const string DESCRIPTION =
        "Resets times which achieved using skips not possible on the Steam version of the game.";
    public const string VERSION = "0.0.0";
}

// TODO: Save best skip and non-skip time somewhere so we can switch between them
public class Mod : NeonMod<Mod>
{
    public const string TTT_LEVEL_ID = "GRID_BOSS_GODSDEATHTEMPLE";
    public const long TTT_MIN_NON_SKIP_TIME = 40_000_000;
    public const long TTT_DEFAULT_NON_SKIP_TIME = 5 * 60_000_000;

    public long TttBestNonSkipTime = TTT_DEFAULT_NON_SKIP_TIME;

    [HarmonyPatch(typeof(GameDataManager), "OnReadPlayerSaveDataComplete")]
    internal static class GameDataManager_OnReadPlayerSaveDataComplete_Patch
    {
        [HarmonyPrefix]
        internal static void Prefix(PlayerSaveData data)
        {
            try
            {
                Dbg.Log("GameDataManager_OnReadPlayerSaveDataComplete_Patch");

                if (data?.levelStats == null)
                    return;

                var tttIndex = data.levelStats.keys.IndexOf(TTT_LEVEL_ID);
                if (tttIndex == -1)
                    return;

                var stats = data.levelStats.values[tttIndex];
                if (stats == null || stats._timeBestMicroseconds >= TTT_MIN_NON_SKIP_TIME)
                    return;

                stats._timeBestMicroseconds = Instance.TttBestNonSkipTime;
                Dbg.Log($"TTT best time set to {Instance.TttBestNonSkipTime}");
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error(
                    "GameDataManager_OnReadPlayerSaveDataComplete_Patch failed:"
                );
                Instance.LoggerInstance.Error(ex);
            }
        }
    }
}
