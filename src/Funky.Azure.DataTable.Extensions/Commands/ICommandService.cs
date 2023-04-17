using Azure.Data.Tables;
using static Funky.Azure.DataTable.Extensions.Commands.CommandOperation;

namespace Funky.Azure.DataTable.Extensions.Commands;

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
