using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Moq;
using static Azure.Storage.Table.Wrapper.TableOperation;

namespace Azure.Storage.Table.Wrapper.Tests;

public static class CommandTests
{
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

        var CommandServiceClient = new Mock<TableServiceClient>();
        CommandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(CommandServiceClient.Object);
        var CommandService = new CommandService(factory.Object);
        var op = await CommandService.UpsertAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var failedOp = op as CommandOperation.CommandFailedOperation;
        failedOp.Should().NotBeNull();
        failedOp!.Error.Code.Should().Be(ErrorCodes.CannotUpsert);
        failedOp.Error.Message.Should().Be("upsert failure");
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

        var CommandServiceClient = new Mock<TableServiceClient>();
        CommandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(CommandServiceClient.Object);
        var CommandService = new CommandService(factory.Object);
        var op = await CommandService.UpsertAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var succOp = op as TableOperation.CommandOperation;
        succOp.Should().NotBeNull();
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

        var CommandServiceClient = new Mock<TableServiceClient>();
        CommandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(CommandServiceClient.Object);
        var CommandService = new CommandService(factory.Object);
        var op = await CommandService.UpdateAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var succOp = op as TableOperation.CommandOperation;
        succOp.Should().NotBeNull();
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

        var CommandServiceClient = new Mock<TableServiceClient>();
        CommandServiceClient.Setup(x => x.GetTableClient("products")).Returns(tableClient.Object);

        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(CommandServiceClient.Object);
        var CommandService = new CommandService(factory.Object);
        var op = await CommandService.UpdateAsync(
            "test",
            "products",
            ProductDataModel.New("tech", "prod1", 100),
            new CancellationToken()
        );

        var failedOp = op as CommandOperation.CommandFailedOperation;
        failedOp.Should().NotBeNull();
        failedOp!.Error.Code.Should().Be(ErrorCodes.CannotUpdate);
        failedOp.Error.Message.Should().Be(ErrorMessages.CannotUpdate);
    }

    [Theory(DisplayName = "Invalid category or table")]
    [InlineData("", "")]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData(null, null)]
    public static async Task InvalidCategoryAndTable(string category, string table)
    {
        var CommandServiceClient = new Mock<TableServiceClient>();
        CommandServiceClient
            .Setup(x => x.GetTableClient("products"))
            .Throws(new Exception("table not found"));
        var factory = new Mock<IAzureClientFactory<TableServiceClient>>();
        factory.Setup(x => x.CreateClient("test")).Returns(CommandServiceClient.Object);

        var CommandService = new CommandService(factory.Object);
        var op = await CommandService.UpsertAsync(
            category,
            table,
            ProductDataModel.New("tech", "prod1", 100.5d),
            new CancellationToken()
        );
        var failedOp = op as CommandOperation.CommandFailedOperation;
        failedOp.Should().NotBeNull();
        failedOp!.Error.Code.Should().Be(ErrorCodes.Invalid);
        failedOp.Error.Message.Should().Be(ErrorMessages.EmptyOrNull);
    }
}
