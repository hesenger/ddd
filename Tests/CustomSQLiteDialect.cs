using System.Data.Common;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;

namespace Tests;

public class CustomSQLiteDialect : SQLiteDialect
{
    public override IDataBaseSchema GetDataBaseSchema(DbConnection connection)
    {
        return new CustomSQLiteSchema(connection, this);
    }
}
