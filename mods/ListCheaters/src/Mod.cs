using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitCode.Platform.Leaderboards;
using BitCode.Platform.PlayFab;
using HarmonyLib;
using TFBGames;

namespace Jakzo.NeonWhiteMods;

public static class BuildInfo
{
    public const string NAME = "ListCheaters";
    public const string DESCRIPTION =
        "Lists all (very likely) cheaters on the leaderboards by comparing the times of each level against the world record for the level.";
    public const string VERSION = "0.0.0";
}

public class Mod : NeonMod<Mod>
{
    public async Task ListCheaters()
    {
        var cheaters = new Dictionary<ulong, string>();
        var game = Singleton<Game>.Instance;
        var platform = (PlatformBitCode)game.Platform;
        var leaderboardManager = (PlayFabLeaderboardManager)platform.Services.LeaderboardManager;
        var currentUser = platform.GetCurrentUser();

        foreach (var campaign in game.GetGameData().campaigns)
        {
            foreach (var mission in campaign.missionData)
            {
                foreach (var level in mission.levels)
                {
                    LoggerInstance.Msg(
                        $"Processing level: {level.levelID} ({level.levelDisplayName})"
                    );

                    if (!LEVEL_WRS.TryGetValue(level.levelID, out var wr))
                    {
                        LoggerInstance.Warning($"No WR found for level");
                        continue;
                    }

                    // Xbox-only strat
                    if (level.levelID == "GRID_BOSS_GODSDEATHTEMPLE")
                        wr = TimeSpan.FromSeconds(7.5);

                    var leaderboard = platform.FindLeaderboardByLevelData(level);

                    var hasReachedWrTime = false;
                    var offset = 0u;
                    var pageSize = 10u;
                    do
                    {
                        LoggerInstance.Msg($"Fetching page at offset {offset}");
                        var rankings = await leaderboardManager.GetRankingsAsync(
                            leaderboard,
                            currentUser,
                            offset,
                            pageSize,
                            LeaderboardSearchFilter.Global
                        );
                        offset += pageSize;

                        foreach (var ranking in rankings.Entries)
                        {
                            var time = TimeSpan.FromMilliseconds(ranking.Score.Score);
                            if (time >= wr)
                            {
                                hasReachedWrTime = true;
                                break;
                            }

                            var user = (PlayFabRemoteAccount)ranking.User;
                            var id = Convert.ToUInt64(user.PlayerId, 16);
                            if (cheaters.ContainsKey(id))
                                continue;

                            var name = user.Name.Value;
                            cheaters.Add(id, name);
                            LoggerInstance.Msg($"Adding cheater: {name}");
                        }

                        // Avoid triggering leaderboard rate limit
                        await Task.Delay(200);
                    } while (!hasReachedWrTime);
                }
            }
        }

        LoggerInstance.Msg("\n\n=== Cheaters ===\n");
        LoggerInstance.Msg(
            "\n\n" + string.Join("\n", cheaters.Select(entry => entry.Value.TrimEnd('#'))) + "\n"
        );
        LoggerInstance.Msg(
            "\n\n"
                + string.Join("\n", cheaters.Select(entry => $"{entry.Key}, // {entry.Value}"))
                + "\n"
        );
        LoggerInstance.Msg(
            "\n\n" + string.Join("\n", cheaters.Select(entry => $"{entry.Key},")) + "\n"
        );

        LoggerInstance.Msg($"List cheaters done, found {cheaters.Count} cheaters");
    }

    // Copy level IDs from: https://docs.google.com/spreadsheets/d/1bWbPA8JazoOM69zxHH4JkhUOuFgEfcqwLjvC3DhAty8/edit?gid=859441996#gid=859441996
    // Copy WR times from: https://docs.google.com/spreadsheets/d/1DtA0dbB70jK9gNCJ53VkaVEduz0ufl90AXWv3mlXRMo/edit?gid=693645076#gid=693645076
    public static string[] LEVEL_IDS = """
TUT_MOVEMENT
TUT_SHOOTINGRANGE
SLUGGER
TUT_FROG
TUT_JUMP
GRID_TUT_BALLOON
TUT_BOMB2
TUT_BOMBJUMP
TUT_FASTTRACK
GRID_PORT
GRID_PAGODA
TUT_RIFLE
TUT_RIFLEJOCK
TUT_DASHENEMY
GRID_JUMPDASH
GRID_SMACKDOWN
GRID_MEATY_BALLOONS
GRID_FAST_BALLOON
GRID_DRAGON2
GRID_DASHDANCE
TUT_GUARDIAN
TUT_UZI
TUT_JUMPER
TUT_BOMB
GRID_DESCEND
GRID_STAMPEROUT
GRID_CRUISE
GRID_SPRINT
GRID_MOUNTAIN
GRID_SUPERKINETIC
GRID_ARRIVAL
FLOATING
GRID_BOSS_YELLOW
GRID_HOPHOP
GRID_RINGER_TUTORIAL
GRID_RINGER_EXPLORATION
GRID_HOPSCOTCH
GRID_BOOM
GRID_SNAKE_IN_MY_BOOT
GRID_FLOCK
GRID_BOMBS_AHOY
GRID_ARCS
GRID_APARTMENT
TUT_TRIPWIRE
GRID_TANGLED
GRID_HUNT
GRID_CANNONS
GRID_FALLING
TUT_SHOCKER2
TUT_SHOCKER
GRID_PREPARE
GRID_TRIPMAZE
GRID_RACE
TUT_FORCEFIELD2
GRID_SHIELD
SA L VAGE2
GRID_VERTICAL
GRID_MINEFIELD
TUT_MIMIC
GRID_MIMICPOP
GRID_SWARM
GRID_SWITCH
GRID_TRAPS2
TUT_ROCKETJUMP
TUT_ZIPLINE
GRID_CLIMBANG
GRID_ROCKETUZI
GRID_CRASHLAND
GRID_ESCALATE
GRID_SPIDERCLAUS
GRID_FIRECRACKER_2
GRID_SPIDERMAN
GRID_DESTRUCTION
GRID_HEAT
GRID_BOLT
GRID_PON
GRID_CHARGE
GRID_MIMICFINALE
GRID_BARRAGE
GRID_1GUN
GRID_HECK
GRID_ANTFARM
GRID_FORTRESS
GRID_GODTEMPLE_ENTRY
GRID_BOSS_GODSDEATHTEMPLE
GRID_EXTERMINATOR
GRID_FEVER
GRID_SKIPSLIDE
GRID_CLOSER
GRID_HIKE
GRID_SKIP
GRID_CEILING
GRID_BOOP
GRID_TRIPRAP
GRID_ZIPRAP
TUT_ORIGIN
GRID_BOSS_RAPTURE
SIDEQUEST_OBSTACLE_PISTOL
SIDEQUEST_OBSTACLE_PISTOL_SHOOT
SIDEQUEST_OBSTACLE_MACHINEGUN
SIDEQUEST_OBSTACLE_RIFLE_2
SIDEQUEST_OBSTACLE_UZI2
SIDEQUEST_OBSTACLE_SHOTGUN
SIDEQUEST_OBSTACLE_ROCKETLAUNCHER
SIDEQUEST_RAPTURE_QUEST
SIDEQUEST_DODGER
GRID_GLASSPATH
GRID_GLASSPATH2
GRID_HELLVATOR
GRID_GLASSPATH3
SIDEQUEST_ALL_SEEING_EYE
SIDEQUEST_RESIDENTSAWB
SIDEQUEST_RESIDENTSAW
SIDEQUEST_SUNSET_FLIP_POWERBOMB
GRID_BALLOONLAIR
SIDEQUEST_BARREL_CLIMB
SIDEQUEST_FISHERMAN_SUPLEX
SIDEQUEST_STF
SIDEQUEST_ARENASIXNINE
SIDEQUEST_ATTITUDE_ADJUSTMENT
SIDEQUEST_ROCKETGODZ
""".Trim().Split('\n');

    public static string[] WR_TIMES = """
00:17.470
00:05.538
00:06.232
00:08.568
00:13.907
00:14.816
00:07.912
00:10.195
00:18.694
00:17.286
00:13.794
00:05.688
00:08.429
00:11.165
00:08.844
00:08.985
00:12.843
00:21.096
00:14.407
00:15.068
00:16.697
00:12.182
00:10.773
00:10.184
00:08.784
00:09.978
00:12.723
00:14.802
00:14.513
00:12.650
00:20.041
00:22.213
00:29.047
00:15.245
00:10.619
00:09.388
00:10.133
00:13.871
00:05.624
00:11.179
00:05.478
00:15.127
00:10.132
00:20.882
00:11.418
00:18.792
00:21.950
00:17.998
00:21.215
00:18.047
00:22.788
00:23.585
00:20.343
00:13.214
00:13.158
00:11.189
00:22.527
00:10.099
00:08.465
00:16.115
00:06.950
00:15.165
00:19.269
00:10.474
00:10.688
00:13.579
00:32.028
00:22.989
00:19.681
00:35.340
00:24.819
00:17.344
00:21.903
00:21.430
00:21.407
00:21.467
00:27.840
00:14.381
00:22.971
00:25.912
00:19.237
00:24.395
00:19.345
00:49.186
00:47.030
00:05.775
00:03.877
00:06.775
00:09.432
00:05.208
00:09.397
00:11.699
00:19.865
00:07.748
00:10.099
00:15.832
01:07.583
00:13.624
00:23.655
00:24.738
00:11.176
00:34.701
00:32.040
00:30.905
00:01.249
00:17.657
00:23.606
00:17.907
00:20.673
00:23.234
00:25.144
00:15.907
00:15.978
00:35.161
00:19.279
00:13.954
00:34.692
00:14.484
00:16.379
00:37.294
00:36.966
""".Trim().Split('\n');

    public static Dictionary<string, TimeSpan> LEVEL_WRS = Enumerable
        .Range(0, LEVEL_IDS.Length)
        .ToDictionary(
            i => LEVEL_IDS[i],
            i => TimeSpan.ParseExact(WR_TIMES[i], @"mm\:ss\.fff", null)
        );
}
