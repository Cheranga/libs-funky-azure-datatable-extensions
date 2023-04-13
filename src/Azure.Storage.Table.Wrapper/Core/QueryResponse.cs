using Azure.Storage.Table.Wrapper.Queries;

namespace Azure.Storage.Table.Wrapper.Core;

public record QueryResponse<TA, TB, TC>
    where TA : QueryResult
    where TB : QueryResult
    where TC : QueryResult
{
    public QueryResult Response { get; }

    private QueryResponse(TA data)
    {
        Response = data;
    }

    private QueryResponse(TB data)
    {
        Response = data;
    }

    private QueryResponse(TC data)
    {
        Response = data;
    }

    public static implicit operator QueryResponse<TA, TB, TC>(TA a) => new(a);

    public static implicit operator QueryResponse<TA, TB, TC>(TB b) => new(b);

    public static implicit operator QueryResponse<TA, TB, TC>(TC c) => new(c);
}

public record QueryResponse<TA, TB, TC, TD>
    where TA : QueryResult
    where TB : QueryResult
    where TC : QueryResult
    where TD : QueryResult
{
    public QueryResult Response { get; }

    private QueryResponse(TA data)
    {
        Response = data;
    }

    private QueryResponse(TB data)
    {
        Response = data;
    }

    private QueryResponse(TC data)
    {
        Response = data;
    }

    private QueryResponse(TD data)
    {
        Response = data;
    }

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TA a) => new(a);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TB b) => new(b);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TC c) => new(c);

    public static implicit operator QueryResponse<TA, TB, TC, TD>(TD d) => new(d);
}
