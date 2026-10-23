using HomeService.Domain.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace HomeService.Infraestructure.Persistence.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<HouseZone> HouseZones { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<PresenceSensor> Lights { get; set; }
        public DbSet<Roomba> PresenceSensors { get; set; }
        public DbSet<Light> RainSensors { get; set; }
        public DbSet<Roomba> Roombas { get; set; }
        public DbSet<TemperatureSensor> TemperatureSensors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) { }
    }
}
