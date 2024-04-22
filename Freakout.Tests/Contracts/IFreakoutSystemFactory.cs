using Microsoft.Extensions.DependencyInjection;
using System;

namespace Freakout.Tests.Contracts;

public interface IFreakoutSystemFactory : IDisposable
{
    FreakoutSystem Create(Action<IServiceCollection> before = null, Action<IServiceCollection> after = null);
}