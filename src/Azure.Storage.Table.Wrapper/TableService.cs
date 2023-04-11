using System.Linq.Expressions;
using Azure.Data.Tables;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;
using static Azure.Storage.Table.Wrapper.AzureTableStorageWrapper;

namespace Azure.Storage.Table.Wrapper;

internal class TableService : ITableService
{
    private readonly IAzureClientFactory<TableServiceClient> _factory;

    public TableService(IAzureClientFactory<TableServiceClient> factory) => _factory = factory;

    public async Task<TableOperation> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from tc in TableClient(_factory, category, table)
                from op in AffMaybe<Response>(
                    async () =>
                        await tc.UpsertEntityAsync(
                            data,
                            mode: TableUpdateMode.Replace,
                            cancellationToken: token
                        )
                )
                from _3 in guardnot(op.IsError, Error.New(ErrorCodes.CannotUpsert, op.ReasonPhrase))
                select op
            ).Run()
        ).Match(
            _ => TableOperation.Success(),
            err => TableOperation.Failure(Error.New(err.Code, err.Message, err.ToException()))
        );

    public async Task<TableOperation> GetEntityAsync<T>(
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from _3 in ValidateEmptyString(
                    partitionKey,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _4 in ValidateEmptyString(
                    rowKey,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from tc in TableClient(_factory, category, table)
                from op in GetAsync<T>(tc, partitionKey, rowKey, token)
                select op
            ).Run()
        ).Match(
            operation => operation,
            err => TableOperation.Failure(Error.New(err.Code, err.Message, err.ToException()))
        );

    public async Task<TableOperation> GetEntityListAsync<T>(
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from tc in TableClient(_factory, category, table)
                from records in Aff(
                    async () =>
                        await tc.QueryAsync<T>(filter, cancellationToken: token).ToListAsync(token)
                )
                from _3 in guard(
                    records.Any(),
                    Error.New(
                        ErrorCodes.EntityListDoesNotExist,
                        ErrorMessages.EntityListDoesNotExist
                    )
                )
                select records
            ).Run()
        ).Match(
            TableOperation.GetEntities,
            err => TableOperation.Failure(Error.New(err.Code, err.Message, err.ToException()))
        );

    public async Task<TableOperation> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from tc in TableClient(_factory, category, table)
                from op in AffMaybe<Response>(
                    async () => await tc.UpsertEntityAsync(data, TableUpdateMode.Merge, token)
                )
                from a in guardnot(
                    op.IsError,
                    Error.New(ErrorCodes.CannotUpdate, ErrorMessages.CannotUpdate)
                )
                select op
            ).Run()
        ).Match(
            _ => TableOperation.Success(),
            err => TableOperation.Failure(Error.New(err.Code, err.Message, err.ToException()))
        );

    private static Eff<Unit> ValidateEmptyString(string s, int errorCode, string errorMessage) =>
        from _1 in guardnot(string.IsNullOrWhiteSpace(s), Error.New(errorCode, errorMessage))
            .ToEff()
        select unit;

    private static Eff<TableClient> TableClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table
    ) =>
        from sc in GetServiceClient(factory, category)
        from tc in GetTableClient(sc, table)
        select tc;
}
