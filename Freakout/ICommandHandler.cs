using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

public interface ICommandHandler<in TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}