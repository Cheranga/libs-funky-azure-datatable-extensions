using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using LanguageExt.Common;

namespace Azure.Storage.Table.Wrapper.Queries;

[ExcludeFromCodeCoverage]
public abstract class QueryResult
{
    public static QueryFailedResult Fail(Error error) => QueryFailedResult.New(error);

    public static EmptyResult Empty() => EmptyResult.New();

    public static SingleResult<T> Single<T>(T data)
        where T : class, ITableEntity => SingleResult<T>.New(data);

    public static CollectionResult<T> Collection<T>(IEnumerable<T> data)
        where T : class, ITableEntity => CollectionResult<T>.New(data);

    public sealed class EmptyResult : QueryResult
    {
        private EmptyResult() { }

        internal static EmptyResult New() => new();
    }

    public sealed class SingleResult<T> : QueryResult
        where T : class, ITableEntity
    {
        private SingleResult(T entity) => Entity = entity;

        public T Entity { get; }

        internal static SingleResult<T> New(T data) => new(data);
    }

    public sealed class CollectionResult<T> : QueryResult
        where T : class, ITableEntity
    {
        private CollectionResult(IEnumerable<T> entities) =>
            Entities = entities?.ToList() ?? new List<T>();

        public List<T> Entities { get; }

        internal static CollectionResult<T> New(IEnumerable<T> data) => new(data);
    }

    public sealed class QueryFailedResult : QueryResult
    {
        private QueryFailedResult(Error error)
        {
            ErrorCode = error.Code;
            ErrorMessage = error.Message;
            Exception = error.ToException();
        }

        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        internal static QueryFailedResult New(Error error) => new(error);
    }
}
