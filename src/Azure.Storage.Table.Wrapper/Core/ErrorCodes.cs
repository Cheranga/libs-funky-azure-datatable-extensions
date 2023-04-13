namespace Azure.Storage.Table.Wrapper.Core;

public static class ErrorCodes
{
    public const int UnregisteredTableService = 500;
    public const int TableUnavailable = 501;
    public const int CannotUpsert = 502;
    public const int EntityDoesNotExist = 503;
    public const int Invalid = 504;
    public const int CannotInsert = 505;
    public const int CannotUpdate = 506;
    public const int EntityListDoesNotExist = 507;
    public const int CannotGetDataFromTable = 508;
}

public static class ErrorMessages
{
    public const string UnregisteredTableService =
        "table service is unregistered for the storage account";

    public const string TableUnavailable = "table is unavailable";
    public const string CannotUpsert = "data cannot be upserted into the storage table";
    public const string EntityDoesNotExist = "entity does not exist";
    public const string Invalid = "invalid";
    public const string CannotInsert = "error occurred when inserting entity to table";
    public const string CannotUpdate = "error occurred when updating entity to table";
    public const string EntityListDoesNotExist = "entities does not exist";
    public const string EmptyOrNull = "empty or null";
    public const string CannotGetDataFromTable = "error occurred when getting data from table";
}
