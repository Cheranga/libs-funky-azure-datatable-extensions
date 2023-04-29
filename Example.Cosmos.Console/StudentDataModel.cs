using Azure;
using Azure.Data.Tables;

namespace Example.Cosmos.Console;

public enum Gender
{
    Unspecified,
    Male,
    Female
}

public class StudentDto : ITableEntity
{
    public string Name { get; set; }
    public string StudentId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime EnrolledIn { get; set; }
    public Gender Gender { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public static StudentDto New(
        string name,
        string studentId,
        DateTime dateOfBirth,
        DateTime enrolledDate,
        Gender gender
    ) =>
        new()
        {
            Name = name,
            StudentId = studentId,
            PartitionKey = studentId.ToUpper(),
            RowKey = studentId.ToUpper(),
            DateOfBirth = dateOfBirth.ToUniversalTime(),
            EnrolledIn = enrolledDate.ToUniversalTime(),
            Gender = gender
        };
}