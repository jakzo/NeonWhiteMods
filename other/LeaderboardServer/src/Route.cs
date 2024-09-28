using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Jakzo.NeonWhiteMods.LeaderboardServer;

public interface IRoute
{
    public string Path { get; }
    public Task<APIGatewayHttpApiV2ProxyResponse> HandleRequest(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context
    );
}

public abstract class Route<Input, Output> : IRoute
{
    public abstract string Path { get; }
    public abstract Task<Output> Handler(Input input, ILambdaContext context);

    public Input ParseRequestBody(APIGatewayHttpApiV2ProxyRequest request) =>
        JsonSerializer.Deserialize<Input>(
            BodyAsString(request),
            Utils.SerializerOptionsIncludeFields
        ) ?? throw new JsonException("Request body cannot be null");

    public string BodyAsString(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (!request.IsBase64Encoded)
            return request.Body;

        var bytes = Convert.FromBase64String(request.Body);
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleRequest(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context
    )
    {
        try
        {
            var input = ParseRequestBody(request);

            try
            {
                var output = await Handler(input, context);
                return Utils.Response(HttpStatusCode.OK, output);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Exception while handling: {ex}");
                return Utils.Response(
                    HttpStatusCode.InternalServerError,
                    new Utils.ServerError
                    {
                        Name = "InternalServiceError",
                        Message = "An error occurred",
                    }
                );
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Exception while parsing input: {ex}");
            return Utils.Response(
                HttpStatusCode.BadRequest,
                new Utils.ServerError { Name = "BadRequest", Message = ex.Message }
            );
        }
    }
}
