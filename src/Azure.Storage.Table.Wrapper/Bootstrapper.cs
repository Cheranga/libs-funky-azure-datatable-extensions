using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Storage.Table.Wrapper;

public static class Bootstrapper
{
    public static void RegisterTablesWithConnectionString(
        this IServiceCollection services,
        string name,
        string connectionstring
    )
    {
        services.AddAzureClients(builder =>
        {
            builder.AddTableServiceClient(connectionstring).WithName(name);
        });

        services.AddSingleton<ITableService, TableService>();
    }

    public static void RegisterTablesWithManagedIdentity(
        this IServiceCollection services,
        string name,
        string storageAccountUrl
    )
    {
        services.AddAzureClients(builder =>
        {
            builder
                .AddTableServiceClient(storageAccountUrl)
                .WithName(name)
                .WithCredential(new ManagedIdentityCredential());
        });

        services.AddSingleton<ITableService, TableService>();
    }
}
