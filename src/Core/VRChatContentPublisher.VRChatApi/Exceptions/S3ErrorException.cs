using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace VRChatContentPublisher.VRChatApi.Exceptions;

/// <summary>
/// Thrown when AWS S3 (or a pre-signed S3 url) replies with an HTTP error response.
/// </summary>
/// <remarks>
/// S3 reports failures as an XML document, for example:
/// <code>
/// &lt;Error&gt;
///   &lt;Code&gt;SignatureDoesNotMatch&lt;/Code&gt;
///   &lt;Message&gt;The request signature we calculated does not match the signature you provided.&lt;/Message&gt;
///   &lt;RequestId&gt;...&lt;/RequestId&gt;
///   &lt;HostId&gt;...&lt;/HostId&gt;
/// &lt;/Error&gt;
/// </code>
/// The parsed error code and message are always included in <see cref="Exception.Message"/>.
/// </remarks>
public sealed class S3ErrorException : Exception
{
    /// <summary>Maximum length of the raw response body kept for diagnostics.</summary>
    private const int MaxRawResponseBodyLength = 2048;

    /// <summary>Maximum length of the raw response body snippet embedded into the exception message.</summary>
    private const int MaxResponseBodyPreviewLength = 512;

    public S3ErrorException(
        int statusCode,
        string? errorCode = null,
        string? errorMessage = null,
        string? requestId = null,
        string? hostId = null,
        string? resource = null,
        string? rawResponseBody = null)
        : base(BuildMessage(statusCode, errorCode, errorMessage, requestId, hostId, resource, rawResponseBody))
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RequestId = requestId;
        HostId = hostId;
        Resource = resource;
        RawResponseBody = rawResponseBody;
    }

    /// <summary>Http status code of the S3 error response.</summary>
    public int StatusCode { get; }

    /// <summary>S3 error code (the <c>Code</c> element of the S3 error response), if it could be parsed.</summary>
    public string? ErrorCode { get; }

    /// <summary>S3 error message (the <c>Message</c> element of the S3 error response), if it could be parsed.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Request id (the <c>RequestId</c> element of the S3 error response), if it could be parsed.</summary>
    public string? RequestId { get; }

    /// <summary>Host id (the <c>HostId</c> element of the S3 error response), if it could be parsed.</summary>
    public string? HostId { get; }

    /// <summary>
    /// Resource the request failed for (the <c>Resource</c>, <c>BucketName</c> or <c>Key</c> element of the S3 error
    /// response), if it could be parsed.
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// Raw S3 response body, kept for diagnostics since it cannot be read anymore once the response is disposed.
    /// </summary>
    public string? RawResponseBody { get; }

    /// <summary>
    /// Reads the error response body and creates an <see cref="S3ErrorException"/> from it.
    /// </summary>
    /// <remarks>
    /// Reading and parsing the body never throws. A missing or unrecognized body only results in a less detailed
    /// exception, the raw body is preserved in <see cref="RawResponseBody"/> in this case.
    /// </remarks>
    public static async ValueTask<S3ErrorException> FromResponseAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        string? responseBody = null;
        try
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The response body is diagnostic data only, never fail because it could not be read.
        }

        var error = ParseErrorResponseBody(responseBody);

        return new S3ErrorException(
            (int)response.StatusCode,
            error.Code,
            error.Message,
            error.RequestId,
            error.HostId,
            error.Resource,
            Truncate(responseBody, MaxRawResponseBodyLength));
    }

    private static S3ErrorPayload ParseErrorResponseBody(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return default;

        XElement? errorElement;
        try
        {
            var rootElement = XDocument.Parse(responseBody).Root;
            if (rootElement is null)
                return default;

            // S3 returns an <Error> root element, some S3 compatible services nest it in another root element.
            errorElement = rootElement.DescendantsAndSelf()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "Error", StringComparison.OrdinalIgnoreCase));
        }
        catch (XmlException)
        {
            // Not an XML document at all (for example an error page from a proxy), keep the payload empty.
            return default;
        }

        if (errorElement is null)
            return default;

        var code = GetElementValue(errorElement, "Code");
        var message = GetElementValue(errorElement, "Message");
        var requestId = GetElementValue(errorElement, "RequestId");
        var hostId = GetElementValue(errorElement, "HostId");

        var resource = GetElementValue(errorElement, "Resource");
        if (resource is null)
        {
            var key = GetElementValue(errorElement, "Key");
            var bucketName = GetElementValue(errorElement, "BucketName");
            resource = key is null ? null : bucketName is null ? key : $"{bucketName}/{key}";
        }

        return new S3ErrorPayload(code, message, requestId, hostId, resource);
    }

    private static string? GetElementValue(XElement parent, string elementName)
    {
        var value = parent.Elements()
            .FirstOrDefault(element =>
                string.Equals(element.Name.LocalName, elementName, StringComparison.OrdinalIgnoreCase))
            ?.Value.Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string BuildMessage(int statusCode, string? errorCode, string? errorMessage, string? requestId,
        string? hostId, string? resource, string? rawResponseBody)
    {
        var builder = new StringBuilder();
        builder.Append("S3 request failed with status code ").Append(statusCode).Append(": ");

        if (errorCode is not null || errorMessage is not null)
        {
            builder.Append("Code=").Append(errorCode ?? "(not specified)")
                .Append(", Message=").Append(errorMessage ?? "(not specified)");
        }
        else
        {
            builder.Append("response body does not contain a recognizable S3 error, Code and Message are unavailable.");

            if (!string.IsNullOrWhiteSpace(rawResponseBody))
                builder.Append(" Response body: ")
                    .Append(Truncate(rawResponseBody, MaxResponseBodyPreviewLength));
        }

        if (requestId is not null)
            builder.Append(", RequestId=").Append(requestId);

        if (hostId is not null)
            builder.Append(", HostId=").Append(hostId);

        if (resource is not null)
            builder.Append(", Resource=").Append(resource);

        return builder.ToString();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength] + "...";

    private readonly record struct S3ErrorPayload(
        string? Code,
        string? Message,
        string? RequestId,
        string? HostId,
        string? Resource);
}
