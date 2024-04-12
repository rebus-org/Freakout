using System;
using System.Threading.Tasks;

namespace Freakout.Tests.Contracts;

public interface IFreakoutSystemFactory : IDisposable
 {
    Task<FreakoutSystem> CreateAsync();
}

public record FreakoutSystem(
    IOutbox Outbox,
    IOutboxCommandStore OutboxCommandStore
);