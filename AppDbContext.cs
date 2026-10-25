using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NWSDB.API.Models;

namespace NWSDB.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<WaterConnection> WaterConnections => Set<WaterConnection>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress the EF Core 9 PendingModelChangesWarning so it never throws an exception
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fix decimal precision for models that contain Amount
        modelBuilder.Entity<Bill>()
            .Property(b => b.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        // Foreign Key Relationships
        modelBuilder.Entity<MeterReading>()
            .HasOne(m => m.RecordedByStaff)
            .WithMany()
            .HasForeignKey(m => m.RecordedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Bill)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1. Seed Default Administrator Account. The hash below is only a placeholder: DbInitializer
        //    replaces it at startup with a hash of Seed:AdminPassword (user-secrets / env var).
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "nwsdb_admin",
                Email = "admin@nwsdb.lk",
                Phone = "0112635888",
                Role = "Admin",
                PasswordHash = "$2a$11$qRzT3fU9zY8tM9o6G6o0xeO21r74jQ/hL505FkF9K7g8sU4vN.M8q"
            }
        );

        // 2. Seed Default System Configurations & Tariffs
        modelBuilder.Entity<SystemConfiguration>().HasData(
            new SystemConfiguration 
            { 
                Id = 1, 
                ConfigKey = "Tariff_BaseFixedCharge", 
                ConfigValue = "300.00", 
                Description = "Base monthly connection charge in LKR",
                LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedBy = "System"
            },
            new SystemConfiguration 
            { 
                Id = 2, 
                ConfigKey = "Tariff_UnitRate", 
                ConfigValue = "50.00", 
                Description = "Rate per cubic meter unit in LKR",
                LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedBy = "System"
            },
            new SystemConfiguration 
            { 
                Id = 3, 
                ConfigKey = "Billing_GracePeriodDays", 
                ConfigValue = "30", 
                Description = "Payment grace period before overdue flag",
                LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedBy = "System"
            },
            new SystemConfiguration 
            { 
                Id = 4, 
                ConfigKey = "Billing_LateFinePercentage", 
                ConfigValue = "10.0", 
                Description = "Penalty surcharge percentage on overdue invoices",
                LastModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedBy = "System"
            }
        );
    }
}