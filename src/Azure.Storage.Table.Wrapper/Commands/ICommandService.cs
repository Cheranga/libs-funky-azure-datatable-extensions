using Azure.Data.Tables;
using static Azure.Storage.Table.Wrapper.Commands.CommandOperation;

namespace Azure.Storage.Table.Wrapper.Commands;

public interface ICommandService
{
    Task<CommandResponse<CommandFailedOperation, CommandSuccessOperation>> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;

    Task<CommandResponse<CommandFailedOperation, CommandSuccessOperation>> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity;
}
