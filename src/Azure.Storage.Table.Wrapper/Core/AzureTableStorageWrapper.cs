using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;

namespace Azure.Storage.Table.Wrapper.Core;

[ExcludeFromCodeCoverage]
internal static class AzureTableStorageWrapper
{
    public static Eff<TableServiceClient> GetServiceClient(
        IAzureClientFactory<TableServiceClient> factory,
        string category
    ) =>
        EffMaybe<TableServiceClient>(() => factory.CreateClient(category))
            .MapFail(
                ex =>
                    Error.New(
                        ErrorCodes.UnregisteredTableService,
                        ErrorMessages.UnregisteredTableService,
                        ex
                    )
            );

    public static Eff<TableClient> GetTableClient(TableServiceClient serviceClient, string table) =>
        EffMaybe<TableClient>(() => serviceClient.GetTableClient(table))
            .MapFail(
                ex => Error.New(ErrorCodes.TableUnavailable, ErrorMessages.TableUnavailable, ex)
            );
}
