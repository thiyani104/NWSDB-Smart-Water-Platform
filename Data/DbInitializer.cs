using Microsoft.EntityFrameworkCore;

namespace NWSDB.API.Data;

/// <summary>
/// Startup tasks: optional migration and rotation of the seeded administrator credential.
/// The EF seed data ships a well-known placeholder hash for the default admin account;
/// when Seed:AdminPassword is configured (user-secrets / environment variable) and the
/// admin still has a placeholder hash, it is replaced with a hash of the configured password.
/// </summary>
public static class DbInitializer
{
    // Placeholder hashes created by the InitialCreate/SeedDefaultAdmin migrations.
    private static readonly string[] PlaceholderAdminHashes =
    {
        "$2a$11$kM19xNbkMWtvSZ1.onPPpOpUSB6cCZbn7Uu31a7UbkeuihoSTDCXq",
        "$2a$11$qRzT3fU9zY8tM9o6G6o0xeO21r74jQ/hL505FkF9K7g8sU4vN.M8q"
    };

    public const int MinAdminPasswordLength = 12;

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
            {
                await context.Database.MigrateAsync();
            }

            await EnsureAdminCredentialAsync(context, configuration["Seed:AdminPassword"], logger);
        }
        catch (Exception ex)
        {
            // Keep the app up so /healthz can report the real problem instead of crashing on boot
            logger.LogError(ex, "Database initialization failed. Check the connection string and that migrations are applied.");
        }
    }

    public static async Task EnsureAdminCredentialAsync(AppDbContext context, string? configuredPassword, ILogger logger)
    {
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Id == 1 && u.Role == "Admin");
        if (admin == null || !PlaceholderAdminHashes.Contains(admin.PasswordHash))
            return; // no seeded admin, or the credential was already changed

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            logger.LogWarning(
                "The seeded admin '{Username}' still uses a placeholder password hash. " +
                "Set Seed:AdminPassword (user-secrets or environment variable) to rotate it.",
                admin.Username);
            return;
        }

        if (configuredPassword.Length < MinAdminPasswordLength)
        {
            logger.LogError("Seed:AdminPassword must be at least {Min} characters. Admin credential not changed.",
                MinAdminPasswordLength);
            return;
        }

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuredPassword);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded admin '{Username}' credential was rotated from configuration.", admin.Username);
    }
}
