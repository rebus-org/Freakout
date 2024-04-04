using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Freakout.Internals;

static class InternalExtensions
{
    public static ILogger<T> GetLoggerFor<T>(this IServiceProvider serviceProvider) => serviceProvider.GetLoggerFactory().CreateLogger<T>();

    public static ILoggerFactory GetLoggerFactory(this IServiceProvider serviceProvider) => serviceProvider.GetService<ILoggerFactory>() ?? new NullLoggerFactory();

}