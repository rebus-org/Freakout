using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Freakout.Internals;

static class InternalExtensions
{
    public static ILogger<T> GetLoggerFor<T>(this IServiceProvider serviceProvider) => serviceProvider.GetLoggerFactory().CreateLogger<T>();

    public static ILoggerFactory GetLoggerFactory(this IServiceProvider serviceProvider) => serviceProvider.GetService<ILoggerFactory>() ?? new NullLoggerFactory();

    public static string GetValueOrThrow(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (key == null) throw new ArgumentNullException(nameof(key));

        return dictionary.TryGetValue(key, out var result)
            ? result
            : throw new KeyNotFoundException($"Could not find element with key '{key}' among these: {string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"))}");
    }
}