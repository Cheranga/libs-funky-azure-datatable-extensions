using Azure.Data.Tables;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;

namespace Azure.Storage.Table.Wrapper;

internal static class AzureTableStorageWrapper
{
    public static Eff<TableServiceClient> GetServiceClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category
    ) =>
        EffMaybe<TableServiceClient>(() => factory.CreateClient(category))
            .MapFail(
                ex =>
                    Error.New(
                        ErrorCodes.UnregisteredTableService,
                        ErrorMessages.UnregisteredTableService,
                        ex
                    )
            );

    public static Eff<TableClient> GetTableClient(TableServiceClient serviceClient, string table) =>
        (
            from tc in EffMaybe<TableClient>(() => serviceClient.GetTableClient(table))
            select tc
        ).MapFail(
            ex =>
                Error.New(
                    ErrorCodes.TableUnavailable,
                    ErrorMessages.TableUnavailable,
                    ex
                )
        );

    public static Aff<TableOperation> Upsert<T>(
        TableClient client,
        T data,
        CancellationToken token,
        bool createNew = true
    ) where T : ITableEntity =>
        (
            from op in AffMaybe<Response>(
                async () =>
                    await client.UpsertEntityAsync(
                        data,
                        createNew ? TableUpdateMode.Replace : TableUpdateMode.Merge,
                        token
                    )
            )
            from _ in guardnot(
                op.IsError,
                Error.New(ErrorCodes.CannotUpsert, ErrorMessages.CannotUpsert)
            )
            select op
        ).Match(
            _ => TableOperation.Success(),
            err =>
                TableOperation.Failure(
                    Error.New(err.Code, err.Message, err.ToException())
                )
        );

    public static Aff<TableOperation> GetAsync<T>(
        TableClient client,
        string partitionKey,
        string rowKey,
        CancellationToken token
    ) where T : class, ITableEntity =>
        (
            from op in AffMaybe<Response<T>>(
                async () =>
                    await client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: token)
            )
            from _ in guardnot(
                op.GetRawResponse().IsError,
                Error.New(ErrorCodes.EntityDoesNotExist, ErrorMessages.EntityDoesNotExist)
            )
            select op
        ).Match(
            response => TableOperation.GetEntity(response.Value),
            err =>
                TableOperation.Failure(
                    Error.New(err.Code, err.Message, err.ToException())
                )
        );
}
