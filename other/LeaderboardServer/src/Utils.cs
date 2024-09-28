using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Jakzo.NeonWhiteMods.LeaderboardServer;

public static class Utils
{
    public static readonly JsonSerializerOptions SerializerOptionsIncludeFields =
        new() { IncludeFields = true };

    public static APIGatewayHttpApiV2ProxyResponse Response<Body>(
        HttpStatusCode statusCode,
        Body body
    ) =>
        new()
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, SerializerOptionsIncludeFields),
            IsBase64Encoded = false,
        };

    public class ServerError
    {
        public required string Name;
        public required string Message;
    }
}
