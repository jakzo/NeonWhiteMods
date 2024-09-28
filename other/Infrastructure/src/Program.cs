using System;
using System.Collections.Generic;
using System.Text.Json;
using Jakzo.NeonWhiteMods.LeaderboardServer;
using Pulumi;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(() =>
{
    var configuration =
        Environment.GetEnvironmentVariable("CONFIGURATION")
        ?? throw new Exception("CONFIGURATION environment variable is not set");

    var leaderboardTable = new Aws.DynamoDB.Table(
        "LeaderboardTable",
        Utils.GenerateTableArgs<DbRecord>()
    );

    var leaderboardLambdaRole = new Aws.Iam.Role(
        "LeaderboardLambdaRole",
        new()
        {
            AssumeRolePolicy = JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["Version"] = "2012-10-17",
                    ["Statement"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["Action"] = "sts:AssumeRole",
                            ["Effect"] = "Allow",
                            ["Principal"] = new Dictionary<string, object?>
                            {
                                ["Service"] = "lambda.amazonaws.com",
                            },
                        },
                    },
                }
            ),
            ManagedPolicyArns =
            {
                "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
            },
            InlinePolicies = new Aws.Iam.Inputs.RoleInlinePolicyArgs[]
            {
                new()
                {
                    Name = "LeaderboardServerPolicy",
                    Policy = leaderboardTable.Arn.Apply(
                        (arn) =>
                            JsonSerializer.Serialize(
                                new Dictionary<string, object?>
                                {
                                    ["Version"] = "2012-10-17",
                                    ["Statement"] = new Dictionary<string, object?>[]
                                    {
                                        new()
                                        {
                                            ["Sid"] = "DbWrite",
                                            ["Effect"] = "Allow",
                                            ["Action"] = new[]
                                            {
                                                "dynamodb:PutItem",
                                                "dynamodb:UpdateItem",
                                                "dynamodb:DeleteItem",
                                                "dynamodb:BatchWriteItem",
                                                "dynamodb:GetItem",
                                                "dynamodb:BatchGetItem",
                                                "dynamodb:Query",
                                                "dynamodb:ConditionCheckItem",
                                            },
                                            ["Resource"] = new[] { arn, $"{arn}/index/*" },
                                        },
                                        new()
                                        {
                                            ["Sid"] = "ReadSecrets",
                                            ["Effect"] = "Allow",
                                            ["Action"] = "ssm:GetParameter",
                                            ["Resource"] = "arn:aws:ssm:*:*:parameter/*",
                                        },
                                    },
                                },
                                new JsonSerializerOptions { WriteIndented = true }
                            )
                    ),
                },
            },
        }
    );

    var leaderboardServer = new Aws.Lambda.Function(
        "LeaderboardServer",
        new()
        {
            Runtime = Aws.Lambda.Runtime.Dotnet8,
            Code = new FileArchive($"../LeaderboardServer/bin/{configuration}/net8.0"),
            Handler = "LeaderboardServer::Jakzo.NeonWhiteMods.LeaderboardServer.Lambda::Handler",
            Role = leaderboardLambdaRole.Arn,
        }
    );

    var leaderboardServerUrl = new Aws.Lambda.FunctionUrl(
        "LeaderboardServerUrl",
        new() { FunctionName = leaderboardServer.Name, AuthorizationType = "NONE" }
    );

    return new Dictionary<string, object?>
    {
        ["LeaderboardServer"] = new Dictionary<string, object?>
        {
            ["Name"] = leaderboardServer.Name,
            ["Url"] = leaderboardServerUrl.FunctionUrlResult,
            ["TableName"] = leaderboardTable.Name,
        },
    };
});
