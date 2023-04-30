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

public static class QueryTests
{
    [Fact(DisplayName = "Table does not exists")]
    public static async Task TableDoesNotExists()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Fail("table does not exist")
            );
        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient("test")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);
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
            QueryResult.QueryFailedResult err => new { err.ErrorCode, err.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = "error" }
        };

        response.ErrorCode.Should().Be(ErrorCodes.TableUnavailable);
        response.ErrorMessage.Should().Be(ErrorMessages.TableUnavailable);
    }

    [Fact(DisplayName = "Get an existing entity from table")]
    public static async Task GetExistingEntity()
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
            QueryResult.SingleResult<ProductDataModel> qf => qf.Entity,
            _ => null
        };
        response.Should().NotBeNull();
        response!.Category.Should().Be("TECH");
        response.Id.Should().Be("PROD1");
        response.Price.Should().Be(100);
    }

    [Fact(DisplayName = "Entity does not exist")]
    public static async Task EntityDoesNotExist()
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
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityAsync<ProductDataModel>(
            "test",
            "products",
            "TECH",
            "PROD1",
            new CancellationToken()
        );

        (
            op.Response switch
            {
                QueryResult.EmptyResult => "empty",
                _ => string.Empty
            }
        ).Should().Be("empty");
    }

    [Fact(DisplayName = "Filter returns a collection of entities")]
    public static async Task FilterReturnsEntities()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "TECH",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Returns(
                AsyncPageable<ProductDataModel>.FromPages(
                    new[]
                    {
                        Page<ProductDataModel>.FromValues(
                            new[]
                            {
                                ProductDataModel.New("TECH", "PROD1", 100),
                                ProductDataModel.New("TECH", "PROD2", 150)
                            },
                            It.IsAny<string>(),
                            It.IsAny<Response>()
                        )
                    }
                )
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
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityListAsync<ProductDataModel>(
            "test",
            "products",
            x => x.Category == "TECH",
            new CancellationToken()
        );

        (
            op.Response switch
            {
                QueryResult.CollectionResult<ProductDataModel> qf => qf.Entities,
                _ => new List<ProductDataModel>()
            }
        ).Count.Should().Be(2);
    }

    [Fact(DisplayName = "Filter returns a single entity")]
    public static async Task FilterReturnsSingleEntity()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "TECH",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Returns(
                AsyncPageable<ProductDataModel>.FromPages(
                    new[]
                    {
                        Page<ProductDataModel>.FromValues(
                            new[] { ProductDataModel.New("TECH", "PROD1", 100) },
                            It.IsAny<string>(),
                            It.IsAny<Response>()
                        )
                    }
                )
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
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityListAsync<ProductDataModel>(
            "test",
            "products",
            x => x.Category == "TECH",
            new CancellationToken()
        );

        var response = op.Response switch
        {
            QueryResult.SingleResult<ProductDataModel> qf => qf.Entity,
            _ => null
        };
        response.Should().NotBeNull();
        response!.Category.Should().Be("TECH");
        response.Id.Should().Be("PROD1");
        response.Price.Should().Be(100);
    }

    [Fact(DisplayName = "Filter returns empty")]
    public static async Task FilterReturnsEmptyResult()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "TECH",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Returns(
                AsyncPageable<ProductDataModel>.FromPages(
                    new[]
                    {
                        Page<ProductDataModel>.FromValues(
                            Array.Empty<ProductDataModel>(),
                            It.IsAny<string>(),
                            It.IsAny<Response>()
                        )
                    }
                )
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
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityListAsync<ProductDataModel>(
            "test",
            "products",
            x => x.Category == "TECH",
            new CancellationToken()
        );

        (
            op.Response switch
            {
                QueryResult.EmptyResult qf => "empty",
                _ => string.Empty
            }
        ).Should().Be("empty");
    }

    [Fact(DisplayName = "Filter throws error")]
    public static async Task FilterThrowsError()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "TECH",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Throws(new RequestFailedException(TestResponse.Fail("error occurred")));

        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityListAsync<ProductDataModel>(
            "test",
            "products",
            x => x.Category == "TECH",
            new CancellationToken()
        );

        var response = op.Response switch
        {
            QueryResult.QueryFailedResult qf => new { qf.ErrorCode, qf.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };
        response.ErrorCode.Should().Be(ErrorCodes.CannotGetDataFromTable);
        response.ErrorMessage.Should().Be(ErrorMessages.CannotGetDataFromTable);
    }

    [Fact(DisplayName = "Filter does not return entities")]
    public static async Task FilterDoesNotReturnEntities()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.QueryAsync<ProductDataModel>(
                        x => x.Category == "TECH",
                        It.IsAny<int?>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .Returns(
                AsyncPageable<ProductDataModel>.FromPages(
                    Enumerable.Empty<Page<ProductDataModel>>()
                )
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
        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityListAsync<ProductDataModel>(
            "test",
            "products",
            x => x.Category == "TECH",
            new CancellationToken()
        );

        (
            op.Response switch
            {
                QueryResult.EmptyResult => "empty",
                _ => string.Empty
            }
        ).Should().Be("empty");
    }

    [Theory(DisplayName = "Invalid category or table")]
    [InlineData("", "")]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData(null, null)]
    public static async Task InvalidCategoryAndTable(string category, string table)
    {
        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient
            .Setup(x => x.GetTableClient("products"))
            .Throws(new Exception("table not found"));
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(tableServiceClient.Object);

        var tableService = new QueryService(factory.Object);
        var op = await tableService.GetEntityAsync<ProductDataModel>(
            category,
            table,
            "tech",
            "prod1",
            new CancellationToken()
        );
        var response = op.Response switch
        {
            QueryResult.QueryFailedResult qf => new { qf.ErrorCode, qf.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };
        response.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }
}
