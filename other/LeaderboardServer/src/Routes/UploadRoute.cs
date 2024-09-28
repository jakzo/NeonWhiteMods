using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Jakzo.NeonWhiteMods.LeaderboardServer.Routes;

public class UploadRoute : Route<UploadRoute.Input, UploadRoute.Output>
{
    public override string Path
    {
        get => "/upload";
    }

    public class Input
    {
        public required string LevelId;
        public required long TimeMicroseconds;

        [JsonConverter(typeof(PlatformConverter))]
        public required Platform Platform;
        public required string UserId;
    }

    public class Output
    {
        public required string Message;
    }

    public class PlatformConverter : JsonConverter<Platform>
    {
        public override Platform Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            return DbRecord.StringToPlatform(
                reader.GetString() ?? throw new JsonException("Platform cannot be null")
            );
        }

        public override void Write(
            Utf8JsonWriter writer,
            Platform value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(DbRecord.PlatformToString(value));
        }
    }

    public override async Task<Output> Handler(Input input, ILambdaContext context)
    {
        var submitTimestamp = DateTime.UtcNow;
        var record = new DbRecord()
        {
            LevelId = input.LevelId,
            TimeMicroseconds = input.TimeMicroseconds,
            FinishTimestamp = submitTimestamp,
            Platform = input.Platform,
            UserId = input.UserId,
        };
        context.Logger.LogLine($"Uploading with HK \"{record.HK}\" and RK \"{record.RK}\"");
        await Db.Context.SaveAsync(record);
        return new() { Message = "Upload successful" };
    }
}
