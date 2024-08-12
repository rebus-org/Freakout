using System.Runtime.Serialization;
using Freakout.Config;
using Freakout.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;
using Testy.Extensions;

namespace Freakout.Testing.Tests;

[TestFixture]
public class CheckHowItLooks : FixtureBase
{
    [Test]
    public async Task ThisIsHowItShouldWOrk_Callback()
    {
        var taskCompletionSource = new TaskCompletionSource<InMemOutboxCommand>();

        var configuration = new InMemFreakoutConfiguration();
        configuration.CommandAdded += cmd => Task.Run(() => taskCompletionSource.SetResult(cmd));

        var services = new ServiceCollection();

        services.AddFreakout(configuration);

        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IOutbox>().AddOutboxCommandAsync(new MyCommand("hello there 🙂"));

        await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ThisIsHowItShouldWOrk_BuiltInQueue()
    {
        var configuration = new InMemFreakoutConfiguration();
        var commands = configuration.Commands;

        var services = new ServiceCollection();

        services.AddFreakout(configuration);

        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IOutbox>().AddOutboxCommandAsync(new MyCommand("hello there 🙂"));

        Assert.That(commands.Count, Is.EqualTo(1));

        var command = commands.First();
        Assert.That(command.Command, Is.TypeOf<MyCommand>());
        Assert.That(command.Command, Is.EqualTo(new MyCommand("hello there 🙂")));
    }

    record MyCommand(string Text);

    [Test]
    public async Task SerializationError_CanBeDetected()
    {
        var services = new ServiceCollection();

        services.AddFreakout(new InMemFreakoutConfiguration());

        await using var provider = services.BuildServiceProvider();

        var exception = Assert.ThrowsAsync<SerializationException>(() =>
            provider.GetRequiredService<IOutbox>().AddOutboxCommandAsync(new CannotBeRoundtripped(123)));

        Console.WriteLine(exception);

        var details = exception!.ToString();

        Assert.That(details, Contains.Substring(nameof(SystemTextJsonCommandSerializer)));
        Assert.That(details, Contains.Substring("Serialization check failed"));
    }

    [Test]
    public async Task SerializationError_SerializationCanBeBypassed()
    {
        var services = new ServiceCollection();

        services.AddFreakout(new InMemFreakoutConfiguration { CheckSerialization = false });

        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IOutbox>().AddOutboxCommandAsync(new CannotBeRoundtripped(123));
    }

    class CannotBeRoundtripped(int wrongType)
    {
        public string WrongType { get; } = wrongType.ToString();
    }
}