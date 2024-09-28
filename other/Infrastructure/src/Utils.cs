using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Pulumi.Aws.DynamoDB;
using Pulumi.Aws.DynamoDB.Inputs;

public static class Utils
{
    public static TableArgs GenerateTableArgs<T>()
        where T : class
    {
        var type = typeof(T);
        var tableName = GetTableName(type);
        var hashKey = GetKeyPropertyName(type, typeof(DynamoDBHashKeyAttribute));
        var rangeKey = GetKeyPropertyName(type, typeof(DynamoDBRangeKeyAttribute));

        var attributes = GetDynamoDBAttributes(type);

        return new TableArgs
        {
            Name = tableName,
            BillingMode = "PAY_PER_REQUEST",
            HashKey = hashKey,
            RangeKey = rangeKey,
            Attributes = attributes.ToArray(),
        };
    }

    private static string GetTableName(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<DynamoDBTableAttribute>();
        return tableAttribute?.TableName
            ?? throw new InvalidOperationException("DynamoDBTable attribute not found");
    }

    private static string GetKeyPropertyName(Type type, Type keyAttributeType)
    {
        var keyProperty = type.GetProperties()
            .FirstOrDefault(prop => prop.GetCustomAttribute(keyAttributeType) != null);
        return keyProperty?.Name
            ?? throw new InvalidOperationException(
                $"No property with {keyAttributeType.Name} found"
            );
    }

    private static TableAttributeArgs[] GetDynamoDBAttributes(Type type)
    {
        return type.GetProperties()
            .SelectMany(GetDynamoDBAttribute)
            .Concat(type.GetFields().SelectMany(GetDynamoDBAttribute))
            .ToArray();
    }

    private static readonly Type[] DynamoDBAttributes =
    [
        typeof(DynamoDBHashKeyAttribute),
        typeof(DynamoDBRangeKeyAttribute),
        typeof(DynamoDBGlobalSecondaryIndexHashKeyAttribute),
        typeof(DynamoDBGlobalSecondaryIndexRangeKeyAttribute),
        typeof(DynamoDBLocalSecondaryIndexRangeKeyAttribute),
    ];

    private static IEnumerable<TableAttributeArgs> GetDynamoDBAttribute(PropertyInfo prop)
    {
        if (DynamoDBAttributes.Any(attr => prop.GetCustomAttribute(attr) != null))
            yield return new() { Name = prop.Name, Type = GetDynamoDBType(prop.PropertyType) };
    }

    private static IEnumerable<TableAttributeArgs> GetDynamoDBAttribute(FieldInfo prop)
    {
        if (DynamoDBAttributes.Any(attr => prop.GetCustomAttribute(attr) != null))
            yield return new() { Name = prop.Name, Type = GetDynamoDBType(prop.FieldType) };
    }

    private static string GetDynamoDBType(Type type)
    {
        if (type == typeof(string))
            return "S";
        if (type == typeof(int) || type == typeof(long))
            return "N";
        if (type == typeof(byte[]))
            return "B";
        throw new InvalidOperationException($"Unsupported property type: {type.Name}");
    }
}
