using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Security;
using HomeService.Application.Interface.Service;
using HomeService.Application.UseCase;
using HomeService.Infraestructure.BackgroundTasks.Job;
using HomeService.Infraestructure.BackgroundTasks.Worker;
using HomeService.Infraestructure.Gateway;
using HomeService.Infraestructure.Persistence.Repository;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Security.Alexa;
using Microsoft.Extensions.DependencyInjection;

namespace HomeService.Infraestructure.Persistence.Dependencies
{
    public static class DependencyInjection
    {
        #region AddInfrastructureServices
        // Unico punto de entrada que llama Program.cs. El ApplicationDbContext se
        // registra alli con AddDbContext, aqui va todo lo demas.
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Una instancia para todo el proceso: la lectura del .env esta cacheada.
            services.AddSingleton(Settings.GetSettings());

            _addSecurity(services);
            _addRepositories(services);
            _addApplications(services);
            _addGatewayServices(services);
            _addBackgroundTasks(services);

            return services;
        }
        #endregion

        #region _addSecurity
        private static void _addSecurity(IServiceCollection services)
        {
            // Cliente tipado: el contenedor le pasa un HttpClient del pool y el ILogger.
            // Lo inyecta AlexaController. El filtro AlexaAuth tambien lo buscaria aqui,
            // pero hoy no se aplica en ningun controlador.
            services.AddHttpClient<IAlexaRequestVerifier, AlexaRequestVerifier>(client =>
                client.Timeout = TimeSpan.FromSeconds(10));
        }
        #endregion

        #region _addRepositories
        private static void _addRepositories(IServiceCollection services)
        {
            // Scoped porque reciben el ApplicationDbContext, que es scoped; el de computer
            // status solo necesita Settings, pero se deja igual por uniformidad.
            services.AddScoped<IChangeComputerStatusRepository, ChangeComputerStatusRepository>();
            services.AddScoped<ILightRepository, LightRepository>();
            services.AddScoped<IPresenceSensorRepository, PresenceSensorRepository>();
            services.AddScoped<IRainSensorRepository, RainSensorRepository>();
            services.AddScoped<IRoombaRepository, RoombaRepository>();
            services.AddScoped<ITemperatureSensorRepository, TemperatureSensorRepository>();
        }
        #endregion

        #region _addApplications
        private static void _addApplications(IServiceCollection services)
        {
            services.AddScoped<IAlexaApplication, AlexaApplication>();
            services.AddScoped<IChangeComputerStatusApplication, ChangeComputerStatusApplication>();
            services.AddScoped<ILightApplication, LightApplication>();
            services.AddScoped<IPresenceSensorApplication, PresenceSensorApplication>();
            services.AddScoped<IRainSensorApplication, RainSensorApplication>();
            services.AddScoped<IRoombaApplication, RoombaApplication>();
            services.AddScoped<ITemperatureSensorApplication, TemperatureSensorApplication>();
        }
        #endregion

        #region _addGatewayServices
        private static void _addGatewayServices(IServiceCollection services)
        {
            // Singleton los que no dependen de nada scoped: MqttService comparte la
            // conexion con el broker y el resto no guarda estado por peticion.
            services.AddSingleton<IMqttService, MqttService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IComputerStatusService, ComputerStatusService>();
            services.AddSingleton<IGeminiService, GeminiService>();
            services.AddSingleton<ITemperatureSensorService, TemperatureSensorService>();
            services.AddSingleton<IRoombaService, RoombaService>();

            // Scoped porque LightService depende de ILightRepository, y AlexaService de
            // LightService.
            services.AddScoped<ILightService, LightService>();
            services.AddScoped<IAlexaService, AlexaService>();
        }
        #endregion

        #region _addBackgroundTasks
        private static void _addBackgroundTasks(IServiceCollection services)
        {
            // IHostedService: el host los arranca al levantar y los para al cerrar.
            services.AddHostedService<PresenceSensorMonitor>();
            services.AddHostedService<RainSensorMonitor>();
            services.AddHostedService<RoombaActivationHandler>();
        }
        #endregion
    }
}
