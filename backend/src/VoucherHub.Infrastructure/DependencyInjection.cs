using VoucherHub.Application.Wallets;
using VoucherHub.Application.Users.BlockUser;
using VoucherHub.Application.Users.Ports;
using VoucherHub.Application.Identity.Registration;
using VoucherHub.Application.Campaigns.UseCases;
using VoucherHub.Application.Campaigns.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoucherHub.Application.Identity;
using VoucherHub.Application.Identity.Login;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Application.Campaigns;
using VoucherHub.Application.Users;
using VoucherHub.Application.Common.Security;
using VoucherHub.Application.Vouchers;
using VoucherHub.Application.Vouchers.Ports;
using VoucherHub.Application.Vouchers.RedeemVoucher;
using VoucherHub.Application.Wallets.Ports;
using VoucherHub.Application.Wallets.GrantPoints;
using VoucherHub.Domain.Entities;
using VoucherHub.Infrastructure.Identity;
using VoucherHub.Infrastructure.Email;
using VoucherHub.Infrastructure.BackgroundJobs;
using VoucherHub.Infrastructure.Persistence;
using VoucherHub.Infrastructure.Persistence.Identity;
using VoucherHub.Infrastructure.Persistence.Campaigns;
using VoucherHub.Infrastructure.Persistence.Vouchers;
using VoucherHub.Infrastructure.Persistence.Wallets;

namespace VoucherHub.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddVoucherHubInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoucherHubDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing database connection string.")));
        services.Configure<RegistrationOptions>(configuration.GetSection(RegistrationOptions.SectionName));
        services.AddSingleton(configuration.GetSection(RegistrationOptions.SectionName).Get<RegistrationOptions>() ?? new RegistrationOptions());
        services.AddScoped<VoucherHub.Application.Common.Persistence.IDatabaseReadiness, DatabaseReadiness>();
        services.AddScoped<IRegistrationStore, RegistrationStore>();
        services.AddScoped<IPendingPasswordService, PendingPasswordService>();
        services.AddScoped<ICampaignStore, CampaignStore>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher<PendingRegistration>, PasswordHasher<PendingRegistration>>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IVerificationEmailSender, SmtpVerificationEmailSender>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginStore, LoginStore>();
        services.AddScoped<IUserPasswordService, UserPasswordService>();
        services.AddScoped<IAccessTokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserAccess, UserAccess>();
        services.AddScoped<IWalletSessionFactory, WalletSessionFactory>();
        services.AddScoped<IPointHistoryQueries, PointHistoryQueries>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IRedemptionSessionFactory, RedemptionSessionFactory>();
        services.AddScoped<IVoucherQueries, VoucherQueries>();
        services.AddScoped<IRedemptionService, RedemptionService>();
        services.AddScoped<IAdminUserStore, AdminUserStore>();
        services.AddScoped<IAdminUserQueries, AdminUserQueries>();
        services.AddScoped<IAdminUsersService, AdminUsersService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignCatalog, CampaignCatalog>();
        services.AddHostedService<PendingRegistrationCleanupService>();
        return services;
    }
}
