using VoucherHub.Api.Errors;
using System.Text;
using System.Security.Claims;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using VoucherHub.Application.Identity;
using VoucherHub.Infrastructure.Identity;

using VoucherHub.Infrastructure;
using VoucherHub.Infrastructure.Email;
namespace VoucherHub.Api.Configuration;
public static class ServiceConfiguration
{
    public static void AddVoucherHubApi(this WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing JWT configuration.");
        
        if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }
        
        var registration = builder.Configuration.GetSection(RegistrationOptions.SectionName)
            .Get<RegistrationOptions>() ?? new RegistrationOptions();
        var smtp = builder.Configuration.GetSection(SmtpOptions.SectionName)
            .Get<SmtpOptions>() ?? new SmtpOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            jwtOptions.AccessTokenLifetimeMinutes <= 0 ||
            registration.VerificationTokenLifetimeMinutes <= 0 ||
            registration.PendingRegistrationRetentionHours <= 0 ||
            registration.ResendCooldownSeconds < 0 ||
            !Uri.TryCreate(registration.VerificationPageUrl, UriKind.Absolute, out var verificationUri) ||
            verificationUri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(smtp.FromEmail))
            throw new InvalidOperationException("Invalid JWT, registration or SMTP configuration.");
        
        var registrationLimit = builder.Configuration.GetValue("RateLimits:RegistrationPerMinute", 10);
        var loginLimit = builder.Configuration.GetValue("RateLimits:LoginPerMinute", 5);
        var redeemLimit = builder.Configuration.GetValue("RateLimits:RedeemPerMinute", 20);
        if (registrationLimit <= 0 || loginLimit <= 0 || redeemLimit <= 0)
            throw new InvalidOperationException("Rate limits must be positive.");
        
        builder.Services.AddVoucherHubInfrastructure(builder.Configuration);
        
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var trustedProxyAddresses = builder.Configuration.GetSection("Proxy:KnownProxies")
            .Get<string[]>() ?? [];
        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        foreach (var address in trustedProxyAddresses)
        {
            if (!IPAddress.TryParse(address, out var proxyIp))
                throw new InvalidOperationException("Proxy:KnownProxies must contain valid IP addresses.");
            forwardedHeaders.KnownProxies.Add(proxyIp);
        }
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = forwardedHeaders.ForwardedHeaders;
            options.ForwardLimit = forwardedHeaders.ForwardLimit;
            foreach (var ip in forwardedHeaders.KnownProxies) options.KnownProxies.Add(ip);
        });
        builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        builder.Services.AddHybridCache();

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        
        builder.Services.AddAuthorization();
        
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        
            options.AddPolicy("registration", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = registrationLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        
            options.AddPolicy("redeem", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = redeemLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
        
        builder.Services.AddControllers(options => options.Filters.Add<ApiErrorResultFilter>());
        builder.Services.AddOpenApi();

    }
}
