using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Freakout command handler marker interface. Used to enable a little bit of nudging via generics.
/// </summary>
public interface ICommandHandler { }

/// <summary>
/// Freakout command handler interface. Classes that implement this one or more times can be registered
/// as command handlers and will get to handle commands.
/// </summary>
public interface ICommandHandler<in TCommand> : ICommandHandler
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}