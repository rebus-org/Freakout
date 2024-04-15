using System;

namespace Freakout;

/// <summary>
/// Accessor that enables getting the current ambient Freakout context (or NULL if there is none)
/// </summary>
public interface IFreakoutContextAccessor
{
    /// <summary>
    /// Returns the current ambient Freakout context of type <typeparamref name="TContext"/> or NULL, if there is none.
    /// Throws <see cref="InvalidCastException"/> if there is a context but it is not of type <typeparamref name="TContext"/>.
    /// </summary>
    TContext GetContext<TContext>() where TContext : class, IFreakoutContext;
}