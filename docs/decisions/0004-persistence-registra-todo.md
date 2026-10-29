# 0004 — `Persistence` registra la DI; nadie más la referencia

**Estado**: aceptada

## Contexto

`AddInfrastructureServices` (`Infraestructure.Persistence/Dependencies/DependencyInjection.cs`) es el único punto de registro que llama `Program.cs`. Registra repositorios, casos de uso, services del Gateway, tareas de fondo y el verificador de firma de Alexa. Para eso `Persistence` referencia `Gateway`, `BackgroundTasks` y `Security`.

## Decisión

`Gateway`, `BackgroundTasks` y `Security` **no** referencian `Persistence`; dependen de las interfaces de `Application.Interface`.

- **Tareas de fondo**: son singleton y los repositorios scoped, así que abren un scope por vuelta (`CreateAsyncScope`) y piden el repositorio por interfaz.
- **`RainSensorMonitor`**: en Python modificaba las entidades devueltas por el repositorio y hacía `session.commit()`, lo que en C# exigiría el `ApplicationDbContext`. Se movió a `IRainSensorRepository.ExpireRainStatus(expiryLimit)`.
- **Lifetimes**:
  - Singleton todo lo que no depende de nada scoped: `MqttService` comparte la conexión con el broker y el resto no guarda estado por petición. Las limpiezas «en X tiempo» de `RoombaService` van en un `Task.Run`, que no depende del lifetime del servicio.
  - Scoped `LightService`, que usa `ILightRepository`, y `AlexaService`, que usa `LightService`.

## Consecuencias

- Una dependencia nueva de infraestructura se pide por interfaz. No inyectar `ApplicationDbContext` fuera de `Persistence`.
- En Development, `builder.Build()` valida el grafo completo: registrar algo cuyas dependencias no están registradas impide arrancar. Los controladores no se validan ahí; fallan en la petición.
