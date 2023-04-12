using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper.Queries;

[ExcludeFromCodeCoverage]
public abstract class QueryOperation
{
    internal static QueryOperation Fail(Error error) => QueryFailedOperation.New(error);

    internal static QueryOperation Empty() => EmptyResult.New();

    internal static QueryOperation Single<T>(T data)
        where T : class, ITableEntity => SingleResult<T>.New(data);

    internal static QueryOperation Collection<T>(IEnumerable<T> data)
        where T : class, ITableEntity => CollectionResult<T>.New(data);

    public sealed class EmptyResult : QueryOperation
    {
        private EmptyResult() { }

        internal static EmptyResult New() => new();
    }

    public sealed class SingleResult<T> : QueryOperation
        where T : class, ITableEntity
    {
        private SingleResult(T entity) => Entity = entity;

        public T Entity { get; }

        internal static SingleResult<T> New(T data) => new(data);
    }

    public sealed class CollectionResult<T> : QueryOperation
        where T : class, ITableEntity
    {
        private CollectionResult(IEnumerable<T> entities) =>
            Entities = entities?.ToList() ?? new List<T>();

        public List<T> Entities { get; }

        internal static CollectionResult<T> New(IEnumerable<T> data) => new(data);
    }

    public sealed class QueryFailedOperation : QueryOperation
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        private QueryFailedOperation(Error error)
        {
            ErrorCode = error.Code;
            ErrorMessage = error.Message;
            Exception = error.ToException();
        }

        internal static QueryOperation New(Error error) => new QueryFailedOperation(error);
    }
}
