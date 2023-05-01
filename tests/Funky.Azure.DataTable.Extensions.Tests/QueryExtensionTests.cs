using System.Net;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FluentAssertions;
using Funky.Azure.DataTable.Extensions.Core;
using Funky.Azure.DataTable.Extensions.Queries;
using Microsoft.Extensions.Azure;
using Moq;

namespace Funky.Azure.DataTable.Extensions.Tests;

public static class QueryExtensionTests
{
    [Fact(DisplayName = "Table service client name is not provided")]
    public static async Task TableServiceClientNameIsNotProvided()
    {
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Throws(new Exception("unavailable"));

        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            factory.Object,
            "productsdomain",
            "products",
            "category",
            "productid"
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.UnregisteredTableService);
        response.ErrorMessage.Should().Be(ErrorMessages.UnregisteredTableService);
    }

    [Fact(DisplayName = "Table unavailable")]
    public static async Task TableUnavailable()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException(500, "table does not exists"));

        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient
            .Setup(x => x.GetTableClient(It.IsAny<string>()))
            .Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(tableServiceClient.Object);

        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            factory.Object,
            "productsdomain",
            "products",
            "category",
            "productid"
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.TableUnavailable);
        response.ErrorMessage.Should().Be(ErrorMessages.TableUnavailable);
    }

    [Fact(DisplayName = "Record unavailable for provided partition and row key")]
    public static async Task RecordUnavailable()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.GetEntityAsync<ProductDataModel>(
                        "TECH",
                        "PROD1",
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Throws(new RequestFailedException((int)HttpStatusCode.NotFound, "entity not found"));
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);

        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            factory.Object,
            "test",
            "products",
            "TECH",
            "PROD1"
        );

        (op.Response is QueryResult.EmptyResult).Should().BeTrue();
    }

    [Theory(DisplayName = "Invalid category")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidCategory(string category)
    {
        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            Mock.Of<IAzureClientFactory<TableServiceClient>>(),
            category,
            "products",
            "tech",
            "prod1"
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Theory(DisplayName = "Invalid table name")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidTableName(string table)
    {
        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            Mock.Of<IAzureClientFactory<TableServiceClient>>(),
            "productsdomain",
            table,
            "tech",
            "prod1"
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Theory(DisplayName = "Invalid partition key")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidPartitionKey(string partitionKey)
    {
        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            Mock.Of<IAzureClientFactory<TableServiceClient>>(),
            "productsdomain",
            "products",
            partitionKey,
            "prod1"
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Theory(DisplayName = "Invalid row key")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidRowKey(string rowKey)
    {
        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            Mock.Of<IAzureClientFactory<TableServiceClient>>(),
            "productsdomain",
            "products",
            "tech",
            rowKey
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Fact(DisplayName = "Record available for provided partition and row key")]
    public static async Task RecordAvailable()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.GetEntityAsync<ProductDataModel>(
                        "TECH",
                        "PROD1",
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(
                TestResponse<ProductDataModel>.Success(ProductDataModel.New("TECH", "PROD1", 100))
            );
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);

        var op = await Queries.QueryExtensions.GetEntityAsync<ProductDataModel>(
            factory.Object,
            "test",
            "products",
            "TECH",
            "PROD1"
        );

        var product = op.Response switch
        {
            QueryResult.SingleResult<ProductDataModel> p => p.Entity,
            _ => ProductDataModel.New("", "", -1)
        };

        product.Category.Should().Be("TECH");
        product.Id.Should().Be("PROD1");
        product.Price.Should().Be(100);
    }

    [Fact(DisplayName = "Records are available for provided filter")]
    public static async Task RecordAvailableForFilter()
    {
        var tableClient = new Mock<TableClient>();

        var products = Enumerable
            .Range(1, 5)
            .Select(x => ProductDataModel.New("tech", $"prod{x}", 100))
            .ToList()
            .AsReadOnly();
        var pages = Page<ProductDataModel>.FromValues(
            products,
            string.Empty,
            TestResponse.Success()
        );
        var func = AsyncPageable<ProductDataModel>.FromPages(new[] { pages });

        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "tech",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Returns(func);
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);

        var op = await Queries.QueryExtensions.GetEntityListAsync<ProductDataModel>(
            factory.Object,
            "test",
            "products",
            x => x.Category == "tech",
            new CancellationToken()
        );

        var productList = op.Response switch
        {
            QueryResult.CollectionResult<ProductDataModel> p => p.Entities,
            _ => new List<ProductDataModel>()
        };
        productList.Count.Should().Be(5);
    }
}
