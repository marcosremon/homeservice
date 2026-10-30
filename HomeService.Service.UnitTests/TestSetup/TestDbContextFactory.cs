using HomeService.Infraestructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomeService.Service.UnitTests.TestSetup
{
    // ApplicationDbContext sobre el proveedor InMemory: una base nueva por test. Los
    // repositorios abren transacciones, que InMemory no soporta; se ignoran.
    public static class TestDbContextFactory
    {
        #region Create
        public static ApplicationDbContext Create(string? databaseName = null)
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }
        #endregion
    }
}
