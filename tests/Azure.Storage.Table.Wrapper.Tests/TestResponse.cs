using System.Net;
using Azure.Core;
using Azure.Data.Tables;

namespace Azure.Storage.Table.Wrapper.Tests;

public class TestResponse : Response
{
    private TestResponse(HttpStatusCode statusCode)
    {
        Status = (int)statusCode;
    }

    private TestResponse(string reason)
    {
        ReasonPhrase = reason;
    }

    public override void Dispose() { }

    protected override bool TryGetHeader(string name, out string? value)
    {
        value = string.Empty;
        return true;
    }

    protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
    {
        values = new[] { string.Empty };
        return true;
    }

    protected override bool ContainsHeader(string name) => true;

    protected override IEnumerable<HttpHeader> EnumerateHeaders() => Array.Empty<HttpHeader>();

    public override int Status { get; }
    public override string ReasonPhrase { get; }
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; }
    public override bool IsError => !string.IsNullOrWhiteSpace(ReasonPhrase);

    public static TestResponse Success() => new(HttpStatusCode.OK);

    public static TestResponse Fail(string reason) => new(reason);
}

public class TestResponse<T> : Response<T>
{
    private readonly TestResponse _response;

    private TestResponse(TestResponse response)
    {
        _response = response;
    }

    private TestResponse(TestResponse response, T data)
    {
        _response = response;
        Value = data;
    }

    public override Response GetRawResponse() => _response;

    public override T Value { get; }

    public static TestResponse<T> Fail(string reason) => new(TestResponse.Fail(reason));

    public static TestResponse<T> Success(T data) => new(TestResponse.Success(), data);
}
