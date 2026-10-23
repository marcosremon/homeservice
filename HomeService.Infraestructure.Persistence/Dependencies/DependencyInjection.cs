using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Security;
using HomeService.Application.UseCase;
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
            // AlexaController lo recibe por constructor y AlexaAuth lo coge de aqui en
            // vez de usar su fabrica estatica.
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
            services.AddScoped<IChangeComputerStatusApplication, ChangeComputerStatusApplication>();
            services.AddScoped<ILightApplication, LightApplication>();
            services.AddScoped<IPresenceSensorApplication, PresenceSensorApplication>();
            services.AddScoped<IRoombaApplication, RoombaApplication>();
            services.AddScoped<ITemperatureSensorApplication, TemperatureSensorApplication>();

            // PENDIENTE: estos dos dependen de services del Gateway que aun no existen
            // (INotificationService e IAlexaService). Registrarlos ya rompe el arranque
            // en Development, porque ahi builder.Build() valida el grafo completo.
            //
            // services.AddScoped<IRainSensorApplication, RainSensorApplication>();
            // services.AddScoped<IAlexaApplication, AlexaApplication>();
        }
        #endregion

        #region _addGatewayServices
        private static void _addGatewayServices(IServiceCollection services)
        {
            // PENDIENTE: Infraestructure.Gateway no esta portado y este proyecto
            // todavia no lo referencia.
            //
            // services.AddSingleton<IMqttService, MqttService>();          // la conexion con el broker se comparte
            // services.AddSingleton<INotificationService, NotificationService>();
            // services.AddSingleton<IComputerStatusService, ComputerStatusService>();
            // services.AddSingleton<IGeminiService, GeminiService>();
            // services.AddSingleton<ITemperatureSensorService, TemperatureSensorService>();
            // services.AddScoped<IRoombaService, RoombaService>();
            // services.AddScoped<ILightService, LightService>();           // depende de ILightRepository
            // services.AddScoped<IAlexaService, AlexaService>();           // depende de ILightService
        }
        #endregion

        #region _addBackgroundTasks
        private static void _addBackgroundTasks(IServiceCollection services)
        {
            // PENDIENTE: los monitores y el handler de activacion del roomba. En
            // ASP.NET van como IHostedService, que el host arranca y para solo.
            //
            // services.AddHostedService<PresenceSensorMonitor>();
            // services.AddHostedService<RainSensorMonitor>();
            // services.AddHostedService<RoombaActivationHandler>();
        }
        #endregion
    }
}
