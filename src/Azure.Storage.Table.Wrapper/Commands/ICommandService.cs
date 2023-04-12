using Azure.Data.Tables;

namespace Azure.Storage.Table.Wrapper.Commands;

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
