using System.Net;
using Polly;
using Polly.Timeout;

namespace VRChatContentPublisher.Core.Resilience;

// https://github.com/dotnet/extensions/blob/9e8d935cf0e7014d338ebdb7d6d7d4d348d62021/src/Libraries/Microsoft.Extensions.Http.Resilience/Polly/HttpClientResiliencePredicates.cs#L53-L55
// https://github.com/dotnet/extensions/issues/7188
public class AppHttpClientResiliencePredicates
{
    /// <summary>
    /// Determines whether an outcome should be treated by resilience strategies as a transient failure.
    /// </summary>
    /// <returns><see langword="true"/> if outcome is transient, <see langword="false"/> if not.</returns>
    public static bool IsTransient(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: { } response } when IsTransientHttpFailure(response) => true,
        { Exception: { } exception } when IsTransientHttpException(exception) => true,
        _ => false
    };

    /// <summary>
    /// Determines whether an <see cref="HttpResponseMessage"/> should be treated by resilience strategies as a transient failure.
    /// </summary>
    /// <param name="outcome">The outcome of the user-specified callback.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> associated with the execution.</param>
    /// <returns><see langword="true"/> if outcome is transient, <see langword="false"/> if not.</returns>
    public static bool IsTransient(Outcome<HttpResponseMessage> outcome, CancellationToken cancellationToken)
        => IsHttpConnectionTimeout(outcome, cancellationToken)
           || IsTransient(outcome);

    /// <summary>
    /// Determines whether an exception should be treated by resilience strategies as a transient failure.
    /// </summary>
    internal static bool IsTransientHttpException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is HttpRequestException or TimeoutRejectedException;
    }

    internal static bool IsHttpConnectionTimeout(in Outcome<HttpResponseMessage> outcome,
        in CancellationToken cancellationToken)
        => !cancellationToken.IsCancellationRequested
           && outcome.Exception is OperationCanceledException
           {
               InnerException: TimeoutException
           };

    /// <summary>
    /// Determines whether a response contains a transient failure.
    /// </summary>
    /// <remarks> The current handling implementation uses approach proposed by Polly:
    /// <see href="https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs"/>.
    /// </remarks>
    internal static bool IsTransientHttpFailure(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var statusCode = (int)response.StatusCode;

        return statusCode >= InternalServerErrorCode ||
               response.StatusCode == HttpStatusCode.RequestTimeout ||
               statusCode == TooManyRequests;
    }

    private const int InternalServerErrorCode = (int)HttpStatusCode.InternalServerError;

    private const int TooManyRequests = 429;
}