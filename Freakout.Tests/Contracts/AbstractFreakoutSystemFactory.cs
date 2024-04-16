using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.Disposables;

namespace Freakout.Tests.Contracts;

public abstract class AbstractFreakoutSystemFactory : IFreakoutSystemFactory
{
    protected readonly CollectionDisposable disposables = new();

    public FreakoutSystem Create(IServiceCollection services = null)
    {
        services ??= new ServiceCollection();

        ConfigureServices(services);

        services.AddLogging(builder => builder.AddConsole());

        var provider = services.BuildServiceProvider();

        disposables.Add(provider);

        return GetFreakoutSystem(provider);
    }

    protected abstract FreakoutSystem GetFreakoutSystem(ServiceProvider provider);

    protected abstract void ConfigureServices(IServiceCollection services);

    public void Dispose() => disposables.Dispose();
}