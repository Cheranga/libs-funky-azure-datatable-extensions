using System.Diagnostics.CodeAnalysis;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper.Commands;

[ExcludeFromCodeCoverage]
public abstract class CommandOperation
{
    private CommandOperation() { }

    internal static CommandOperation Success() => CommandSuccessOperation.New();

    internal static CommandOperation Fail(Error error) => CommandFailedOperation.New(error);

    public sealed class CommandSuccessOperation : CommandOperation
    {
        private CommandSuccessOperation() { }

        internal static CommandOperation New() => new CommandSuccessOperation();
    }

    public sealed class CommandFailedOperation : CommandOperation
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        private CommandFailedOperation(Error error)
        {
            ErrorCode = error.Code;
            ErrorMessage = error.Message;
            Exception = error.ToException();
        }

        internal static CommandOperation New(Error error) => new CommandFailedOperation(error);
    }
}
