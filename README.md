# Freakout

📤 Just a general outbox thing

Why? Because "outbox" is closer to your chosen type of persistence (SQL Server, Postgres, etc.) than to anything else.


## Which types of persistence does it support?

1. Microsoft SQL Server (for when you're working with the "Microsoft.Data.SqlClient" NuGet package and `SqlConnection`/`SqlTransaction`)
1. PostgreSQL (for when you're working with "Npgsql" NuGet package and `NpgsqlConnection`/`NpgsqlTransaction`)

and that's it for now. 😅


## How?

First, enable Freakout in your app:

```csharp
services.AddFreakout(new MsSqlFreakoutConfiguration(connectionString));
```

It will register a couple of things, e.g. a background worker that will poll the outbox for pending commands. `AddFreakout` can only be called once.

Then, add your handlers:

```csharp
services.AddCommandHandler<RecalculateClaimSummaryCommandHandler>();
services.AddCommandHandler<SendEmailCommandHandler>();
services.AddCommandHandler<PublishRebusEventCommandHandler>();
```

which will of course be resolved from the container, each in their own service scope.

Now it's fully configured - what's missing is putting something in the outbox.

Since this example is for SQL Server, and we're pretending to be working with `Microsoft.Data.SqlClient` and `SqlConnection`, it's natural to
provide the outbox functionality as an extension method to `DbTransaction`. This way, your code can do stuff like this:

```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();

await using var transaction = await connection.BeginTransactionAsync();

// do your own work with connection+transaction here
// (...)

// possibly call this bad boy a couple of times
await transaction.AddOutboxCommandAsync(new PublishRebusEventCommand(
	Event: new JournalEntryAdded(Id: journalEntryId)
));

// do more of your own work
// (...)

// commit it all atomically
await transaction.CommitAsync();
```

which in this case would result in publishing a couple of `JournalEntryAdded` events using Rebus.

Btw. a command handler to do this could look like this:

```csharp
public class PublishRebusEventCommandHandler(IBus bus) : ICommandHandler<PublishRebusEventCommand>
{
	public Task HandleAsync(PublishRebusEventCommand command, CancellationToken token) => bus.Publish(command.Event);
}
```

and that would be it.