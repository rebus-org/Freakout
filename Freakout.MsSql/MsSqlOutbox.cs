using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Freakout.MsSql;

class MsSqlOutbox(string connectionString, string tableName, string schemaName) : IOutbox
{
    public async Task<IReadOnlyList<OutboxTask>> GetPendingOutboxTasksAsync(CancellationToken cancellationToken = default)
    {
        return new List<OutboxTask>();
    }

    public void CreateSchema()
    {
        using var connection = new SqlConnection(connectionString);
        
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"

IF NOT EXISTS (SELECT TOP 1 * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = '{tableName}' AND s.name = '{schemaName}') 
BEGIN
    CREATE TABLE [{schemaName}].[{tableName}] (
        [Id] UNIQUEIDENTIFIER,
        [Time] DATETIMEOFFSET(3) NOT NULL,
        [Headers] NVARCHAR(MAX),
        [Payload] VARBINARY(MAX),
        [Completed] BIT NOT NULL DEFAULT(0),

        PRIMARY KEY ([Id])
    )
END
";

        command.ExecuteNonQuery();
    }
}