using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FluentAssertions;
using Funky.Azure.DataTable.Extensions.Commands;
using Funky.Azure.DataTable.Extensions.Core;
using Microsoft.Extensions.Azure;
using Moq;

namespace Funky.Azure.DataTable.Extensions.Tests;

public static class CommandExtensionTests
{
    private const string Category = "productsdomain";
    private const string Table = "products";

    private static (int errorCode, string errorMessage) GetErrorResponse(
        CommandResponse<
            CommandOperation.CommandFailedOperation,
            CommandOperation.CommandSuccessOperation
        > op
    ) =>
        op.Operation switch
        {
            CommandOperation.CommandFailedOperation f => (f.ErrorCode, f.ErrorMessage),
            _ => (-1, string.Empty)
        };

    private static ProductDataModel GetValidProduct() => ProductDataModel.New("tech", "prod1", 100);

    private static Mock<TableServiceClient> GetTableServiceClient(Mock<TableClient> tableClient)
    {
        var tableServiceClient = new Mock<TableServiceClient>();
        tableServiceClient.Setup(x => x.GetTableClient(Table)).Returns(tableClient.Object);
        return tableServiceClient;
    }

    private static Mock<IAzureClientFactory<TableServiceClient>> GetFactory(
        Mock<TableServiceClient> tableServiceClient
    )
    {
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient(Category)).Returns(tableServiceClient.Object);
        return factory;
    }

    private static Mock<TableClient> GetTableClientWithUpsertException() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Replace,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .Throws(new RequestFailedException(TestResponse.Fail("error")))
        );

    private static Mock<TableClient> GetTableClientWithUpsertErrorResponse() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Replace,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .ReturnsAsync(TestResponse.Fail("error"))
        );

    private static Mock<TableClient> GetTableClientWithUpdateException() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Merge,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .Throws(new RequestFailedException(TestResponse.Fail("error")))
        );

    private static Mock<TableClient> GetTableClientWithUpsert() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Replace,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .ReturnsAsync(TestResponse.Success)
        );

    private static Mock<TableClient> GetTableClientWithUpdate() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Merge,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .ReturnsAsync(TestResponse.Success())
        );

    private static Mock<TableClient> GetTableClientWithUpdateErrorResponse() =>
        GetTableClientWith(
            x =>
                x.Setup(
                        _ =>
                            _.UpsertEntityAsync(
                                It.IsAny<ProductDataModel>(),
                                TableUpdateMode.Merge,
                                It.IsAny<CancellationToken>()
                            )
                    )
                    .ReturnsAsync(TestResponse.Fail("error"))
        );

    private static Mock<TableClient> GetTableClientWhenTableDoesNotExists() =>
        GetTableClientWith(
            x =>
                x.Setup(_ => _.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
                    .Throws(new RequestFailedException(500, "table does not exists"))
        );

    private static Mock<TableClient> GetTableClientWith(Action<Mock<TableClient>> mock)
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        mock(tableClient);
        return tableClient;
    }

    [Fact(DisplayName = "Table service client name is not provided")]
    public static async Task TableServiceClientNameIsNotProvided()
    {
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient(Category)).Throws(new Exception("unavailable"));

        var op = await CommandExtensions.UpsertAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.UnregisteredTableService);
        response.errorMessage.Should().Be(ErrorMessages.UnregisteredTableService);
    }

    [Fact(DisplayName = "Table unavailable")]
    public static async Task TableUnavailable()
    {
        var tableClient = GetTableClientWhenTableDoesNotExists();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpsertAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.TableUnavailable);
        response.errorMessage.Should().Be(ErrorMessages.TableUnavailable);
    }

    [Theory(DisplayName = "Invalid category")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidCategory(string category)
    {
        var op = await CommandExtensions.UpsertAsync(
            It.IsAny<IAzureClientFactory<TableServiceClient>>(),
            category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.Invalid);
        response.errorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Theory(DisplayName = "Invalid table name")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidTableName(string table)
    {
        var op = await CommandExtensions.UpsertAsync(
            It.IsAny<IAzureClientFactory<TableServiceClient>>(),
            "productsdomain",
            table,
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.Invalid);
        response.errorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Fact(DisplayName = "Cannot upsert due to exception")]
    public static async Task CannotUpsertDueToException()
    {
        var tableClient = GetTableClientWithUpsertException();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpsertAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);
        response.errorCode.Should().Be(ErrorCodes.CannotUpsert);
        response.errorMessage.Should().Be(ErrorMessages.CannotUpsert);
    }

    [Fact(DisplayName = "Cannot upsert due to error response from Azure")]
    public static async Task CannotUpsertDueToErrorResponseFromAzure()
    {
        var tableClient = GetTableClientWithUpsertErrorResponse();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpsertAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);
        response.errorCode.Should().Be(ErrorCodes.CannotUpsert);
        response.errorMessage.Should().Be(ErrorMessages.CannotUpsert);
    }

    [Fact(DisplayName = "Upsert successful")]
    public static async Task UpsertSuccessful()
    {
        var tableClient = GetTableClientWithUpsert();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpsertAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        (op.Operation is CommandOperation.CommandSuccessOperation).Should().BeTrue();
    }

    [Fact(DisplayName = "Updating item when Azure throws an error")]
    public static async Task CannotUpdateDueToAzureThrowsError()
    {
        var tableClient = GetTableClientWithUpdateException();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpdateAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);
        response.errorCode.Should().Be(ErrorCodes.CannotUpdate);
        response.errorMessage.Should().Be(ErrorMessages.CannotUpdate);
    }

    [Fact(DisplayName = "Updating item when Azure returns error response")]
    public static async Task CannotUpdateDueToErrorResponseFromAzure()
    {
        var tableClient = GetTableClientWithUpdateErrorResponse();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpdateAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);
        response.errorCode.Should().Be(ErrorCodes.CannotUpdate);
        response.errorMessage.Should().Be(ErrorMessages.CannotUpdate);
    }

    [Fact(DisplayName = "Update successful")]
    public static async Task UpdateSuccessful()
    {
        var tableClient = GetTableClientWithUpdate();
        var tableServiceClient = GetTableServiceClient(tableClient);
        var factory = GetFactory(tableServiceClient);

        var op = await CommandExtensions.UpdateAsync(
            factory.Object,
            Category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        (op.Operation is CommandOperation.CommandSuccessOperation).Should().BeTrue();
    }
    
    [Theory(DisplayName = "Invalid category when updating")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidCategoryWhenUpdating(string category)
    {
        var op = await CommandExtensions.UpdateAsync(
            It.IsAny<IAzureClientFactory<TableServiceClient>>(),
            category,
            Table,
            GetValidProduct(),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.Invalid);
        response.errorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }

    [Theory(DisplayName = "Invalid table name when updating")]
    [InlineData("")]
    [InlineData(null)]
    public static async Task InvalidTableNameWhenUpdating(string table)
    {
        var op = await CommandExtensions.UpdateAsync(
            It.IsAny<IAzureClientFactory<TableServiceClient>>(),
            "productsdomain",
            table,
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var response = GetErrorResponse(op);

        response.errorCode.Should().Be(ErrorCodes.Invalid);
        response.errorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }
}
