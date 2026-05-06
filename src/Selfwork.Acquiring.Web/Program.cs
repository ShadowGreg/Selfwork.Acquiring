using Selfwork.Acquiring.Client.Extensions;
using Selfwork.Acquiring.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSelfworkAcquiring(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();

// Verify API connectivity at startup (non-fatal — log and continue)
using (var scope = app.Services.CreateScope())
{
    var acquiring = scope.ServiceProvider.GetRequiredService<IAcquiringService>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var verify = await acquiring.VerifyAsync();
        log.LogInformation("Selfwork API connected. Account: {Account}", verify.Account);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Selfwork API connectivity check failed at startup");
    }
}

app.Run();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program { }
