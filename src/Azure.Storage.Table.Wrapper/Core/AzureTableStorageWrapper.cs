using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure.Data.Tables;
using Azure.Storage.Table.Wrapper.Commands;
using Azure.Storage.Table.Wrapper.Queries;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;

namespace Azure.Storage.Table.Wrapper.Core;

[ExcludeFromCodeCoverage]
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
        ).MapFail(ex => Error.New(ErrorCodes.TableUnavailable, ErrorMessages.TableUnavailable, ex));

    public static Aff<CommandOperation> Upsert<T>(
        TableClient client,
        T data,
        CancellationToken token,
        bool createNew = true
    )
        where T : ITableEntity =>
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
            _ => CommandOperation.Success(),
            err => CommandOperation.Fail(Error.New(err.Code, err.Message, err.ToException()))
        );

    public static Aff<QueryOperation> GetAsync<T>(
        TableClient client,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            from op in AffMaybe<Response<T>>(
                async () =>
                    await client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: token)
            )
            from _1 in guardnot(
                op.GetRawResponse().IsError,
                Error.New(ErrorCodes.EntityDoesNotExist, ErrorMessages.EntityDoesNotExist)
            )
            select op
        ).Match(
            response => QueryOperation.Single(response.Value),
            err =>
                err.ToException() switch
                {
                    RequestFailedException rf
                        => rf.Status == (int)(HttpStatusCode.NotFound)
                            ? QueryOperation.Empty()
                            : QueryOperation.Fail(err),
                    _ => QueryOperation.Fail(err)
                }
        );
}
