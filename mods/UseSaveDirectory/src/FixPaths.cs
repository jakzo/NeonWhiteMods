using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TFBGames;

namespace Jakzo.NeonWhiteMods;

// All methods below seem to have been modified in the Xbox version to replace slashes with
// underscores (so it just saves flat files, no directories) and remove spaces so reverting
// those changes with these patches

// TODO: There may be more of these I haven't found...

public static class FixPaths
{
    [HarmonyPatch(typeof(GhostRecorder), "GetCompressedSavePath")]
    internal static class GhostRecorder_GetCompressedSavePath_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(out string __result, string forcedPath, ulong forcedID)
        {
            try
            {
                var filePath = "";
                GhostUtils.GetPath(GhostUtils.GhostType.PersonalGhost, ref filePath);
                __result = Path.Combine(
                    forcedPath != "" ? forcedPath : filePath,
                    $"{forcedID}.phant"
                );
            }
            catch (Exception ex)
            {
                Mod.Instance.LoggerInstance.Error(
                    "GhostRecorder_GetCompressedSavePath_Patch failed:"
                );
                Mod.Instance.LoggerInstance.Error(ex);
                __result = null;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(GhostRecorder), nameof(GhostRecorder.GetCompressedSavePathForLevel))]
    internal static class GhostRecorder_GetCompressedSavePathForLevel_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(out string __result, LevelData level)
        {
            try
            {
                var filePath = "";
                GhostUtils.GetPath(level.levelID, GhostUtils.GhostType.PersonalGhost, ref filePath);
                var id = 0UL;
                __result = Path.Combine(filePath, $"{id}.phant");
            }
            catch (Exception ex)
            {
                Mod.Instance.LoggerInstance.Error(
                    "GhostRecorder_GetCompressedSavePathForLevel_Patch failed:"
                );
                Mod.Instance.LoggerInstance.Error(ex);
                __result = null;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(GhostUtils))]
    internal static class GhostUtils_GetPath_Patch
    {
        internal static MethodBase TargetMethod() =>
            typeof(GhostUtils).GetMethod(
                nameof(GhostUtils.GetPath),
                [typeof(string), typeof(GhostUtils.GhostType), typeof(string).MakeByRefType()]
            );

        [HarmonyPrefix]
        internal static bool Prefix(
            ref bool __result,
            string levelName,
            GhostUtils.GhostType ghostType,
            ref string filePath
        )
        {
            try
            {
                if (ghostType != GhostUtils.GhostType.PersonalGhost)
                    return true;

                filePath = FileManagement.SaveMountPath + Path.Combine("Ghosts", levelName);
                __result = true;
            }
            catch (Exception ex)
            {
                Mod.Instance.LoggerInstance.Error("GhostUtils_GetPath_Patch failed:");
                Mod.Instance.LoggerInstance.Error(ex);
                __result = false;
            }
            return false;
        }
    }
}
