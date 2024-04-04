using Microsoft.Extensions.DependencyInjection;

namespace Freakout.MsSql;

public class MsSqlFreakoutConfiguration(string connectionString) : FreakoutConfiguration
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IOutbox>(p => new MsSqlOutbox(connectionString));
    }
}