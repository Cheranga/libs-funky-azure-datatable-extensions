﻿using System.Linq.Expressions;
using Azure.Data.Tables;

namespace Azure.Storage.Table.Wrapper;

public interface IQueryService
{
    Task<TableOperation> GetEntityAsync<T>(
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<TableOperation> GetEntityListAsync<T>(
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity;
}