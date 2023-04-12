using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper;

[ExcludeFromCodeCoverage]
public abstract class TableOperation
{
    private TableOperation() { }

    public abstract class CommandOperation : TableOperation
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

    public abstract class QueryOperation : TableOperation
    {
        public static QueryOperation Fail(Error error) => QueryFailedOperation.New(error);

        public static QueryOperation Empty() => EmptyResult.New();

        public static QueryOperation Single<T>(T data)
            where T : class, ITableEntity => SingleResult<T>.New(data);

        public static QueryOperation Collection<T>(IEnumerable<T> data)
            where T : class, ITableEntity => CollectionResult<T>.New(data);

        public sealed class EmptyResult : QueryOperation
        {
            private EmptyResult() { }

            public static EmptyResult New() => new();
        }

        public sealed class SingleResult<T> : QueryOperation
            where T : class, ITableEntity
        {
            private SingleResult(T entity) => Entity = entity;

            public T Entity { get; }

            public static SingleResult<T> New(T data) => new(data);
        }

        public sealed class CollectionResult<T> : QueryOperation
            where T : class, ITableEntity
        {
            private CollectionResult(IEnumerable<T> entities) =>
                Entities = entities?.ToList() ?? new List<T>();

            public List<T> Entities { get; }

            public static CollectionResult<T> New(IEnumerable<T> data) => new(data);
        }

        public sealed class QueryFailedOperation : QueryOperation
        {
            public Error Error { get; }

            private QueryFailedOperation(Error error)
            {
                Error = error;
            }

            public static QueryOperation New(Error error) => new QueryFailedOperation(error);
        }
    }
}
