using System.Diagnostics.CodeAnalysis;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper;

// [ExcludeFromCodeCoverage]
// public class TableOperationException : Exception
// {
//     public TableOperationException(Error error) : base(error.Message, error.ToException()) { }
// }

[ExcludeFromCodeCoverage]
public record TableOperationError : Error
{
    // public TableOperationException Exception { get; }

    private TableOperationError(Error error)
        : base(error)
    {
        Message = error.Message;
    }

    // public override int Code { get; }


    public override string Message { get; }
    public override bool IsExceptional => true;
    public override bool IsExpected => false;

    public override bool Is<E>() => true;

    public override ErrorException ToErrorException() => ErrorException.New(ToException());

    // public static TableOperationError New(
    //     int errorCode,
    //     string errorMessage,
    //     Exception? exception = null
    // ) =>
    //     exception is null
    //         ? new TableOperationError(Error.New(errorCode, errorMessage))
    //         : new TableOperationError(Error.New(errorCode, errorMessage, exception!));
}
