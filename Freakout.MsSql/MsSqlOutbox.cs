using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
// ReSharper disable UseAwaitUsing

namespace Freakout.MsSql;

/// <summary>
/// This one should probably be changed into one that does not manage its own connection/transaction
/// </summary>
class MsSqlOutbox(string connectionString) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        transaction.AddOutboxCommand(command, headers);
        transaction.Commit();
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await transaction.AddOutboxCommandAsync(command, headers, cancellationToken);
        transaction.Commit();
    }
}