using Azure.Data.Tables;
using Azure.Storage.Table.Wrapper.Core;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;
using static Azure.Storage.Table.Wrapper.Core.AzureTableStorageWrapper;
using static Azure.Storage.Table.Wrapper.Commands.CommandOperation;

namespace Azure.Storage.Table.Wrapper.Commands;

public static class CommandExtensions
{
    public static async Task<
        CommandResponse<CommandFailedOperation, CommandSuccessOperation>
    > UpsertAsync<T>(
        IAzureClientFactory<TableServiceClient> factory,
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
                from tc in TableClient(factory, category, table)
                from op in AffMaybe<Response>(
                    async () => await tc.UpsertEntityAsync(data, TableUpdateMode.Replace, token)
                )
                from _3 in guardnot(op.IsError, Error.New(ErrorCodes.CannotUpsert, op.ReasonPhrase))
                select op
            ).Run()
        ).Match(
            _ => Success(),
            err =>
                Fail(
                    Error.New(
                        ErrorCodes.CannotUpsert,
                        ErrorMessages.CannotUpsert,
                        err.ToException()
                    )
                )
        );

    public static async Task<
        CommandResponse<CommandFailedOperation, CommandSuccessOperation>
    > UpdateAsync<T>(
        IAzureClientFactory<TableServiceClient> factory,
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
                from tc in TableClient(factory, category, table)
                from op in AffMaybe<Response>(
                    async () => await tc.UpsertEntityAsync(data, TableUpdateMode.Merge, token)
                )
                from a in guardnot(
                    op.IsError,
                    Error.New(ErrorCodes.CannotUpdate, ErrorMessages.CannotUpdate)
                )
                select op
            ).Run()
        ).Match(
            _ => Success(),
            err =>
                Fail(
                    Error.New(
                        ErrorCodes.CannotUpdate,
                        ErrorMessages.CannotUpdate,
                        err.ToException()
                    )
                )
        );

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

    private static CommandResponse<CommandFailedOperation, CommandSuccessOperation> Success() =>
        CommandOperation.Success();

    private static CommandResponse<CommandFailedOperation, CommandSuccessOperation> Fail(
        Error error
    ) => CommandOperation.Fail(error);
}
