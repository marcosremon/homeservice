using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Security;
using HomeService.Infraestructure.BackgroundTasks.Job;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HomeService.Service.UnitTests.TestSetup
{
    // Levanta la WebApi en memoria con el pipeline de verdad (prefijo /api, filtros,
    // binding y serializacion), pero con las aplicaciones sustituidas por mocks y sin
    // los monitores en segundo plano: nada sale a la BBDD ni a los dispositivos.
    public class HomeServiceWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IAlexaApplication> AlexaApplication { get; } = new Mock<IAlexaApplication>();
        public Mock<IAlexaRequestVerifier> AlexaRequestVerifier { get; } = new Mock<IAlexaRequestVerifier>();
        public Mock<IChangeComputerStatusApplication> ChangeComputerStatusApplication { get; } = new Mock<IChangeComputerStatusApplication>();
        public Mock<IPresenceSensorApplication> PresenceSensorApplication { get; } = new Mock<IPresenceSensorApplication>();
        public Mock<IRainSensorApplication> RainSensorApplication { get; } = new Mock<IRainSensorApplication>();
        public Mock<IRoombaApplication> RoombaApplication { get; } = new Mock<IRoombaApplication>();
        public Mock<ITemperatureSensorApplication> TemperatureSensorApplication { get; } = new Mock<ITemperatureSensorApplication>();

        #region ResetMocks
        // La factoria se comparte entre los tests de una clase: cada test empieza con
        // los mocks limpios.
        public void ResetMocks()
        {
            AlexaApplication.Reset();
            AlexaRequestVerifier.Reset();
            ChangeComputerStatusApplication.Reset();
            PresenceSensorApplication.Reset();
            RainSensorApplication.Reset();
            RoombaApplication.Reset();
            TemperatureSensorApplication.Reset();
        }
        #endregion

        #region ConfigureWebHost
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Solo los hosted services del proyecto: el del servidor web tiene que
                // seguir registrado.
                List<ServiceDescriptor> backgroundTasks = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                         descriptor.ImplementationType?.Assembly == typeof(RoombaActivationHandler).Assembly)
                    .ToList();

                foreach (ServiceDescriptor backgroundTask in backgroundTasks)
                    services.Remove(backgroundTask);

                _replace(services, AlexaApplication.Object);
                _replace(services, AlexaRequestVerifier.Object);
                _replace(services, ChangeComputerStatusApplication.Object);
                _replace(services, PresenceSensorApplication.Object);
                _replace(services, RainSensorApplication.Object);
                _replace(services, RoombaApplication.Object);
                _replace(services, TemperatureSensorApplication.Object);
            });
        }
        #endregion

        #region _replace
        private static void _replace<TService>(IServiceCollection services, TService implementation) where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(implementation);
        }
        #endregion
    }
}
