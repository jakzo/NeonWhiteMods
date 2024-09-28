using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;

namespace Jakzo.NeonWhiteMods.LeaderboardServer;

public static class Db
{
    public static readonly AmazonDynamoDBClient Client = new();
    public static readonly DynamoDBContext Context = new(Client);
}
