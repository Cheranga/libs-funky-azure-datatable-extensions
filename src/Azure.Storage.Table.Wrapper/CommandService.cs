using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using static Azure.Storage.Table.Wrapper.AzureTableStorageWrapper;

namespace Azure.Storage.Table.Wrapper;

public class CommandService : ICommandService
{
    private readonly IAzureClientFactory<TableServiceClient> _factory;

    public CommandService(IAzureClientFactory<TableServiceClient> factory)
    {
        _factory = factory;
    }

    public async Task<CommandOperation> UpsertAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from tc in TableClient(_factory, category, table)
                from op in AffMaybe<Response>(
                    async () =>
                        await tc.UpsertEntityAsync(
                            data,
                            mode: TableUpdateMode.Replace,
                            cancellationToken: token
                        )
                )
                from _3 in guardnot(op.IsError, Error.New(ErrorCodes.CannotUpsert, op.ReasonPhrase))
                select op
            ).Run()
        ).Match(_ => CommandOperation.Success(), CommandOperation.Fail);

    public async Task<CommandOperation> UpdateAsync<T>(
        string category,
        string table,
        T data,
        CancellationToken token
    )
        where T : class, ITableEntity =>
        (
            await (
                from _1 in ValidateEmptyString(
                    category,
                    ErrorCodes.Invalid,
                    ErrorMessages.EmptyOrNull
                )
                from _2 in ValidateEmptyString(table, ErrorCodes.Invalid, ErrorMessages.EmptyOrNull)
                from tc in TableClient(_factory, category, table)
                from op in AffMaybe<Response>(
                    async () => await tc.UpsertEntityAsync(data, TableUpdateMode.Merge, token)
                )
                from a in guardnot(
                    op.IsError,
                    Error.New(ErrorCodes.CannotUpdate, ErrorMessages.CannotUpdate)
                )
                select op
            ).Run()
        ).Match(_ => CommandOperation.Success(), CommandOperation.Fail);

    private static Eff<Unit> ValidateEmptyString(string s, int errorCode, string errorMessage) =>
        from _1 in guardnot(string.IsNullOrWhiteSpace(s), Error.New(errorCode, errorMessage))
            .ToEff()
        select unit;

    private static Eff<TableClient> TableClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category,
        string table
    ) =>
        from sc in GetServiceClient(factory, category)
        from tc in GetTableClient(sc, table)
        select tc;
}
