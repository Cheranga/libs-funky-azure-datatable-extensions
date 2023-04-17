using Azure.Data.Tables;
using Funky.Azure.DataTable.Extensions.Core;
using Funky.Azure.DataTable.Extensions.Queries;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Moq;

namespace Funky.Azure.DataTable.Extensions.Tests;

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

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult qf => new { qf.ErrorCode, qf.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };
        response.ErrorCode.Should().Be(ErrorCodes.UnregisteredTableService);
        response.ErrorMessage.Should().Be(ErrorMessages.UnregisteredTableService);
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

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult qf => new { qf.ErrorCode, qf.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };
        response.ErrorCode.Should().Be(ErrorCodes.TableUnavailable);
        response.ErrorMessage.Should().Be(ErrorMessages.TableUnavailable);
    }
}
