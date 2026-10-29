using HomeService.Domain.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace HomeService.Infraestructure.Persistence.Context
{
    // El esquema lo crea DatabaseMigrator, no EF: aqui solo se describe el mapeo
    // contra las tablas que ya existen. Las entidades son POCOs sin atributos, asi
    // que los nombres de tabla y columna salen todos de OnModelCreating.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<HouseZone> HouseZones { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Light> Lights { get; set; }
        public DbSet<PresenceSensor> PresenceSensors { get; set; }
        public DbSet<RainSensor> RainSensors { get; set; }
        public DbSet<Roomba> Roombas { get; set; }
        public DbSet<TemperatureSensor> TemperatureSensors { get; set; }

        #region OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _configureHouseZone(modelBuilder);
            _configureDevice(modelBuilder);
            _configureLight(modelBuilder);
            _configurePresenceSensor(modelBuilder);
            _configureRainSensor(modelBuilder);
            _configureRoomba(modelBuilder);
            _configureTemperatureSensor(modelBuilder);
        }
        #endregion

        #region _configureHouseZone
        private static void _configureHouseZone(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<HouseZone>(entity =>
            {
                entity.ToTable("house_zone");
                entity.HasKey(houseZone => houseZone.HouseZoneId);

                entity.Property(houseZone => houseZone.HouseZoneId).HasColumnName("house_zone_id").UseSerialColumn();
                entity.Property(houseZone => houseZone.Callout).HasColumnName("callout");
            });
        #endregion

        #region _configureDevice
        private static void _configureDevice(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("device");
                entity.HasKey(device => device.DeviceId);

                entity.Property(device => device.DeviceId).HasColumnName("device_id").UseSerialColumn();
                entity.Property(device => device.HouseZoneId).HasColumnName("house_zone_id");
                entity.Property(device => device.DeviceType).HasColumnName("device_type");
                entity.Property(device => device.DeviceName).HasColumnName("device_name");
                entity.Property(device => device.Model).HasColumnName("model");
                entity.Property(device => device.Manufacturer).HasColumnName("manufacturer");
                entity.Property(device => device.MacAddress).HasColumnName("mac_address");

                // Las entidades no tienen propiedades de navegacion: la relacion se
                // declara solo por la clave ajena.
                entity.HasOne<HouseZone>()
                    .WithMany()
                    .HasForeignKey(device => device.HouseZoneId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion

        #region _configureLight
        private static void _configureLight(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Light>(entity =>
            {
                entity.ToTable("light");
                entity.HasKey(light => light.LightId);

                entity.Property(light => light.LightId).HasColumnName("light_id").UseSerialColumn();
                entity.Property(light => light.DeviceId).HasColumnName("device_id");
                entity.Property(light => light.Location).HasColumnName("location");
                entity.Property(light => light.MqttTopic).HasColumnName("mqtt_topic");
                entity.Property(light => light.IsOn).HasColumnName("is_on");
                entity.Property(light => light.Brightness).HasColumnName("brightness");
                entity.Property(light => light.Color).HasColumnName("color");
                entity.Property(light => light.ColorTemperature).HasColumnName("color_temperature");
                entity.Property(light => light.LastStatusChange).HasColumnName("last_status_change").HasColumnType("timestamp without time zone");
                entity.Property(light => light.IsOnline).HasColumnName("is_online");
                entity.Property(light => light.LastSeen).HasColumnName("last_seen").HasColumnType("timestamp without time zone");

                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(light => light.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion

        #region _configurePresenceSensor
        private static void _configurePresenceSensor(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<PresenceSensor>(entity =>
            {
                entity.ToTable("presence_sensor");
                entity.HasKey(presenceSensor => presenceSensor.PresenceSensorId);

                entity.Property(presenceSensor => presenceSensor.PresenceSensorId).HasColumnName("presence_sensor_id").UseSerialColumn();
                entity.Property(presenceSensor => presenceSensor.DeviceId).HasColumnName("device_id");

                // La columna es INTEGER, no BIGINT: la propiedad es long, asi que el tipo
                // se fuerza para que EF no mande un bigint contra un int4.
                entity.Property(presenceSensor => presenceSensor.Ts).HasColumnName("ts").HasColumnType("integer");

                entity.Property(presenceSensor => presenceSensor.Presence).HasColumnName("presence");
                entity.Property(presenceSensor => presenceSensor.DistanceCm).HasColumnName("distance_cm");
                entity.Property(presenceSensor => presenceSensor.Motion).HasColumnName("motion");
                entity.Property(presenceSensor => presenceSensor.LastDetectedPresence).HasColumnName("last_detected_presence").HasColumnType("timestamp without time zone");

                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(presenceSensor => presenceSensor.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion

        #region _configureRainSensor
        private static void _configureRainSensor(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<RainSensor>(entity =>
            {
                entity.ToTable("rain_sensor");
                entity.HasKey(rainSensor => rainSensor.RainSensorId);

                entity.Property(rainSensor => rainSensor.RainSensorId).HasColumnName("rain_sensor_id").UseSerialColumn();
                entity.Property(rainSensor => rainSensor.DeviceId).HasColumnName("device_id");
                entity.Property(rainSensor => rainSensor.AdcValue).HasColumnName("adc_value");
                entity.Property(rainSensor => rainSensor.WetnessPercent).HasColumnName("wetness_percent");
                entity.Property(rainSensor => rainSensor.IsRaining).HasColumnName("is_raining");
                entity.Property(rainSensor => rainSensor.MeasureAt).HasColumnName("measure_at").HasColumnType("timestamp without time zone");
                entity.Property(rainSensor => rainSensor.LastDetectedRain).HasColumnName("last_detected_rain").HasColumnType("timestamp without time zone");

                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(rainSensor => rainSensor.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion

        #region _configureRoomba
        private static void _configureRoomba(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Roomba>(entity =>
            {
                entity.ToTable("roomba");
                entity.HasKey(roomba => roomba.RoombaId);

                entity.Property(roomba => roomba.RoombaId).HasColumnName("roomba_id").UseSerialColumn();
                entity.Property(roomba => roomba.DeviceId).HasColumnName("device_id");
                entity.Property(roomba => roomba.Phase).HasColumnName("phase");
                entity.Property(roomba => roomba.BatteryPercent).HasColumnName("battery_percent");
                entity.Property(roomba => roomba.BinFull).HasColumnName("bin_full");
                entity.Property(roomba => roomba.LastTarget).HasColumnName("last_target");
                entity.Property(roomba => roomba.LastRoombaActivation).HasColumnName("last_roomba_activation").HasColumnType("timestamp without time zone");
                entity.Property(roomba => roomba.LastRoombaEnd).HasColumnName("last_roomba_end").HasColumnType("timestamp without time zone");
                entity.Property(roomba => roomba.LastCleanDurationMinutes).HasColumnName("last_clean_duration_minutes");
                entity.Property(roomba => roomba.ErrorCode).HasColumnName("error_code");
                entity.Property(roomba => roomba.ErrorMessage).HasColumnName("error_message");
                entity.Property(roomba => roomba.PmapId).HasColumnName("pmap_id");
                entity.Property(roomba => roomba.UserPmapvId).HasColumnName("user_pmapv_id");
                entity.Property(roomba => roomba.IsOnline).HasColumnName("is_online");
                entity.Property(roomba => roomba.LastSeen).HasColumnName("last_seen").HasColumnType("timestamp without time zone");

                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(roomba => roomba.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion

        #region _configureTemperatureSensor
        private static void _configureTemperatureSensor(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TemperatureSensor>(entity =>
            {
                entity.ToTable("temperature_sensor");
                entity.HasKey(temperatureSensor => temperatureSensor.TemperatureSensorId);

                entity.Property(temperatureSensor => temperatureSensor.TemperatureSensorId).HasColumnName("temperature_sensor_id").UseSerialColumn();
                entity.Property(temperatureSensor => temperatureSensor.DeviceId).HasColumnName("device_id");

                // Unica columna cuyo nombre no es el snake_case de la propiedad.
                entity.Property(temperatureSensor => temperatureSensor.Temperature).HasColumnName("temperature_celsius");

                entity.Property(temperatureSensor => temperatureSensor.AdcVoltage).HasColumnName("adc_voltage");
                entity.Property(temperatureSensor => temperatureSensor.MeasureAt).HasColumnName("measure_at");

                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(temperatureSensor => temperatureSensor.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        #endregion
    }
}
