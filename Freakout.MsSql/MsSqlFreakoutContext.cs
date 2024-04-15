using System.Data.Common;

namespace Freakout.MsSql;

/// <summary>
/// Implementation of <see cref="IFreakoutContext"/> that wraps a
/// <see cref="DbConnection"/> and a <see cref="DbTransaction"/>
/// </summary>
public class MsSqlFreakoutContext : IFreakoutContext
{
    public DbConnection Connection { get; }
    public DbTransaction Transaction { get; }

    public MsSqlFreakoutContext(DbConnection connection, DbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }
}