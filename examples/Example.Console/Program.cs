using Azure.Storage.Table.Wrapper.Commands;
using Azure.Storage.Table.Wrapper.Core;
using Azure.Storage.Table.Wrapper.Queries;
using Example.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string category = "ProductsCategory";
const string table = "products";

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.RegisterTablesWithConnectionString(category, "UseDevelopmentStorage=true");
    })
    .Build();


var queryService = host.Services.GetRequiredService<IQueryService>();
var commandService = host.Services.GetRequiredService<ICommandService>();

await AddProductAsync();
await GetProductAsync();
await GetProductListAsync();
await UpdateProductAsync();

async Task GetProductListAsync()
{
    await Task.WhenAll(
        Enumerable
            .Range(1, 10)
            .Select(
                x =>
                    commandService.UpsertAsync(
                        category,
                        table,
                        ProductDataModel.New("TECH", x.ToString(), x),
                        new CancellationToken()
                    )
            )
    );

    var op = await queryService.GetEntityListAsync<ProductDataModel>(
        category,
        table,
        x => x.Category == "TECH",
        new CancellationToken()
    );

    Console.WriteLine(
        op.Response switch
        {
            QueryResult.CollectionResult<ProductDataModel> products
                => $"found {products.Entities.Count} items",
            QueryResult.QueryFailedResult f => $"{f.ErrorCode} with {f.ErrorMessage}",
            _ => "unsupported"
        }
    );
}

async Task GetProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var _ = await commandService.UpsertAsync(
        category,
        table,
        productDataModel,
        new CancellationToken()
    );

    var op = await queryService.GetEntityAsync<ProductDataModel>(
        category,
        table,
        "TECH",
        "PROD1",
        new CancellationToken()
    );
    Console.WriteLine(
        op.Response switch
        {
            QueryResult.SingleResult<ProductDataModel> r
                => $"{r.Entity.Category}:{r.Entity.Id}:{r.Entity.Price}",
            QueryResult.QueryFailedResult f => $"{f.ErrorCode}:{f.ErrorMessage}:{f.Exception}",
            _ => "unsupported"
        }
    );
}

async Task AddProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var op = await commandService.UpsertAsync(
        category,
        table,
        productDataModel,
        new CancellationToken()
    );

    Console.WriteLine(
        op.Operation switch
        {
            CommandOperation.CommandSuccessOperation _ => "successfully upserted",
            CommandOperation.CommandFailedOperation f
                => $"{f.ErrorCode}:{f.ErrorMessage}:{f.Exception}",
            _ => "unsupported"
        }
    );
}

async Task UpdateProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var insertOp = await commandService.UpsertAsync(
        category,
        table,
        productDataModel,
        new CancellationToken()
    );

    Console.WriteLine(
        insertOp.Operation switch
        {
            CommandOperation.CommandSuccessOperation _ => "successfully upserted",
            CommandOperation.CommandFailedOperation f
                => f.Exception is null ? throw new Exception("failed") : throw f.Exception,
            _ => throw new Exception("unsupported")
        }
    );

    var updateOp = await commandService.UpdateAsync(
        category,
        table,
        ProductDataModel.New("TECH", "PROD1", 100.50d),
        new CancellationToken()
    );

    Console.WriteLine(
        updateOp.Operation switch
        {
            CommandOperation.CommandSuccessOperation => "successfully updated",
            CommandOperation.CommandFailedOperation f
                => f.Exception is null ? throw new Exception("failed") : throw f.Exception,
            _ => throw new Exception("unsupported")
        }
    );
}
