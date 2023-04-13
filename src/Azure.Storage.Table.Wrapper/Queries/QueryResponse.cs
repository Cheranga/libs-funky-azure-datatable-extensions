namespace Azure.Storage.Table.Wrapper.Queries;

public class QueryResponse<TA, TB, TC>
    where TA : QueryResult
    where TB : QueryResult
    where TC : QueryResult
{
    private QueryResponse(QueryResult data) => Response = data;

    public QueryResult Response { get; }

    public static implicit operator QueryResponse<TA, TB, TC>(TA a) => new(a);

    public static implicit operator QueryResponse<TA, TB, TC>(TB b) => new(b);

    public static implicit operator QueryResponse<TA, TB, TC>(TC c) => new(c);
}

public class QueryResponse<TA, TB, TC, TD>
    where TA : QueryResult
    where TB : QueryResult
    where TC : QueryResult
    where TD : QueryResult
{
    private QueryResponse(TA data) => Response = data;

    private QueryResponse(TB data) => Response = data;

    private QueryResponse(TC data) => Response = data;

    private QueryResponse(TD data) => Response = data;

    public QueryResult Response { get; }

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TA a) => new(a);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TB b) => new(b);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TC c) => new(c);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TD d) => new(d);
}
