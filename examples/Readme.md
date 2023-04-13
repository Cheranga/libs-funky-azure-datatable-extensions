## Example

Console application using the package.

- [x] Upsert product.
- [x] Get product.
- [x] Update product.

## Upsert product

```csharp
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
```

## Get product
```csharp
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
```

## Update product
```csharp
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
```