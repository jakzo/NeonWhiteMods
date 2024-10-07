// In console:
// Jakzo.NeonWhiteMods.Mod.Instance.TttBestNonSkipTime = TimeSpan.FromSeconds(12.34).Ticks;

public static Dictionary<ulong, string> Cheaters = new Dictionary<ulong, string>();

static void Prefix(
    System.Collections.Generic.IReadOnlyList<BitCode.Platform.Leaderboards.Ranking> rankings
)
{
    // Hijacking this for a stable global editable reference
    var wr = TimeSpan.FromTicks(Jakzo.NeonWhiteMods.Mod.Instance.TttBestNonSkipTime);
    UnityExplorer.ExplorerCore.Log("WR = " + wr);
    try
    {
        foreach (var ranking in rankings)
        {
            var time = TimeSpan.FromMilliseconds(ranking.Score.Score);
            if (time < wr)
            {
                var user = ranking.User as BitCode.Platform.PlayFab.PlayFabRemoteAccount;
                var id = Convert.ToUInt64(user.PlayerId, 16);
                var name = user.Name.Value;
                UnityExplorer.ExplorerCore.Log(
                    "Adding cheater: [" + ranking.Rank + "] " + id + " " + name + " in " + time
                );
                Cheaters[id] = name;
            }
        }

        UnityExplorer.ExplorerCore.Log("All cheaters:");
        foreach (var entry in Cheaters)
        {
            UnityExplorer.ExplorerCore.Log(entry.key + ", // " + entry.value);
        }
    }
    catch (System.Exception ex)
    {
        UnityExplorer.ExplorerCore.LogWarning($"Exception in patch:\n{ex}");
    }
}
