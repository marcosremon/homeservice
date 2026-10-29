using HomeService.Infraestructure.Persistence.Context;
using HomeService.Infraestructure.Persistence.CreateDatabase;
using HomeService.Infraestructure.Persistence.Dependencies;
using HomeService.Transversal.Common.Configuration;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

// Npgsql guarda DateTime.MinValue como -infinity por defecto; el esquema y los datos
// existentes usan '0001-01-01 00:00:00'. Tiene que ir antes de cualquier uso de Npgsql.
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Conventions.Insert(0, new RoutePrefixConvention("api"));
});
builder.Services.AddHttpClient();

// La cadena sale de Settings (.env o variables de entorno), no de ConnectionStrings.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(Settings.GetSettings().DatabaseUrl));

builder.Services.AddInfrastructureServices();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Aplicar migraciones de base de datos al arrancar: pone la BBDD al dia sola.
if (!DatabaseMigrator.CreateOrUpdateDatabase())
    Console.WriteLine("Program -> Fallo al aplicar las migraciones de base de datos al arrancar.");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Permite releer el body (necesario para verificacion de firma Alexa)
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthorization();
app.MapControllers();
app.Run();

public class RoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public RoutePrefixConvention(string prefix)
    {
        _routePrefix = new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));
    }

    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            foreach (SelectorModel selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel != null)
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
            }
        }
    }
}
