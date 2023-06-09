﻿using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using static Funky.Azure.DataTable.Extensions.Commands.CommandOperation;

namespace Funky.Azure.DataTable.Extensions.Commands;

public class CommandService : ICommandService
{
    private readonly IAzureClientFactory<TableServiceClient> _factory;

    public CommandService(IAzureClientFactory<TableServiceClient> factory) => _factory = factory;

    public Task<CommandResponse<CommandFailedOperation, CommandSuccessOperation>> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        CommandExtensions.UpsertAsync(_factory, category, table, data, token);

    public Task<CommandResponse<CommandFailedOperation, CommandSuccessOperation>> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        CommandExtensions.UpdateAsync(_factory, category, table, data, token);
}
