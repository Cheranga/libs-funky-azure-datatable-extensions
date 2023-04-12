using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Moq;
using static Azure.Storage.Table.Wrapper.TableOperation;

namespace Azure.Storage.Table.Wrapper.Tests;

public static class InfrastructureTests
{
    [Fact(DisplayName = "Table service is unregistered")]
    public static async Task UnregisteredTableService()
    {
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityAsync<ProductDataModel>(
            "test",
            "products",
            "TECH",
            "PROD1",
            new CancellationToken()
        );

        var failedOp = op as QueryOperation.QueryFailedOperation;
        failedOp.Should().NotBeNull();
        failedOp!.Error.Code.Should().Be(ErrorCodes.UnregisteredTableService);
    }

    [Fact(DisplayName = "Table does not exist")]
    public static async Task TableDoesNotExist()
    {
        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient
            .Setup(x => x.GetTableClient("products"))
            .Throws(new Exception("table not found"));
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);

        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityAsync<ProductDataModel>(
            "test",
            "products",
            "tech",
            "prod1",
            new CancellationToken()
        );

        var failedOp = op as QueryOperation.QueryFailedOperation;
        failedOp.Should().NotBeNull();
        failedOp!.Error.Code.Should().Be(ErrorCodes.TableUnavailable);
    }
}
