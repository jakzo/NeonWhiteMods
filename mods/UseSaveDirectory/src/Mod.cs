using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BitCode;
using BitCode.IO;
using BitCode.Platform.GameCore.IO;
using BitCode.Users;
using HarmonyLib;
using MelonLoader;
using TFBGames;
using UnityEngine;
using XGamingRuntime;

namespace Jakzo.NeonWhiteMods;

public static class BuildInfo
{
    public const string NAME = "UseSaveDirectory";
    public const string DESCRIPTION =
        "Saves ghost and game progress to the file system instead of to Xbox servers.";
    public const string VERSION = "1.0.0";
}

public class Mod : NeonMod<Mod>
{
    public string SavePath;
    public SimpleSaveDataManager FsSaveDataManager;
    public GameCoreSaveDataManager XboxSaveDataManager;
    public MelonPreferences_Entry<string> PrefSavePath;
    public MelonPreferences_Entry<bool> PrefCopySaveFilesFromXbox;

    private MethodInfo _getDestinationPath;

    public override void OnInitializeMod(MelonPreferences_Category prefsCategory)
    {
        _getDestinationPath = typeof(SimpleSaveDataManager).GetMethod(
            "GetDestinationPath",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (_getDestinationPath == null)
            throw new Exception("Could not find SimpleSaveDataManager.GetDestinationPath method");

        PrefSavePath = prefsCategory.CreateEntry(
            identifier: "save_path",
            display_name: "Save path",
            description: "Path to the directory where your game will be saved to.\n"
                + "Leave empty to use the same location as the Steam version of the game "
                + "(eg. C:\\Users\\<username>\\AppData\\LocalLow\\Little Flag Software, LLC\\Neon White).\n"
                + "Requires a restart after changing.",
            default_value: ""
        );

        PrefCopySaveFilesFromXbox = prefsCategory.CreateEntry(
            identifier: "download_save_files_from_xbox",
            display_name: "Download save files from Xbox",
            description: "If the save folder for your user does not exist (the one which is a long number) "
                + "then it will add the files from your Xbox save.",
            default_value: true
        );

        SavePath =
            PrefSavePath.Value?.Length > 0 ? PrefSavePath.Value : Application.persistentDataPath;
        FsSaveDataManager = new(new DotNetIO(), SavePath);

        Dbg.Log("Initialized");
    }

    public string GetUserSavePath(ILocalAccount userAccount) =>
        Path.Combine(SavePath, userAccount.GetUniqueIdForSaveData());

    [HarmonyPatch(typeof(PlatformServicesBuilderBase), "GetServices")]
    internal static class PlatformServicesBuilderBase_GetServices_Patch
    {
        [HarmonyPostfix]
        internal static void Postfix(ref IReadOnlyList<IPlatformService> __result)
        {
            Dbg.Log("PlatformServicesBuilderBase_GetServices_Patch");
            __result = __result
                .Select(service =>
                {
                    if (service is GameCoreSaveDataManager manager)
                    {
                        Dbg.Log("Replaced GameCoreSaveDataManager with SimpleSaveDataManager");
                        Instance.XboxSaveDataManager = manager;
                        return Instance.FsSaveDataManager;
                    }
                    return service;
                })
                .ToArray();
        }
    }

    public static XGameSaveContainerInfo[] ContainerInfos;

    [HarmonyPatch(typeof(FileManagement), "get_SaveMountPath")]
    internal static class FileManagement_SaveMountPath_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(out string __result)
        {
            Dbg.Log("FileManagement_SaveMountPath_Patch");

            try
            {
                var user = Singleton<Game>.Instance.Platform.GetCurrentUser();
                __result = Instance.GetUserSavePath(user) + Path.DirectorySeparatorChar;
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error(
                    "Failed to call SimpleSaveDataManager.GetDestinationPath:"
                );
                Instance.LoggerInstance.Error(ex);
                throw ex;
            }

            Dbg.Log("__result", __result);
            return false;
        }
    }
}
