using CharonGateway.Models;
using Microsoft.EntityFrameworkCore;

namespace CharonGateway.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Metric> Metrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.ToTable("Metrics");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

