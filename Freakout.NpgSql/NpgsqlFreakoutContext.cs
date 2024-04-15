using System.Data.Common;
using Npgsql;

namespace Freakout.NpgSql;

/// <summary>
/// Implementation of <see cref="IFreakoutContext"/> that wraps a
/// <see cref="DbConnection"/> and a <see cref="DbTransaction"/>
/// </summary>
public class NpgsqlFreakoutContext : IFreakoutContext
{
    /// <summary>
    /// Gets the current <see cref="NpgsqlConnection"/>
    /// </summary>
    public NpgsqlConnection Connection { get; }

    /// <summary>
    /// Gets the current <see cref="NpgsqlTransaction"/>
    /// </summary>
    public NpgsqlTransaction Transaction { get; }

    /// <summary>
    /// Creates the context and sets it up to pass the given <paramref name="connection"/> and <paramref name="transaction"/>
    /// to the <see cref="IOutbox"/> implementation
    /// </summary>
    public NpgsqlFreakoutContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}