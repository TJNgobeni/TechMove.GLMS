using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using TechMove.GLMS.Core.Entities;
using ContractStatus = TechMove.GLMS.Core.Entities.ContractStatus;

namespace TechMove.GLMS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Client
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContactDetails).HasMaxLength(500);
                entity.Property(e => e.Region).HasMaxLength(100);
                entity.HasMany(e => e.Contracts)
                      .WithOne(c => c.Client)
                      .HasForeignKey(c => c.ClientId);
            });

            // Contract
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasDefaultValue(ContractStatus.Draft);
                entity.Property(e => e.ServiceLevel).HasMaxLength(100);
                entity.HasMany(e => e.ServiceRequests)
                      .WithOne(s => s.Contract)
                      .HasForeignKey(s => s.ContractId);
                entity.HasIndex(e => e.ClientId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.StartDate, e.EndDate });
            });

            // ServiceRequest
            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Cost).HasPrecision(18, 2);
                entity.Property(e => e.CostZAR).HasPrecision(18, 2);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
                entity.HasIndex(e => e.ContractId);
                entity.HasIndex(e => e.Status);
            });

            // Seed Data
            // Removed hardcoded client seed entries because constrained Region values caused mismatches.

            base.OnModelCreating(modelBuilder);
        }
    }
}
