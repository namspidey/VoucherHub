using VoucherHub.Api.Configuration;
using VoucherHub.Application.Common.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddVoucherHubApi();
var app = builder.Build();
app.UseExceptionHandler();
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (IDatabaseReadiness database, CancellationToken ct) =>
    await database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.MapControllers();
app.Run();
