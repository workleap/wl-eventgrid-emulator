using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace EventGridEmulator.EventHandling;

internal sealed class PullQueueHttpContextHandler
{
    private static readonly ReceiveResults EmptyReceiveResult = new() { Value = [] };

    [StringSyntax("Route")]
    public const string ReceiveRoute = "topics/{topic}/eventsubscriptions/{subscription}:receive";

    [StringSyntax("Route")]
    public const string AcknowledgeRoute = "topics/{topic}/eventsubscriptions/{subscription}:acknowledge";

    [StringSyntax("Route")]
    public const string ReleaseRoute = "topics/{topic}/eventsubscriptions/{subscription}:release";

    [StringSyntax("Route")]
    public const string RejectRoute = "topics/{topic}/eventsubscriptions/{subscription}:reject";

    public static async Task<IResult> HandleReceiveAsync([FromServices] ILogger<PullQueueHttpContextHandler> logger, [FromRoute] string topic, [FromRoute] string subscription, [FromServices] TopicSubscribers<CloudEvent> topicSubscribers, CancellationToken cancellationToken)
    {
        // To better emulate EventGrid's behavior, after a while without any event being received, the request should return empty
        var internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        internalCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(50));

        try
        {
            var result = await topicSubscribers.GetEventAsync(topic, subscription, internalCancellationTokenSource.Token);
            var receiveResults = new ReceiveResults
            {
                Value = new[]
                {
                    new EventObject
                    {
                        BrokerProperties = new BrokerProperties
                        {
                            DeliveryCount = 1, // currently only support receiving one event at a time
                            LockToken = result.LockToken,
                        },
                        Event = result.Item,
                    },
                },
            };

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Event pulled from topic '{Topic}' for subscription '{SubscriberName}' with payload '{Payload}'", topic, subscription, EventsSerializer.SerializeEventsForDebugPurposes(receiveResults));
            }

            return Results.Ok(receiveResults);
        }
        catch (OperationCanceledException ex) when (internalCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Internal timeout reached without any event being available, return empty
            return Results.Ok(EmptyReceiveResult);
        }
    }

    public static async Task<IResult> HandleAcknowledgeAsync([FromRoute] string topic, [FromRoute] string subscription, [FromBody] LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> topicSubscribers)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
            {
                if (token is null)
                {
                    continue;
                }

                if (topicSubscribers.TryDeleteEvent(topic, subscription, token))
                {
                    succeededLockTokens.Add(token);
                }
                else
                {
                    failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
                }
            }
        }

        return Results.Ok(new LockTokensResultsData
        {
            FailedLockTokens = failedLockTokens,
            SucceededLockTokens = succeededLockTokens,
        });
    }

    public static async Task<IResult> HandleReleaseAsync([FromRoute] string topic, [FromRoute] string subscription, [FromBody] LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> topicSubscribers)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
            {
                if (token is null)
                {
                    continue;
                }

                if (topicSubscribers.TryReleaseEvent(topic, subscription, token))
                {
                    succeededLockTokens.Add(token);
                }
                else
                {
                    failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
                }
            }
        }

        return Results.Ok(new LockTokensResultsData
        {
            FailedLockTokens = failedLockTokens,
            SucceededLockTokens = succeededLockTokens,
        });
    }

    public static async Task<IResult> HandleRejectAsync([FromRoute] string topic, [FromRoute] string subscription, [FromBody] LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> topicSubscribers)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
            {
                if (token is null)
                {
                    continue;
                }

                if (topicSubscribers.TryDeleteEvent(topic, subscription, token))
                {
                    succeededLockTokens.Add(token);
                }
                else
                {
                    failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
                }
            }
        }

        return Results.Ok(new LockTokensResultsData
        {
            FailedLockTokens = failedLockTokens,
            SucceededLockTokens = succeededLockTokens,
        });
    }
}

internal sealed class ReceiveResults
{
    [JsonPropertyName("value")]
    public EventObject[]? Value { get; set; }
}

internal sealed class EventObject
{
    public BrokerProperties? BrokerProperties { get; set; }

    public CloudEvent? Event { get; set; }
}

internal sealed class BrokerProperties
{
    public int DeliveryCount { get; set; }

    public string? LockToken { get; set; }
}

internal sealed class LockTokensResultsData
{
    public List<FailedLockToken>? FailedLockTokens { get; set; }

    public List<string>? SucceededLockTokens { get; set; }
}

internal sealed class LockTokensRequestData
{
    public string?[]? LockTokens { get; set; }
}

internal sealed class FailedLockToken
{
    public string? LockToken { get; set; }

    public ResponseError? Error { get; set; }
}

internal sealed class ResponseError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}