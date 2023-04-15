using Azure.Data.Tables;
using Azure.Storage.Table.Wrapper.Commands;
using Azure.Storage.Table.Wrapper.Queries;
using Example.Console;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Queries = Azure.Storage.Table.Wrapper.Queries;

const string category = "ProductsCategory";
const string table = "products";

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        //services.RegisterTablesWithConnectionString(category, "UseDevelopmentStorage=true");

        services.AddAzureClients(
            builder => builder.AddTableServiceClient("UseDevelopmentStorage=true").WithName(category)
        );
    })
    .Build();

var factory = host.Services.GetRequiredService<IAzureClientFactory<TableServiceClient>>();

// var queryService = host.Services.GetRequiredService<IQueryService>();
// var commandService = host.Services.GetRequiredService<ICommandService>();

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
                    CommandExtensions.UpsertAsync(
                        factory,
                        category,
                        table,
                        ProductDataModel.New("TECH", x.ToString(), x),
                        new CancellationToken()
                    )
            )
    );

    var op = await Queries.QueryExtensions.GetEntityListAsync<ProductDataModel>(
        factory,
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
    var _ = await CommandExtensions.UpsertAsync(factory,category, table, productDataModel, new CancellationToken());

    var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
        factory,
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
    var op = await CommandExtensions.UpsertAsync(factory, category, table, productDataModel, new CancellationToken());

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
    var insertOp = await CommandExtensions.UpsertAsync(
        factory,
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

    var updateOp = await CommandExtensions.UpdateAsync(
        factory,
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
