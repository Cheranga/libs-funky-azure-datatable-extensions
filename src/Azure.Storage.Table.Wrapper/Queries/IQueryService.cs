﻿using System.Linq.Expressions;
using Azure.Data.Tables;
using static Azure.Storage.Table.Wrapper.Queries.QueryResult;

namespace Azure.Storage.Table.Wrapper.Queries;

public interface IQueryService
{
    Task<QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>>> GetEntityAsync<T>(
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<
        QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>, CollectionResult<T>>
    > GetEntityListAsync<T>(
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity;
}
