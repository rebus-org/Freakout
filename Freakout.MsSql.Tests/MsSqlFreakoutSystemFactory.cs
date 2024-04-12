using Freakout.Config;
using Freakout.Tests.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Nito.Disposables;
using Testy.General;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.MsSql.Tests;

public class MsSqlFreakoutSystemFactory : IFreakoutSystemFactory
{
    readonly CollectionDisposable disposables = new();

    public async Task<FreakoutSystem> CreateAsync()
    {
        var tableName = $"outbox-{Guid.NewGuid():N}";

        disposables.Add(new DisposableCallback(() => MsSqlTestHelper.DropTable(tableName)));

        var services = new ServiceCollection();

        var configuration = new MsSqlFreakoutConfiguration(MsSqlTestHelper.ConnectionString)
        {
            TableName = tableName
        };

        services.AddFreakout(configuration);

        var provider = services.BuildServiceProvider();

        disposables.Add(provider);

        var outbox = provider.GetRequiredService<IOutbox>();
        var commandStore = provider.GetRequiredService<IOutboxCommandStore>();

        return new FreakoutSystem(provider, outbox, commandStore);
    }

    public void Dispose() => disposables.Dispose();
}