using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BitCode.Platform.GameCore;
using HarmonyLib;
using XGamingRuntime;

namespace Jakzo.NeonWhiteMods;

public static class XboxSave
{
    public const string SCID = "00000000-0000-0000-0000-0000795ccebe";

    public class GdkException(string name, int hr) : Exception($"{name} failed with code: {hr:X}")
    {
        public static void CheckResult(string name, int hr)
        {
            if (!HR.SUCCEEDED(hr))
                throw new GdkException(name, hr);
        }
    }

    // Deliberately only handling first account added (on startup) because the game itself doesn't
    // handle switching accounts mid-game
    [HarmonyPatch(typeof(GameCoreLocalAccountManager), "KkvpOinYAjRXRYquMikcoEHyXtOJ")]
    internal static class GameCoreLocalAccountManager_AddAccount_Patch
    {
        [HarmonyPrefix]
        internal static void Prefix(GameCoreLocalAccountManager __instance, out bool __state)
        {
            Dbg.Log("GameCoreLocalAccountManager_AddAccount_Patch");
            __state = Mod.Instance.PrefCopySaveFilesFromXbox.Value && __instance.Count == 0;
        }

        [HarmonyPostfix]
        internal static void Postfix(GameCoreLocalAccountManager __instance, bool __state)
        {
            if (!__state || __instance.Count != 1)
                return;

            Dbg.Log("First account added to GameCoreLocalAccountManager");
            TryCopyToFsIfNecessary();
        }
    }

    public static void TryCopyToFsIfNecessary()
    {
        try
        {
            var user = Singleton<Game>.Instance.Platform.GetCurrentUser() as GameCoreLocalAccount;
            Dbg.Log("user", user);

            var userSavePath = Mod.Instance.GetUserSavePath(user);
            Dbg.Log("userSavePath", userSavePath);

            if (Directory.Exists(userSavePath))
            {
                Dbg.Log("User save dir exists, skipping copy");
                return;
            }

            Mod.Instance.LoggerInstance.Msg(
                "Existing save directory not found, copying save from Xbox..."
            );

            // For some reason I can't set this thread as non-time sensitive so I can't do the sync
            // GDK calls but I don't want the game to load until the save is copied...
            Task.Run(() => CopyToFs(user, userSavePath)).Wait();
        }
        catch (Exception ex)
        {
            Mod.Instance.LoggerInstance.Error("Failed to copy save from Xbox:");
            Mod.Instance.LoggerInstance.Error(ex);
        }
    }

    public static void CopyToFs(GameCoreLocalAccount user, string userSavePath)
    {
        GdkException.CheckResult(
            "XGameSaveInitializeProvider",
            SDK.XGameSaveInitializeProvider(user.UserHandle, SCID, true, out var saveProviderHandle)
        );
        Dbg.Log("saveProviderHandle", saveProviderHandle);

        try
        {
            GdkException.CheckResult(
                "XGameSaveCreateContainer",
                SDK.XGameSaveCreateContainer(
                    saveProviderHandle,
                    "NeonWhite",
                    out var containerHandle
                )
            );
            Dbg.Log("containerHandle", containerHandle);

            GdkException.CheckResult(
                "XGameSaveEnumerateBlobInfo",
                SDK.XGameSaveEnumerateBlobInfo(containerHandle, out var blobInfos)
            );
            Dbg.Log("blobInfos", blobInfos.Length);

            GdkException.CheckResult(
                "XGameSaveReadBlobData",
                SDK.XGameSaveReadBlobData(containerHandle, blobInfos, out var blobs)
            );

            foreach (var blob in blobs)
            {
                Dbg.Log($"Saving: {blob.Info.Name} ({blob.Info.Size / 1024}kb)");
                var path = Path.Combine(userSavePath, ToSavePath(blob.Info.Name));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, blob.Data);
            }
        }
        finally
        {
            SDK.XGameSaveCloseProvider(saveProviderHandle);
            Dbg.Log("saveProviderHandle closed");
        }
    }

    public static string ToSavePath(string xboxBlobName)
    {
        var ghostFileMatch = Regex.Match(xboxBlobName, "^(Ghosts)_(.+)_([^_]+)$");
        if (ghostFileMatch.Success)
        {
            return Path.Combine(
                ghostFileMatch.Groups[1].Value,
                ghostFileMatch.Groups[2].Value,
                ghostFileMatch.Groups[3].Value
            );
        }

        return xboxBlobName;
    }
}
