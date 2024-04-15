using System;

namespace Freakout;

/// <summary>
/// Accessor that enables getting the current ambient Freakout context (or NULL if there is none)
/// </summary>
public interface IFreakoutContextAccessor
{
    /// <summary>
    /// Returns the current ambient Freakout context of type <typeparamref name="TContext"/>.
    /// If <paramref name="throwIfNull"/> is TRUE, an <see cref="InvalidOperationException"/> is thrown if no context could be found.
    /// If <paramref name="throwIfNull"/> is FALSE and there's no context, NULL is returned.
    /// Throws <see cref="InvalidCastException"/> if there is a context but it is not of type <typeparamref name="TContext"/>.
    /// </summary>
    TContext GetContext<TContext>(bool throwIfNull = true) where TContext : class, IFreakoutContext;
}