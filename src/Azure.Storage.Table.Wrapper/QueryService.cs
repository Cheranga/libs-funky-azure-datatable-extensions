﻿using System.Linq.Expressions;
using Azure.Data.Tables;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static Azure.Storage.Table.Wrapper.AzureTableStorageWrapper;
using static LanguageExt.Prelude;

namespace Azure.Storage.Table.Wrapper;

internal class QueryService : IQueryService
{
    private readonly IAzureClientFactory<TableServiceClient> _factory;

    public QueryService(IAzureClientFactory<TableServiceClient> factory)
    {
        _factory = factory;
    }

    private static Eff<Unit> ValidateEmptyString(string s, int errorCode, string errorMessage) =>
        from _1 in guardnot(string.IsNullOrWhiteSpace(s), Error.New(errorCode, errorMessage))
            .ToEff()
        select unit;

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

    private static Eff<TableClient> TableClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table
    ) =>
        from sc in GetServiceClient(factory, category)
        from tc in GetTableClient(sc, table)
        select tc;
}