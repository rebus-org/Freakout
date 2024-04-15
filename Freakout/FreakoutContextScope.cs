using System;
using Freakout.Internals;

namespace Freakout;

/// <summary>
/// Disposable scope struct that helps with managing an ambient Freakout context (i.e. an implementation of <see cref="IFreakoutContext"/> that comes with the chosen form of persistence).
/// It may be used like this:
/// <example>
/// <code>
/// var context = (...);
/// 
/// using (new FreakoutContextScope(context))
/// {
///     // in here IOutbox will work and enlist
///     // its outbox commands in the ambient context
/// }
/// </code>
/// </example>
/// One possible use is within an ASP.NET Core middleware that manages an SqlConnection/SqlTransaction pair, this way enabling that
/// they get passed to the IOutbox implementation that comes with Freakout.MsSql.
/// </summary>
public readonly struct FreakoutContextScope : IDisposable
{
    readonly IFreakoutContext _previous = AsyncLocalFreakoutContextAccessor.Instance.Value;

    /// <summary>
    /// Creates the scope and establishes <paramref name="context"/> as the current ambient Freakout context. Any existing context will be remembered and restored when the scope is disposed.
    /// </summary>
    public FreakoutContextScope(IFreakoutContext context) => AsyncLocalFreakoutContextAccessor.Instance.Value = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Removes the ambient context again and restores the previous scope.
    /// </summary>
    public void Dispose() => AsyncLocalFreakoutContextAccessor.Instance.Value = _previous;
}