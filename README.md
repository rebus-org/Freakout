# Freakout

📤 Just a general outbox thing


## What do you mean by "outbox"?

An outbox is a simple mechanism that allows you to store outbound messages in an atomic ☢️ way as part of your normal transaction.

It addresses the ubiquitous dilemma of which to do first: (1) commit your database transaction OR (2) whatever else you want to do.

The problem is that (1) is generally inherently risky (database deadlocks, key violations, etc etc), and (2) is also generally risky (writing to a file share, communicating with a message broker, calling an external web service, etc.).

Two risky things combined are very likely to fail! - which will leave your system in a state where either (1) or (2) was not carried out (depending on the order you chose to execute them in), because it failed.

🆘 This problem can be addressed in many ways, e.g. by retrying, using a middleman with higher availability (e.g. a message broker), etc. etc.

OR you could simply store information about which actions to perform in (2) as part of (1)!! 🤯 That's what the outbox does! 🤓


## Until now, I have only seen outboxes built into messaging libraries - why now a separate library?

Why? Because "outbox" is closer to your chosen type of persistence (SQL Server, Postgres, etc.) than to anything else.


## Which types of persistence does it support?

1. Microsoft SQL Server (for when you're working with the "Microsoft.Data.SqlClient" NuGet package and `SqlConnection`/`SqlTransaction`)
1. PostgreSQL (for when you're working with "Npgsql" NuGet package and `NpgsqlConnection`/`NpgsqlTransaction`)

and that's it for now. 😅


## How?

First, pull in the relevant NuGet package for your chosen type of persistence - e.g. the "Freakout.MsSql" NuGet package.

Next, enable Freakout in your app:

```csharp
services.AddFreakout(new MsSqlFreakoutConfiguration(connectionString));
```

It will register a couple of things, e.g. a background worker that will poll the outbox for pending commands. `AddFreakout` can only be called once.

Then, add your handlers:

```csharp
services.AddCommandHandler<RecalculateClaimSummaryCommandHandler>();
services.AddCommandHandler<SendEmailCommandHandler>();
services.AddCommandHandler<PublishJournalEntryAddedCommandHandler>();
```

which will of course be resolved from the container, each in their own service scope.

Now it's fully configured - what's missing is putting something in the outbox.

## Two ways of adding commands to the outbox

### First way: Directly on the database transaction

Since this example is for SQL Server, and we're pretending to be working with `Microsoft.Data.SqlClient` and `SqlConnection`, it's natural to
provide the outbox functionality as an extension method to `DbTransaction`. This way, your code can do stuff like this:

```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();

await using var transaction = await connection.BeginTransactionAsync();

// do your own work with connection+transaction here
// (...)

// possibly call this bad boy a couple of times
await transaction.AddOutboxCommandAsync(new PublishJournalEntryAddedCommand(Id: journalEntryId));

// do more of your own work
// (...)

// commit it all atomically
await transaction.CommitAsync();
```

which in this case would result in publishing a couple of `JournalEntryAdded` events using Rebus.


### Second way: By using `IOutbox`

This is a neat way to do it: You can manage your unit of work with your `SqlConnection` and `SqlTransaction` somewhere
and then make them available to Freakout by using a `FreakoutContextScope` like this:

```csharp
var context = new MsSqlFreakoutContext(connection, transaction);

using (new FreakoutContextScope(context))
{
	// there's an ambient context now! 🙂
}
```

Inside the scope, `IOutbox` can be resolved, which then provides a technology-agnostic way of putting commands in the outbox!

A cool place to create/dispose `FreakoutContextScope` would be in your ASP.NET Core request handler pipeline, e.g. like this:

```csharp
app.Use(async (context, next) => {
	var provider = context.Request.RequestServices;
	
	// let's just assume we can get these from the request-scoped services:
	var connection = provider.GetRequiredService<SqlConnection>();
	var transaction = provider.GetRequiredService<SqlTransaction>();

	var freakoutContext = new MsSqlFreakoutContext(connection, transaction);

	using (new FreakoutContextScope(freakoutContext))
	{
		// there's an ambient context now! 🙂
		//
		// ASP.NET Core controllers can have IOutbox injected if they want!
		await next();
	}
});

```

Having `IOutbox` injected is pretty neat, because it allows you to put commands in the outbox simply by going:

```csharp
await outbox.AddOutboxCommandAsync(command);
```

without having to bother with thinking about which type of persistence is being used.


## What does a command handler look like?

Outbox commands are dispatched to handlers. Handlers are classes that implement `ICommandHandler<TCommand>` and are registered in the 
container using the `AddCommandHandler` extension method shown above.

A command handler to publish the aforementioned Rebus event could look like this (assuming [Rebus](https://github.com/rebus-org/Rebus) has also been configured in the given container):

```csharp
public class PublishJournalEntryAddedCommandHandler(IBus bus) : ICommandHandler<PublishJournalEntryAddedCommand>
{
	public async Task HandleAsync(PublishJournalEntryAddedCommand command, CancellationToken token)
	{
		await bus.Publish(new JournalEntryAdded(command.Id));
	}
}
```


