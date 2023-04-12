using System.Linq.Expressions;
using Azure.Data.Tables;

namespace Azure.Storage.Table.Wrapper.Queries;

public interface IQueryService
{
    Task<QueryOperation> GetEntityAsync<T>(
        string category,
        string table,
        string partitionKey,
        string rowKey,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<QueryOperation> GetEntityListAsync<T>(
        string category,
        string table,
        Expression<Func<T, bool>> filter,
        CancellationToken token
    )
        where T : class, ITableEntity;
}
