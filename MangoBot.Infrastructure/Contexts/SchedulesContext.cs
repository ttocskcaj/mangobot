using MangoBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace MangoBot.Infrastructure.Contexts;

public class SchedulesContext : DbContext
{
    public SchedulesContext(DbContextOptions<SchedulesContext> contextOptions)
        : base(contextOptions)
    {
    }
    
    public DbSet<Schedule> Schedules { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>()
            .ToContainer("Schedules")
            .HasNoDiscriminator()
            .HasPartitionKey(_ => _.Id)
            .UseETagConcurrency();
        
        modelBuilder.Entity<Schedule>().Property(_ => _.Id).ToJsonProperty("id");

        base.OnModelCreating(modelBuilder);
    }
}