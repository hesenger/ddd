using Domain;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate.Collection.Generic;
using NHibernate.Tool.hbm2ddl;

namespace Tests;

public class Tests
{
    [Test]
    public void Mappings()
    {
        var cfg = Fluently.Configure()
                .Database(MsSqliteConfiguration.Standard
                    .Dialect<CustomSQLiteDialect>()
                    .InMemory()
                    .ShowSql()
                    .FormatSql())
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Tests>())
                .BuildConfiguration();

        var sessionFactory = cfg.BuildSessionFactory();

        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        new SchemaExport(cfg).Create(false, true, connection);

        using var session = sessionFactory
            .WithOptions()
            .Connection(connection)
            .OpenSession();

        var doc = new Documento(1, DateTime.Today, SituacaoDocumento.AguardandoFaturamento);
        session.Save(doc);

        session.Close();

        Assert.That(doc.Id, Is.GreaterThan(0));
    }
}

public class DocumentoMap : ClassMap<Documento>
{
    public DocumentoMap()
    {
        Not.LazyLoad();

        Id(x => x.Id).GeneratedBy.Identity();
        Map(x => x.UCId);
        Map(x => x.DataVencimento);
        Map(x => x.Situacao);
        HasMany(x => x.Eventos)
            .KeyColumn("DocumentoId")
            .Inverse()
            .Cascade.AllDeleteOrphan();
    }
}

public class EventoMap : ClassMap<Evento>
{
    public EventoMap()
    {
        Not.LazyLoad();

        Id(x => x.Id).GeneratedBy.Identity();
        Map(x => x.DocumentoId);
        Map(x => x.Historico);
        Map(x => x.Valor);
    }
}