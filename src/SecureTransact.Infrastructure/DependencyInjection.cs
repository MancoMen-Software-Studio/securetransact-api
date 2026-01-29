using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureTransact.Application.Abstractions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Infrastructure.Cryptography;
using SecureTransact.Infrastructure.EventStore;
using SecureTransact.Infrastructure.Persistence;
using SecureTransact.Infrastructure.Persistence.Contexts;
using SecureTransact.Infrastructure.Persistence.Repositories;
using SecureTransact.Infrastructure.QueryServices;

namespace SecureTransact.Infrastructure;

/// <summary>
/// Dependency injection extensions for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CryptoSettings>(configuration.GetSection(CryptoSettings.SectionName));
        services.Configure<EventStoreSettings>(configuration.GetSection(EventStoreSettings.SectionName));

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "event_store");
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddDbContext<TransactionDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "read_model");
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddSingleton<ICryptoService>(sp =>
        {
            CryptoSettings settings = configuration.GetSection(CryptoSettings.SectionName).Get<CryptoSettings>()
                ?? throw new InvalidOperationException("Cryptography settings not configured.");

            byte[] encryptionKey;
            byte[] hmacKey;

            if (settings.UseKeyVault)
            {
                SecretClient secretClient = new(
                    new Uri(settings.KeyVaultUri!),
                    new DefaultAzureCredential());

                KeyVaultSecret encryptionSecret = secretClient.GetSecret(settings.EncryptionKeySecretName);
                KeyVaultSecret hmacSecret = secretClient.GetSecret(settings.HmacKeySecretName);

                encryptionKey = Convert.FromBase64String(encryptionSecret.Value);
                hmacKey = Convert.FromBase64String(hmacSecret.Value);
            }
            else
            {
                encryptionKey = Convert.FromBase64String(settings.EncryptionKey);
                hmacKey = Convert.FromBase64String(settings.HmacKey);
            }

            return new AesGcmCryptoService(encryptionKey, hmacKey);
        });

        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IEventStore, PostgresEventStore>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddScoped<ITransactionQueryService, TransactionQueryService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds Infrastructure layer services with custom configuration for testing.
    /// </summary>
    public static IServiceCollection AddInfrastructureForTesting(
        this IServiceCollection services,
        string connectionString,
        byte[] encryptionKey,
        byte[] hmacKey)
    {
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<TransactionDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<ICryptoService>(new AesGcmCryptoService(encryptionKey, hmacKey));
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IEventStore, PostgresEventStore>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionQueryService, TransactionQueryService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
