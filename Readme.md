<!-- markdownlint-disable MD033 MD041 -->
<div align="center">

<img src="data-table.png" alt="TypedSpark.NET" width="150px"/>

# Funky Azure Data Table Extensions

Functions to easily interact with **Azure table storage** and **Cosmos Table API**. The library has been developed using C# with functional style programming.

</div>

> The library uses Microsoft's `Azure.Data.Tables` package to perform data operations.</p>
> The library separates commands and queries, and provides separate methods to be used easily on your data tables.

<!-- TOC -->
* [Funky Azure Data Table Extensions](#funky-azure-data-table-extensions)
  * [:tada: Features](#tada-features)
  * [:tada: Query Usages](#tada-query-usages)
  * [:tada: Command Usages](#tada-command-usages)
  * [Using the library in your solutions](#using-the-library-in-your-solutions)
  * [Differences between table storage, and cosmos table API](#differences-between-table-storage-and-cosmos-table-api)
  * [Attributions](#attributions)
<!-- TOC -->

## :tada: Features

:bulb: Separation of command and query operations.

:bulb: Query and command responses are strongly typed with clear intentions of what will be returned.

:bulb: Support for either managed identity or connection string through dependency injection.

:bulb: Use single named table service client for commands and queries (`ICommandService` and `IQueryService`).

:bulb: Use multiple named table service clients with `CommandExtensions` and `QueryExtensions`.

## :tada: Query Usages

:high_brightness: With partition and row key

Use the `GetEntityAsync` method in `IQueryService` for this.
```csharp
var op = await queryService.GetEntityAsync<ProductDataModel>(
    "ProductsDomain", // named service client
    "products", // table name // table name
    "TECH",
    "PROD1",
    new CancellationToken()
    );

// the operation returns the possible outputs, which you can pick and choose to operate on.
op.Response switch
{
    QueryResult.SingleResult<ProductDataModel> r => ,// do things
    QueryResult.EmptyResult e => ,// do things        
    QueryResult.QueryFailedResult f => ,// handle things,
    _ => ,// handle things more
};
```

:high_brightness: With LINQ expressions

Use the `GetEntityListAsync` in `IQueryService` for this.

```csharp
var op = await queryService.GetEntityListAsync<ProductDataModel>(
    "ProductsDomain", // named service client
    "products", // table name
    x => x.Category == "TECH", // filter to apply as LINQ expression
    new CancellationToken()
);

// the operation returns the possible outputs, which you can pick and choose to operate on.
op.Response switch
{
    QueryResult.CollectionResult<ProductDataModel> products => ,// do things,
    QueryResult.SingleResult<ProductDataModel> s => ,// do things,
    QueryResult.EmptyResult e => ,// do things,             
    QueryResult.QueryFailedResult f => ,// handle things
    _ => // handle things more
};
```

## :tada: Command Usages

:high_brightness: Upsert an entity

```csharp
var productDataModel = ProductDataModel.New("TECH", "PROD1", 259.99d);
var commandOp = await commandService.UpsertAsync(
    "ProductsDomain", // named service client
    "products", // table name
    productDataModel, // table entity to upsert
    new CancellationToken()
);

// the operation returns the possible outputs, which you can pick and choose to operate on.
commandOp.Operation switch
  {
      CommandOperation.CommandSuccessOperation _ => ,// do things
      CommandOperation.CommandFailedOperation f => ,// handle things
      _ => // handle things more
  }
```

:high_brightness: Update an entity

Use the `UpdateAsync` method in `ICommandService` for this.

```csharp
var updateOp = await commandService.UpdateAsync(
    category,
    table,
    ProductDataModel.New("TECH", "PROD1", 100.50d),
    new CancellationToken()
);

updateOp.Operation switch
{
    CommandOperation.CommandSuccessOperation => ,// do things
    CommandOperation.CommandFailedOperation f => , // handle things
    _ => // handle things more
};
```
## Using the library in your solutions

:high_brightness: Install package and configure

Install the `Funky.Azure.DataTable.Extensions` from nuget. </br>
Then register it as a dependency either using `RegisterTablesWithConnectionString` or `RegisterTablesWithManagedIdentity`. </p>

This will allow you to register the `TableServiceClient` as a named instance. </p>
In the below example `ProductsDomain` is the named client.

```csharp

// If you are using an ASP.NET application
builder.Services.RegisterTablesWithConnectionString(
            "ProductsDomain",
            "[Connection string for the storage account]"
        );

// This is from the sample console application in the examples section.
var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.RegisterTablesWithConnectionString(
         "ProductsDomain",
         "[Connection string for the storage account]"
        );
    })
    .Build();
    
```

:high_brightness: Inject the dependencies `IQueryService` or `ICommandService` or both to your class.
> Use this approach **ONLY** if you need only a single table service client for both queries and commands.

**Refer [:tada: Command Usages](#tada-command-usages) for commands, and [:tada: Query Usages](#tada-query-usages) for queries using this approach.**

:high_brightness: OR register your own named `IAzureClientFactory<TableServiceClient>` dependency and use `QueryExtensions` and `CommandExtensions`.

## Differences between table storage, and cosmos table API
There are some behaviour in Azure table storage, and in Azure Cosmos table API. Please refer the [official documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/table/table-api-faq#where-is-api-for-table-not-identical-with-azure-table-storage-behavior-) for this

## Attributions

[Icons created by juicy_fish](https://www.flaticon.com/free-icon/data-table_3575798)


