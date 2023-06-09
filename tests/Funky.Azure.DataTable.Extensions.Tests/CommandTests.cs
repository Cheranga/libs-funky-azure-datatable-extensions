﻿using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Funky.Azure.DataTable.Extensions.Commands;
using Funky.Azure.DataTable.Extensions.Core;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Moq;

namespace Funky.Azure.DataTable.Extensions.Tests;

public static class CommandTests
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
        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpsertAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var response = op.Operation switch
        {
            CommandOperation.CommandFailedOperation err => new { err.ErrorCode, err.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = "error" }
        };

        response.ErrorCode.Should().Be(ErrorCodes.TableUnavailable);
        response.ErrorMessage.Should().Be(ErrorMessages.TableUnavailable);
    }

    [Fact(DisplayName = "Upsert is unsuccessful")]
    public static async Task UpsertFails()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.UpsertEntityAsync(
                        It.IsAny<ProductDataModel>(),
                        It.IsAny<TableUpdateMode>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(TestResponse.Fail("upsert failure"));
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var commandServiceClient = new Mock<TableServiceClient>();
        commandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(commandServiceClient.Object);
        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpsertAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var response = op.Operation switch
        {
            CommandOperation.CommandFailedOperation f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };
        response.ErrorCode.Should().Be(ErrorCodes.CannotUpsert);
        response.ErrorMessage.Should().Be(ErrorMessages.CannotUpsert);
    }

    [Fact(DisplayName = "Upsert is successful")]
    public static async Task UpsertSuccessful()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.UpsertEntityAsync(
                        ProductDataModel.New("tech", "prod1", 100),
                        It.IsAny<TableUpdateMode>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(TestResponse.Success());
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var commandServiceClient = new Mock<TableServiceClient>();
        commandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(commandServiceClient.Object);
        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpsertAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        (
            op.Operation switch
            {
                CommandOperation.CommandSuccessOperation => "success",
                _ => string.Empty
            }
        ).Should().Be("success");
    }

    [Fact(DisplayName = "Update entity when entity exists")]
    public static async Task UpdateEntity()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.UpsertEntityAsync(
                        ProductDataModel.New("tech", "prod1", 100),
                        TableUpdateMode.Merge,
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(TestResponse.Success());
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var commandServiceClient = new Mock<TableServiceClient>();
        commandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(commandServiceClient.Object);
        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpdateAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        (
            op.Operation switch
            {
                CommandOperation.CommandSuccessOperation => "success",
                _ => string.Empty
            }
        ).Should().Be("success");
    }

    [Fact(DisplayName = "Update entity when entity does not exists")]
    public static async Task UpdateEntityWhenEntityDoesNotExists()
    {
        var tableClient = new Mock<TableClient>();
        tableClient
            .Setup(
                x =>
                    x.UpsertEntityAsync(
                        ProductDataModel.New("tech", "prod1", 100),
                        TableUpdateMode.Merge,
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(TestResponse.Fail("entity not found"));
        tableClient
            .Setup(x => x.GetAccessPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                TestResponse<IReadOnlyList<TableSignedIdentifier>>.Success(
                    new[] { It.IsAny<TableSignedIdentifier>() }
                )
            );

        var commandServiceClient = new Mock<TableServiceClient>();
        commandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(commandServiceClient.Object);
        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpdateAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var response = op.Operation switch
        {
            CommandOperation.CommandFailedOperation f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response!.ErrorCode.Should().Be(ErrorCodes.CannotUpdate);
        response.ErrorMessage.Should().Be(ErrorMessages.CannotUpdate);
    }

    [Theory(DisplayName = "Invalid category or table")]
    [InlineData("", "")]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData(null, null)]
    public static async Task InvalidCategoryAndTable(string category, string table)
    {
        var commandServiceClient = new Mock<TableServiceClient>();
        commandServiceClient
            .Setup(x => x.GetTableClient("products"))
            .Throws(new Exception("table not found"));
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(commandServiceClient.Object);

        var commandService = new CommandService(factory.Object);
        var op = await commandService.UpsertAsync(
            category,
            table,
            ProductDataModel.New("tech", "prod1", 100.5d),
            new CancellationToken()
        );
        var response = op.Operation switch
        {
            CommandOperation.CommandFailedOperation f => new { f.ErrorCode, f.ErrorMessage },
            _ => new { ErrorCode = -1, ErrorMessage = string.Empty }
        };

        response!.ErrorCode.Should().Be(ErrorCodes.Invalid);
        response.ErrorMessage.Should().Be(ErrorMessages.EmptyOrNull);
    }
}
