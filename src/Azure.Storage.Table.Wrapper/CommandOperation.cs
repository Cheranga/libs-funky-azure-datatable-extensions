using System.Diagnostics.CodeAnalysis;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper;

[ExcludeFromCodeCoverage]
public abstract class CommandOperation
{
    private CommandOperation() { }

    public static CommandOperation Success() => CommandSuccessOperation.New();

    public static CommandOperation Fail(Error error) => CommandFailedOperation.New(error);

    public sealed class CommandSuccessOperation : CommandOperation
    {
        private CommandSuccessOperation() { }

        public static CommandOperation New() => new CommandSuccessOperation();
    }

    public sealed class CommandFailedOperation : CommandOperation
    {
        public Error Error { get; }

        private CommandFailedOperation(Error error)
        {
            Error = error;
        }

        public static CommandOperation New(Error error) => new CommandFailedOperation(error);
    }
}
