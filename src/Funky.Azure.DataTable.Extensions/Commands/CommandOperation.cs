using System.Diagnostics.CodeAnalysis;
using LanguageExt.Common;

namespace Funky.Azure.DataTable.Extensions.Commands;

[ExcludeFromCodeCoverage]
public abstract class CommandOperation
{
    private CommandOperation() { }

    public static CommandSuccessOperation Success() => CommandSuccessOperation.New();

    public static CommandFailedOperation Fail(Error error) => CommandFailedOperation.New(error);

    public sealed class CommandSuccessOperation : CommandOperation
    {
        private CommandSuccessOperation() { }

        internal static CommandSuccessOperation New() => new();
    }

    public sealed class CommandFailedOperation : CommandOperation
    {
        private CommandFailedOperation(Error error)
        {
            ErrorCode = error.Code;
            ErrorMessage = error.Message;
            Exception = error.ToException();
        }

        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        internal static CommandFailedOperation New(Error error) => new(error);
    }
}
