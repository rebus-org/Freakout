﻿using System;
using System.Data.Common;

namespace Freakout.MsSql;

/// <summary>
/// Implementation of <see cref="IFreakoutContext"/> that wraps a
/// <see cref="DbConnection"/> and a <see cref="DbTransaction"/>
/// </summary>
public class MsSqlFreakoutContext : IFreakoutContext
{
    /// <summary>
    /// Gets the current <see cref="DbConnection"/>
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// Gets the current <see cref="DbTransaction"/>
    /// </summary>
    public DbTransaction Transaction { get; }

    /// <summary>
    /// Creates the context and sets it up to pass the given <paramref name="connection"/> and <paramref name="transaction"/>
    /// to the <see cref="IOutbox"/> implementation
    /// </summary>
    public MsSqlFreakoutContext(DbConnection connection, DbTransaction transaction)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}