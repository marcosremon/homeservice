using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;

namespace HomeService.Application.UseCase;

// El repositorio se guarda sin usarse todavia, igual que en el proyecto python:
// ILightApplication no declara ningun metodo.
public class LightApplication : ILightApplication
{
    private readonly ILightRepository _lightRepository;

    public LightApplication(ILightRepository lightRepository) => _lightRepository = lightRepository;
}
