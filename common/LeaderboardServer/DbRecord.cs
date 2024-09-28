using System;
using System.Globalization;
using Amazon.DynamoDBv2.DataModel;

namespace Jakzo.NeonWhiteMods.LeaderboardServer;

public enum Platform
{
    STEAM,
    XBOX_PC,
}

[DynamoDBTable("Leaderboard")]
public class DbRecord
{
    public static Platform StringToPlatform(string platformName) =>
        platformName switch
        {
            "STEAM" => Platform.STEAM,
            "XBOX_PC" => Platform.XBOX_PC,
            _ => throw new ArgumentException("Invalid platform provided"),
        };

    public static string PlatformToString(Platform platform) =>
        platform switch
        {
            Platform.STEAM => "STEAM",
            Platform.XBOX_PC => "XBOX_PC",
            _ => throw new ArgumentException("Invalid platform provided"),
        };

    /// <summary>[Hash key] ID of the level in the game (eg. TUT_GHOST_1).</summary>
    [DynamoDBHashKey]
    public string HK
    {
        get => LevelId;
        set => LevelId = value;
    }

    /// <summary>
    /// [Range key]
    /// <c>#time#MICROSECOND_TIME#ts#TIMESTAMP_WHEN_SUBMITTED#platform#PLATFORM#user#USER_ID#</c>
    /// <list type="bullet">
    /// <item>MICROSECOND_TIME = Microsecond time to 10 digits (including leading zeroes)</item>
    /// <item>TIMESTAMP_WHEN_SUBMITTED = ISO 8601 timestamp string in UTC time (with Z)</item>
    /// <item>PLATFORM = Platform name (eg. STEAM, XBOX_PC)</item>
    /// <item>USER_ID = User ID (specific to platform)</item>
    /// </list>
    /// </summary>
    [DynamoDBRangeKey]
    public string RK
    {
        get
        {
            var time = TimeMicroseconds.ToString().PadLeft(10, '0');
            var ts = FinishTimestamp.ToString("o", CultureInfo.InvariantCulture);
            var platform = PlatformToString(Platform);
            var user = UserId;
            return $"#time#{time}#ts#{ts}#platform#{platform}#user#{user}#";
        }
        set
        {
            var parts = value.Split("#");
            if (parts.Length != 10)
                throw new ArgumentException("Contains invalid number of parts");
            TimeMicroseconds = long.Parse(parts[2]);
            FinishTimestamp = DateTime.Parse(parts[4], null, DateTimeStyles.RoundtripKind);
            Platform = StringToPlatform(parts[6]);
            UserId = parts[8];
        }
    }

    /// <summary>The data of the ghost file but zip compressed.</summary>
    [DynamoDBProperty]
    public byte[]? GhostCompressed;

    [DynamoDBIgnore]
    public required string LevelId;

    [DynamoDBIgnore]
    public required long TimeMicroseconds;

    [DynamoDBIgnore]
    public required DateTime FinishTimestamp;

    [DynamoDBIgnore]
    public required Platform Platform;

    [DynamoDBIgnore]
    public required string UserId;
}
