using Azure.Data.Tables;
using Example.Cosmos.Console;
using Funky.Azure.DataTable.Extensions.Commands;
using Funky.Azure.DataTable.Extensions.Queries;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string category = "StudentsCategory";
const string table = "Students";

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(
        (context, services) =>
        {
            var studentConfiguration = context.Configuration
                .GetSection(nameof(StudentConfiguration))
                .Get<StudentConfiguration>();

            services.AddSingleton(studentConfiguration!);
            services.AddAzureClients(
                builder =>
                    builder
                        .AddTableServiceClient(studentConfiguration!.ConnectionString)
                        .WithName(category)
            );
        }
    )
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddUserSecrets<StudentConfiguration>(false, false);
    })
    .Build();

var tableServiceClient = host.Services.GetRequiredService<
    IAzureClientFactory<TableServiceClient>
>();

await AddStudentAsync();
await ReadStudentAsync();

async Task AddStudentAsync()
{
    var productDataModel = StudentDto.New(
        "Cheranga",
        "STU-001",
        new DateTime(1982, 11, 1),
        DateTime.UtcNow.AddYears(-1),
        Gender.Male
    );
    var op = await CommandExtensions.UpsertAsync(
        tableServiceClient,
        "category",
        table,
        productDataModel,
        new CancellationToken()
    );

    Console.WriteLine(
        op.Operation switch
        {
            CommandOperation.CommandSuccessOperation _ => "successfully upserted",
            CommandOperation.CommandFailedOperation f
                => $"{f.ErrorCode}:{f.ErrorMessage}:{f.Exception}",
            _ => "unsupported"
        }
    );
}

async Task ReadStudentAsync()
{
    var operation =
        await Funky.Azure.DataTable.Extensions.Queries.QueryExtensions.GetEntityAsync<StudentDto>(
            tableServiceClient,
            category,
            table,
            "STU-001",
            "STU-001"
        );

    Console.WriteLine(
        operation.Response switch
        {
            QueryResult.SingleResult<StudentDto> s => $"{s.Entity.StudentId} {s.Entity.Name}",
            _ => "error"
        }
    );
}
