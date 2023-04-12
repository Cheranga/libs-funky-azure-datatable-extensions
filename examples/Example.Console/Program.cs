using Azure.Storage.Table.Wrapper.Commands;
using Azure.Storage.Table.Wrapper.Core;
using Azure.Storage.Table.Wrapper.Queries;
using Example.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.RegisterTablesWithConnectionString("ProductsDomain", "UseDevelopmentStorage=true");
    })
    .Build();

var queryService = host.Services.GetRequiredService<IQueryService>();
var commandService = host.Services.GetRequiredService<ICommandService>();

await AddProductAsync();
await GetProductAsync();
await GetProductListAsync();

async Task GetProductListAsync()
{
    await Task.WhenAll(
        Enumerable
            .Range(1, 10)
            .Select(
                x =>
                    commandService.UpsertAsync(
                        "ProductsDomain",
                        "products",
                        ProductDataModel.New("TECH", x.ToString(), x),
                        new CancellationToken()
                    )
            )
    );

    var op = await queryService.GetEntityListAsync<ProductDataModel>(
        "ProductsDomain",
        "products",
        x => x.Category == "TECH",
        new CancellationToken()
    );

    Console.WriteLine(
        op switch
        {
            QueryOperation.CollectionResult<ProductDataModel> products
                => $"found {products.Entities.Count} items",
            QueryOperation.QueryFailedOperation f => $"{f.ErrorCode} with {f.ErrorMessage}",
            _ => "unsupported"
        }
    );
}

async Task GetProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var _ = await commandService.UpsertAsync(
        "ProductsDomain",
        "products",
        productDataModel,
        new CancellationToken()
    );

    var op = await queryService.GetEntityAsync<ProductDataModel>(
        "ProductsDomain",
        "products",
        "TECH",
        "PROD1",
        new CancellationToken()
    );
    Console.WriteLine(
        op switch
        {
            QueryOperation.SingleResult<ProductDataModel> r
                => $"{r.Entity.Category}:{r.Entity.Id}:{r.Entity.Price}",
            QueryOperation.QueryFailedOperation f
                => $"{f.ErrorCode}:{f.ErrorMessage}:{f.Exception}",
            _ => "unsupported"
        }
    );
}

async Task AddProductAsync()
{
    var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
    var op = await commandService.UpsertAsync(
        "ProductsDomain",
        "products",
        productDataModel,
        new CancellationToken()
    );

    Console.WriteLine(
        op switch
        {
            CommandOperation.CommandSuccessOperation _ => "successfully upserted",
            CommandOperation.CommandFailedOperation f
                => $"{f.ErrorCode}:{f.ErrorMessage}:{f.Exception}",
            _ => "unsupported"
        }
    );
}
