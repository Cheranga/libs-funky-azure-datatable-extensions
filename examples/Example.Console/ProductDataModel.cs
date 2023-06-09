﻿using Azure;
using Azure.Data.Tables;

namespace Example.Console;

public class ProductDataModel : ITableEntity
{
    public string Category { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public double Price { get; set; }
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public static ProductDataModel New(string category, string id, double price) =>
        new()
        {
            PartitionKey = category.ToUpper(),
            RowKey = id.ToUpper(),
            Category = category,
            Id = id,
            Price = price
        };
}
