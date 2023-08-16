﻿using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal abstract class BaseEventHttpContextHander<TEvent>
{
    private readonly HttpClient _httpClient;
    private readonly ISubscriberCancellationTokenRegistry _cancellationTokenRegistry;
    private readonly IOptionsMonitor<TopicOptions> _options;
    private readonly ILogger _logger;

    protected BaseEventHttpContextHander(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        ILogger logger)
    {
        this._httpClient = httpClientFactory.CreateClient(SubscriberConstants.HttpClientName);
        this._cancellationTokenRegistry = cancellationTokenRegistry;
        this._options = options;
        this._logger = logger;
    }

    public async Task HandleAsync(HttpContext context, string topic)
    {
        var result = await this.HandleInternalAsync(context, topic);
        await result.ExecuteAsync(context);
    }

    private async Task<IResult> HandleInternalAsync(HttpContext context, string topic)
    {
        if (!this._options.CurrentValue.Topics.TryGetValue(topic, out var subscribers))
        {
            return Results.Ok();
        }

        var events = await EventsSerializer.DeserializeEventsAsync<TEvent>(context);
        if (events == null)
        {
            return Results.BadRequest();
        }

        foreach (var subscriber in subscribers)
        {
            var cancellationToken = this._cancellationTokenRegistry.Get(topic, subscriber);
            _ = this.SendEventsToSubscriberFireAndForget(topic, subscriber, events, cancellationToken);
        }

        return Results.Ok();
    }

    private async Task SendEventsToSubscriberFireAndForget(string topic, string subscriber, TEvent[] events, CancellationToken cancellationToken)
    {
        var info = new SubscriberRequestInfo(topic, subscriber);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, subscriber);
            request.Content = JsonContent.Create(events);
            request.Options.Set(SubscriberRequestInfo.HttpOptionKey, info);

            using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // This happens when the application stops or when the subscriber was removed from the configuration at runtime
            info.LogRequestCompletelyCanceled(this._logger);
        }
        catch (HttpRequestException ex) when (ex.InnerException is OperationCanceledException)
        {
            // This happens when each individual retry has timed out, but we already took care of logging in our retry handler
        }
        catch (Exception ex)
        {
            info.LogRequestFailed(this._logger, ex);
        }
    }
}