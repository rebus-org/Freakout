using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Testy;

namespace Freakout.Tests.Contracts;

public abstract class OutboxCommandStoreTests<TFreakoutSystemFactory> : FixtureBase where TFreakoutSystemFactory : IFreakoutSystemFactory, new()
{
    FreakoutSystem _system;
    IOutboxCommandStore _store;
    ICommandSerializer _serializer;

    protected override void SetUp()
    {
        base.SetUp();

        var factory = Using(new TFreakoutSystemFactory());

        _system = factory.Create();
        _store = _system.OutboxCommandStore;
        _serializer = _system.CommandSerializer;
    }

    [Test]
    public async Task CanGetEmptyBatch()
    {
        using var batch = await _store.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 100);

        await batch.CompleteAsync();

        Assert.That(batch.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task CanRoundtripCommand()
    {
        await AppendCommand(new LittleBittleCommand("hej"));

        using var batch = await _store.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 100);

        Assert.That(batch.Count(), Is.EqualTo(1));

        var commandObject = _serializer.Deserialize(batch.First());

        Assert.That(commandObject, Is.TypeOf<LittleBittleCommand>());

        var command = (LittleBittleCommand)commandObject;

        Assert.That(command.Text, Is.EqualTo("hej"));
    }

    [Test]
    public async Task OnlyCompletesSuccessfulCommands()
    {
        // append two commands
        await AppendCommand(new LittleBittleCommand("MUST SUCCEED"), new() { ["special"] = "must succeed" });
        await AppendCommand(new LittleBittleCommand("MUST FAIL"), new() { ["special"] = "must fail" });

        // get the batch
        using var batch1 = await _store.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 100);

        foreach (var cmd in batch1)
        {
            if (!cmd.Headers.TryGetValue("special", out var special)) continue;

            // find special mark and mark the commands accordingly
            switch (special)
            {
                case "must succeed": cmd.SetState(new SuccessfullyExecutedCommandState(TimeSpan.FromSeconds(1))); break;
                case "must fail": cmd.SetState(new FailedCommandState(TimeSpan.FromSeconds(1), new ArgumentException("blah"))); break;
            }
        }

        await batch1.CompleteAsync();

        // get another batch - should only contain the command marked as failed before
        using var batch2 = await _store.GetPendingOutboxCommandsAsync(commandProcessingBatchSize: 100);

        Assert.That(batch2.Count(), Is.EqualTo(1));

        var commandObject = _serializer.Deserialize(batch2.First());
        Assert.That(commandObject, Is.TypeOf<LittleBittleCommand>());
        var command = (LittleBittleCommand)commandObject;
        Assert.That(command.Text, Is.EqualTo("MUST FAIL"));

    }

    record LittleBittleCommand(string Text);

    async Task AppendCommand(object command, Dictionary<string, string> headers = null)
    {
        using var scope = _system.CreateScope();
        await _system.Outbox.AddOutboxCommandAsync(command, headers: headers);
        scope.Complete();
    }
}