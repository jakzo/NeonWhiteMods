using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
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

    [HarmonyPatch]
    internal static class GhostRecorder_LoadLevelTotalTimeCompressedAsync_Patch
    {
        public const string GENERATED_CLASS_NAME = "<LoadLevelTotalTimeCompressedAsync>d__29";

        [HarmonyTargetMethod]
        internal static MethodInfo TargetMethod() =>
            typeof(GhostRecorder)
                .GetNestedType(GENERATED_CLASS_NAME, BindingFlags.NonPublic)
                .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        ) => TernaryUnderscoreRemover(GENERATED_CLASS_NAME, instructions);
    }

    [HarmonyPatch]
    internal static class GhostUtils_LoadLevelDataCompressedAsync_Patch
    {
        public const string GENERATED_CLASS_NAME = "<LoadLevelDataCompressedAsync>d__9";

        [HarmonyTargetMethod]
        internal static MethodInfo TargetMethod() =>
            typeof(GhostUtils)
                .GetNestedType(GENERATED_CLASS_NAME, BindingFlags.NonPublic)
                .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        ) => TernaryUnderscoreRemover(GENERATED_CLASS_NAME, instructions);
    }

    private static IEnumerable<CodeInstruction> TernaryUnderscoreRemover(
        string generatedClassName,
        IEnumerable<CodeInstruction> instructions
    )
    {
        // Changes this: ghostType == GhostType.PersonalGhost ? '_' : Path.DirectorySeparatorChar;
        // To this: ghostType == GhostType.PersonalGhost; Path.DirectorySeparatorChar;
        var signature = new OpCode[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldc_I4_1,
            OpCodes.Beq,
        };
        var i = 0;
        var replaced = false;
        foreach (var instruction in instructions)
        {
            if (!replaced)
            {
                if (instruction.opcode == signature[i])
                {
                    if (++i == signature.Length)
                    {
                        // Pop the values the beq would have but don't jump
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Pop);
                        replaced = true;
                        continue;
                    }
                }
                else
                {
                    i = 0;
                }
            }

            yield return instruction;
        }
        if (!replaced)
            MelonLogger.Warning($"Failed to initialize {generatedClassName} patch");
    }
}
