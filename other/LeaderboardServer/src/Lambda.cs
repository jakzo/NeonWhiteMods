using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Jakzo.NeonWhiteMods.LeaderboardServer.Routes;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Jakzo.NeonWhiteMods.LeaderboardServer;

public class Lambda
{
    public static IRoute[] Routes = [new UploadRoute()];
    public static Dictionary<string, IRoute> RoutesByPath = Routes.ToDictionary(route =>
        route.Path
    );

    // https://docs.aws.amazon.com/lambda/latest/dg/urls-invocation.html
    public static async Task<APIGatewayHttpApiV2ProxyResponse> Handler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context
    )
    {
        if (!RoutesByPath.TryGetValue(request.RawPath, out var route))
        {
            return Utils.Response(
                HttpStatusCode.NotFound,
                new Utils.ServerError { Name = "NotFound", Message = "Desired path does not exist" }
            );
        }

        return await route.HandleRequest(request, context);
    }

    // TODO: Test cold start performance of native aot
    // private static async Task Main()
    // {
    //     Func<
    //         APIGatewayHttpApiV2ProxyRequest,
    //         ILambdaContext,
    //         APIGatewayHttpApiV2ProxyResponse
    //     > handler = Handler;
    //     await LambdaBootstrapBuilder
    //         .Create(
    //             handler,
    //             new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>()
    //         )
    //         .Build()
    //         .RunAsync();
    // }
}

// [JsonSerializable(typeof(string))]
// public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext { }
