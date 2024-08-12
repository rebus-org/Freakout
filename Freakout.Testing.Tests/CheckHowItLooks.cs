using Freakout.Config;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;

namespace Freakout.Testing.Tests;

[TestFixture]
public class CheckHowItLooks : FixtureBase
{
    [Test]
    public async Task ThisIsHowItShouldLook()
    {
        var services = new ServiceCollection();

        services.AddFreakout(new InMemFreakoutConfiguration());

        await using var provider = services.BuildServiceProvider();
    }
}