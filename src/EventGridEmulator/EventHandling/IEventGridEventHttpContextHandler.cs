namespace EventGridEmulator.EventHandling;

internal interface IEventGridEventHttpContextHandler
{
    Task HandleAsync(HttpContext context, string topic);
}