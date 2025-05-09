using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Serialization;
using Marten;
using SequentialGuid;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Marten;

public static class MartenOutboxExtensions
{
    static readonly SystemTextJsonCommandSerializer FreakoutCommandSerializer = new();

    public static IOutbox GetOutbox(this IDocumentSession session)
    {
        return new MartenOutboxWrapper(session, FreakoutCommandSerializer);
    }

    class MartenOutboxWrapper(IDocumentSession session, ICommandSerializer commandSerializer) : IOutbox
    {
        const string sql = """
                           INSERT INTO "public"."outbox_commands" ("id", "created_at", "headers", "payload") VALUES (?, CURRENT_TIMESTAMP, ?::jsonb, ?);
                           """;

        public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            AddOutboxCommand(command, headers, cancellationToken);
        }

        public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var (headersToUse, payload) = commandSerializer.Serialize(command);

            if (headers != null)
            {
                InsertInto(headers, headersToUse);
            }

            var serializedHeaders = HeaderSerializer.SerializeToString(headersToUse);

            session.QueueSqlCommand(sql, SequentialGuidGenerator.Instance.NewGuid(), serializedHeaders, payload);
        }

        static void InsertInto(Dictionary<string, string> source, Dictionary<string, string> target)
        {
            foreach (var kvp in source)
            {
                target[kvp.Key] = kvp.Value;
            }
        }
    }
}
