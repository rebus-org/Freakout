using Freakout.Tests.Contracts;
using NUnit.Framework;

namespace Freakout.MsSql.Tests.Contracts;

[TestFixture]
public class MsSqlOutboxCommandStoreTests : OutboxCommandStoreTests<MsSqlFreakoutSystemFactory>;