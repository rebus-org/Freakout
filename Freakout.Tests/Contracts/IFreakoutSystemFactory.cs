using System;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Tests.Contracts;

public interface IFreakoutSystemFactory : IDisposable
{
    FreakoutSystem Create(IServiceCollection services = null);
}