using System.Linq.Expressions;
using System.Net;
using Azure;
using Azure.Data.Tables;
using Funky.Azure.DataTable.Extensions.Core;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static Funky.Azure.DataTable.Extensions.Core.AzureTableStorageWrapper;
using static LanguageExt.Prelude;
using static Funky.Azure.DataTable.Extensions.Queries.QueryResult;

namespace Funky.Azure.DataTable.Extensions.Queries;

public static class QueryExtensions
{
    public static async Task<
        QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>>
    > GetEntityAsync<T>(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token = new()
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(category)
                from _2 in ValidateEmptyString(table)
                from _3 in ValidateEmptyString(partitionKey)
                from _4 in ValidateEmptyString(rowKey)
                from tc in TableClient(factory, category, table)
                from op in AffMaybe<Response<T>>(
                    async () =>
                        await tc.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: token)
                )
                select op
            )
                .Match(GetSingle, GetError<T>)
                .Run()
        ).Match(op => op, GetError<T>);

    public static async Task<
        QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>, CollectionResult<T>>
    > GetEntityListAsync<T>(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(category)
                from _2 in ValidateEmptyString(table)
                from tc in TableClient(factory, category, table)
                from records in Aff(
                    async () =>
                        await tc.QueryAsync<T>(filter, cancellationToken: token).ToListAsync(token)
                )
                select records?.ToList() ?? new List<T>()
            ).Run()
        ).Match(GetCollection, GetCollectionError<T>);

    private static Eff<Unit> ValidateEmptyString(string s) =>
        from _1 in guardnot(
                string.IsNullOrWhiteSpace(s),
                Error.New(ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
            )
            .ToEff()
        select unit;

    private static QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>> GetSingle<T>(
        Response<T> data
    )
        where T : class, ITableEntity => Single(data.Value);

    private static QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>> GetError<T>(
        Error error
    )
        where T : class, ITableEntity =>
        error.ToException() switch
        {
            RequestFailedException rf
                => rf.Status == (int)HttpStatusCode.NotFound
                    ? Empty()
                    : Fail(
                        Error.New(
                            ErrorCodes.CannotGetDataFromTable,
                            error.Message,
                            error.ToException()
                        )
                    ),
            _ => Fail(error)
        };

    private static QueryResponse<
        QueryFailedResult,
        EmptyResult,
        SingleResult<T>,
        CollectionResult<T>
    > GetCollectionError<T>(Error error)
        where T : class, ITableEntity =>
        Fail(
            Error.New(
                ErrorCodes.CannotGetDataFromTable,
                ErrorMessages.CannotGetDataFromTable,
                error.ToException()
            )
        );

    private static QueryResponse<
        QueryFailedResult,
        EmptyResult,
        SingleResult<T>,
        CollectionResult<T>
    > GetCollection<T>(List<T> items)
        where T : class, ITableEntity =>
        items.Count switch
        {
            0 => Empty(),
            1 => Single(items.First()),
            _ => Collection(items)
        };

    private static Aff<TableClient> TableClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table
    ) =>
        from sc in GetServiceClient(factory, category)
        from tc in GetTableClient(sc, table)
        select tc;
}
