using Azure.Data.Tables;
using static Azure.Storage.Table.Wrapper.TableOperation;

namespace Azure.Storage.Table.Wrapper;

public interface ICommandService
{
    Task<CommandOperation> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<CommandOperation> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;
}
