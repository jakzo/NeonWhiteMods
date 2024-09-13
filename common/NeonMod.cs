using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using MelonLoader;

[assembly: AssemblyTitle(Jakzo.NeonWhiteMods.BuildInfo.NAME)]
[assembly: AssemblyDescription(Jakzo.NeonWhiteMods.BuildInfo.DESCRIPTION)]
[assembly: AssemblyProduct(Jakzo.NeonWhiteMods.BuildInfo.NAME)]
[assembly: AssemblyVersion(Jakzo.NeonWhiteMods.BuildInfo.VERSION)]
[assembly: AssemblyFileVersion(Jakzo.NeonWhiteMods.BuildInfo.VERSION)]
[assembly: AssemblyCompany(Jakzo.NeonWhiteMods.Metadata.COMPANY)]
[assembly: AssemblyCopyright("Created by " + Jakzo.NeonWhiteMods.Metadata.AUTHOR)]
[assembly: AssemblyTrademark(Jakzo.NeonWhiteMods.Metadata.COMPANY)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en")]

[assembly: MelonGame(Jakzo.NeonWhiteMods.Metadata.DEVELOPER, Jakzo.NeonWhiteMods.Metadata.GAME)]
[assembly: MelonInfo(
    typeof(Jakzo.NeonWhiteMods.Mod),
    Jakzo.NeonWhiteMods.BuildInfo.NAME,
    Jakzo.NeonWhiteMods.BuildInfo.VERSION,
    Jakzo.NeonWhiteMods.Metadata.AUTHOR,
    Jakzo.NeonWhiteMods.BuildInfo.DOWNLOAD_URL
)]

namespace Jakzo.NeonWhiteMods;

public static class Metadata
{
    public const string AUTHOR = "jakzo";
    public const string COMPANY = null;
    public const string DEVELOPER = "Little Flag Software, LLC";
    public const string GAME = "Neon White";
}

public abstract class NeonMod<T> : MelonMod
    where T : class
{
    public static T Instance;

    public override void OnInitializeMelon()
    {
        Instance = this as T;
        var prefsCategory = MelonPreferences.CreateCategory(BuildInfo.NAME);
        Dbg.Init(prefsCategory, LoggerInstance);
        OnInitializeMod(prefsCategory);
    }

    public virtual void OnInitializeMod(MelonPreferences_Category prefsCategory) { }
}

public static class Dbg
{
    private static MelonLogger.Instance _logger;
    private static MelonPreferences_Entry<bool> _prefPrintDebugLogs;

    public static void Init(MelonPreferences_Category prefsCategory, MelonLogger.Instance logger)
    {
        _logger = logger;

        _prefPrintDebugLogs = prefsCategory.CreateEntry(
            identifier: "print_debug_logs",
            display_name: "Print debug logs",
            description: "Print debug logs to console",
            is_hidden: true,
            dont_save_default: true,
            default_value: false
        );
    }

    public static void Log(params object[] data)
    {
#if DEBUG
        PrintLog(data);
#else
        if (_prefPrintDebugLogs.Value)
            PrintLog(data);
#endif
    }

    private static void PrintLog(object[] data)
    {
        var msg = string.Join(" ", data.Select(d => d == null ? "" : d.ToString()));
        _logger.Msg($"dbg: {msg}");
    }
}
