using System.Text.Json.Serialization;

namespace functions.AI;

// class for deserializing this json: {"error":{"inner_error":{"code":"ResponsibleAIPolicyViolation","content_filter_results":{"sexual":{"filtered":true,"severity":"high"},"violence":{"filtered":false,"severity":"safe"},"hate":{"filtered":false,"severity":"safe"},"self_harm":{"filtered":false,"severity":"safe"}}},"code":"content_filter","message":"The response was filtered due to the prompt triggering Azure OpenAI's content management policy. Please modify your prompt and retry. To learn more about our content filtering policies please read our documentation: \r\nhttps://go.microsoft.com/fwlink/?linkid=2198766.","param":"prompt","type":null}}

public struct ContentFilterResponse
{
    [JsonPropertyName("error")]
    public Error Error { get; set; }
}

public struct Error
{
    [JsonPropertyName("inner_error")]
    public InnerError InnerError { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("param")]
    public string Param { get; set; }

    [JsonPropertyName("type")]
    public object Type { get; set; }
}

public struct InnerError
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("content_filter_results")]
    public ContentFilterResults ContentFilterResults { get; set; }
}

public struct ContentFilterResults
{
    [JsonPropertyName("sexual")]
    public FilterDetail Sexual { get; set; }

    [JsonPropertyName("violence")]
    public FilterDetail Violence { get; set; }

    [JsonPropertyName("hate")]
    public FilterDetail Hate { get; set; }

    [JsonPropertyName("self_harm")]
    public FilterDetail SelfHarm { get; set; }
}

public struct FilterDetail
{
    [JsonPropertyName("filtered")]
    public bool Filtered { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; }
}