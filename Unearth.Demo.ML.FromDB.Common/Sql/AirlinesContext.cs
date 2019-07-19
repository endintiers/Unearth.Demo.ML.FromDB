using Microsoft.EntityFrameworkCore;
using System;

namespace Unearth.Demo.ML.FromDB.Common.Sql
{
    public partial class AirlinesContext : DbContext
    {
        public AirlinesContext()
        {
        }

        public AirlinesContext(DbContextOptions<AirlinesContext> options)
            : base(options)
        {
        }

        public virtual DbSet<FlightCodes> FlightCodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new ApplicationException("Configure options before use: ex: var options = new DbContextOptionsBuilder<AirlinesContext>().UseInMemoryDatabase(databaseName: \"FlightCodes\").Options;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FlightCodes>(entity =>
            {
                entity.Property(e => e.FlightCode).HasMaxLength(50);
                entity.Property(e => e.Iatacode).HasMaxLength(10);
            });
        }
    }
}
