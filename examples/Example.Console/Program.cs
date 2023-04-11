﻿using Azure.Storage.Table.Wrapper;
using Example.Console;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.RegisterTablesWithConnectionString("ProductsDomain", "UseDevelopmentStorage=true");
    })
    .Build();

var tableService = host.Services.GetRequiredService<ITableService>();

await AddProductAsync();
await GetProductAsync();
await GetProductListAsync();

async Task GetProductListAsync()
{
    var _ = await Enumerable
        .Range(1, 10)
        .ToSeq()
        .SequenceParallel(
            x =>
                tableService.UpsertAsync(
                    "ProductsDomain",
                    "products",
                    ProductDataModel.New("TECH", x.ToString(), x),
                    new CancellationToken()
                )
        );

    var op = await tableService.GetEntityListAsync<ProductDataModel>(
        "ProductsDomain",
        "products",
        x => x.Category == "TECH",
        new CancellationToken()
    );

    Console.WriteLine(
        op switch
        {
            TableOperation.QueryListOperation<ProductDataModel> products
                => $"found {products.Entities.Count} items",
            TableOperation.FailedOperation f => $"{f.Error.Code} with {f.Error.Message}",
            _ => "unsupported"
        }
    );
}

async Task GetProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var _ = await tableService.UpsertAsync(
        "ProductsDomain",
        "products",
        productDataModel,
        new CancellationToken()
    );

    var op = await tableService.GetEntityAsync<ProductDataModel>(
        "ProductsDomain",
        "products",
        "TECH",
        "PROD1",
        new CancellationToken()
    );
    Console.WriteLine(
        op switch
        {
            TableOperation.QuerySingleOperation<ProductDataModel> r
                => $"{r.Entity.Category}:{r.Entity.Id}:{r.Entity.Price}",
            TableOperation.FailedOperation f
                => $"{f.Error.Code}:{f.Error.Message}:{f.Error.ToException()}",
            _ => "unsupported"
        }
    );
}

async Task AddProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var op = await tableService.UpsertAsync(
        "ProductsDomain",
        "products",
        productDataModel,
        new CancellationToken()
    );

    Console.WriteLine(
        op switch
        {
            TableOperation.CommandOperation _ => "successfully upserted",
            TableOperation.FailedOperation f
                => $"{f.Error.Code}:{f.Error.Message}:{f.Error.ToException()}",
            _ => "unsupported"
        }
    );
}