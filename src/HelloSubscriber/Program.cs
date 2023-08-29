using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// Serilog provides a more concise console logging experience with colored tokens
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.WriteTo.Console();
});

var app = builder.Build();

app.MapPost("/webhook-200", () => Results.Ok());
app.MapPost("/webhook-404", () => Results.NotFound());
app.MapPost("/webhook-400", () => Results.BadRequest());
app.MapPost("/webhook-401", () => Results.Unauthorized());
app.MapPost("/webhook-slow-200", async (CancellationToken cancellationToken) =>
{
    try
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }

    return Results.Ok();
});

app.Run(); 