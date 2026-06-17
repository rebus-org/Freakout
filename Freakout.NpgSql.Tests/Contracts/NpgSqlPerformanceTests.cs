using Freakout.Tests.Contracts;
using NUnit.Framework;

namespace Freakout.NpgSql.Tests.Contracts;

[TestFixture]
public class NpgSqlPerformanceTests : PerformanceTests<NpgsqlFreakoutSystemFactory>;