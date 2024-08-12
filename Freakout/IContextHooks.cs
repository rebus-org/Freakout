namespace Freakout;

/// <summary>
/// Can be implemented by <see cref="IFreakoutContext"/> to receive calls after being mounted as the ambient context
/// and after being unmounted again.
/// </summary>
public interface IContextHooks : IFreakoutContext
{
    /// <summary>
    /// Called after the context has been mounted as the ambient context by <see cref="FreakoutContextScope"/>
    /// </summary>
    void Mounted();

    /// <summary>
    /// Called after the context has been unmounted again by <see cref="FreakoutContextScope"/>
    /// </summary>
    void Unmounted();
}