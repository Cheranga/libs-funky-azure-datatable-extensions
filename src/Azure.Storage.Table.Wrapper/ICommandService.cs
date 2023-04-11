using Azure.Data.Tables;

namespace Azure.Storage.Table.Wrapper;

public interface ICommandService
{
    Task<TableOperation> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<TableOperation> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;
}
