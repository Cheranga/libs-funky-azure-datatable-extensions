using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Azure;
using static LanguageExt.Prelude;

namespace Funky.Azure.DataTable.Extensions.Core;

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

    public static Aff<TableClient> GetTableClient(TableServiceClient serviceClient, string table) =>
        from op in EffMaybe<TableClient>(() => serviceClient.GetTableClient(table))
            .MapFail(
                ex => Error.New(ErrorCodes.TableUnavailable, ErrorMessages.TableUnavailable, ex)
            )
        from props in AffMaybe<Response<IReadOnlyList<TableSignedIdentifier>>>(
                async () => await op.GetAccessPoliciesAsync()
            )
            .MapFail(
                ex => Error.New(ErrorCodes.TableUnavailable, ErrorMessages.TableUnavailable, ex)
            )
        from _ in guard(
            props.HasValue,
            Error.New(ErrorCodes.TableUnavailable, ErrorMessages.TableUnavailable)
        )
        select op;
}
