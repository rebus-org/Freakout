using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Main Freakout outbox command adder.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Adds a single outbox command to the outbox command store
    /// </summary>
    void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single outbox command to the outbox command store
    /// </summary>
    Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);
}