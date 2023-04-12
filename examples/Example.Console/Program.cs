using Azure.Storage.Table.Wrapper;
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

var queryService = host.Services.GetRequiredService<IQueryService>();
var commandService = host.Services.GetRequiredService<ICommandService>();

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
                commandService.UpsertAsync(
                    "ProductsDomain",
                    "products",
                    ProductDataModel.New("TECH", x.ToString(), x),
                    new CancellationToken()
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
            QueryOperation.QueryFailedOperation f => $"{f.Error.Code} with {f.Error.Message}",
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
                => $"{f.Error.Code}:{f.Error.Message}:{f.Error.ToException()}",
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
                => $"{f.Error.Code}:{f.Error.Message}:{f.Error.ToException()}",
            _ => "unsupported"
        }
    );
}
