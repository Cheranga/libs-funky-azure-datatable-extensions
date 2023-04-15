using System.Linq.Expressions;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using static Azure.Storage.Table.Wrapper.Queries.QueryResult;

namespace Azure.Storage.Table.Wrapper.Queries;

internal class QueryService : IQueryService
{
    private readonly IAzureClientFactory<TableServiceClient> _factory;

    public QueryService(IAzureClientFactory<TableServiceClient> factory) => _factory = factory;

    public Task<QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>>> GetEntityAsync<T>(
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        QueryExtensions.GetEntityAsync<T>(_factory, category, table, partitionKey, rowKey, token);

    public Task<
        QueryResponse<QueryFailedResult, EmptyResult, SingleResult<T>, CollectionResult<T>>
    > GetEntityListAsync<T>(
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        QueryExtensions.GetEntityListAsync(_factory, category, table, filter, token);
}
