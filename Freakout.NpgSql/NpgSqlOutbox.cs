using Npgsql;

namespace Freakout.NpgSql;

public class NpgSqlOutbox(string connectionString) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        transaction.AddOutboxCommand(command, headers);
        transaction.Commit();
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await transaction.AddOutboxCommandAsync(command, headers, cancellationToken);
        transaction.Commit();
    }
}