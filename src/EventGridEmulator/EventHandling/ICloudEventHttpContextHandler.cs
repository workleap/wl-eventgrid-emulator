namespace EventGridEmulator.EventHandling;

internal interface ICloudEventHttpContextHandler
{
    Task HandleAsync(HttpContext context, string topic, bool batch);
}