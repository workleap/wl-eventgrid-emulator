using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace EventGridEmulator.EventHandling;

internal sealed class EventGridPublishHandler
{
    [StringSyntax("Route")]
    public const string CustomTopicRoute = "/{topic}/api/events";

    [StringSyntax("Route")]
    public const string NamespaceTopicRoute = "/topics/{topic}:publish";

    private const string CloudEventContentType = "application/cloudevents+json; charset=utf-8";
    private const string CloudEventBatchContentType = "application/cloudevents-batch+json; charset=utf-8";
    private const string EventGridEventContentType = "application/json";
    private const string EventGridEventWithCharsetContentType = "application/json; charset=utf-8";

    private readonly ILogger<EventGridPublishHandler> _logger;
    private readonly IEventGridEventHttpContextHandler _eventGridEventHttpContextHandler;
    private readonly ICloudEventHttpContextHandler _cloudEventHttpContextHandler;

    public EventGridPublishHandler(ILogger<EventGridPublishHandler> logger, IEventGridEventHttpContextHandler eventGridEventHttpContextHandler, ICloudEventHttpContextHandler cloudEventHttpContextHandler)
    {
        this._logger = logger;
        this._eventGridEventHttpContextHandler = eventGridEventHttpContextHandler;
        this._cloudEventHttpContextHandler = cloudEventHttpContextHandler;
    }

    public static async Task HandleCustomTopicEventAsync(HttpContext context, [FromRoute] string topic, [FromServices] EventGridPublishHandler handler)
    {
        await handler.HandleAsync(context, topic);
    }

    public static async Task<IResult> HandleNamespaceTopicEventAsync(HttpContext context, [FromRoute] string topic, [FromServices] EventGridPublishHandler handler)
    {
        await handler.HandleAsync(context, topic);
        return Results.Ok(new object());
    }

    private Task HandleAsync(HttpContext context, string topic)
    {
        switch (context.Request.ContentType)
        {
            case EventGridEventContentType:
            case EventGridEventWithCharsetContentType:
                return this._eventGridEventHttpContextHandler.HandleAsync(context, topic);

            case CloudEventContentType:
                return this._cloudEventHttpContextHandler.HandleAsync(context, topic, batch: false);

            case CloudEventBatchContentType:
                return this._cloudEventHttpContextHandler.HandleAsync(context, topic, batch: true);

            default:
                this._logger.LogWarning("Content type '{ContentType}' is not supported", context.Request.ContentType);
                return Results.BadRequest().ExecuteAsync(context);
        }
    }
}
