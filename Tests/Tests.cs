using System.Data.Common;
using Domain;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
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

        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            var posicao = new PosicaoDebito(1);
            session.Save(posicao);
        });

        var doc = new Documento(1, DateTime.Today, SituacaoDocumento.Pendente);
        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            session.Save(doc);
            session.Save(new Documento(1, DateTime.Today, SituacaoDocumento.Baixado));
            session.Save(new Documento(1, DateTime.Today, SituacaoDocumento.Baixado));
            doc.Eventos.Add(new Evento("Teste", 22.5m));
        });

        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            var doc2 = session.Get<Documento>(doc.Id);
            var qry = (IQueryable<Evento>)doc2.Eventos; // lazy loaded
            Assert.That(qry.Where(e => e.Valor > 0).ToList(), Has.Count.EqualTo(1));
        });

        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            var posicao = session.Get<PosicaoDebito>(1);
            Assert.That(posicao.DocumentosBaixados, Has.Count.EqualTo(2));

            var novo = new Documento(1, DateTime.Today, SituacaoDocumento.Pendente);
            posicao.EventosFuturos.ToList().ForEach(novo.Eventos.Add);
            posicao.DocumentosPendentes.Add(novo);
        });

        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            var posicao = session.Get<PosicaoDebito>(1);
            Assert.That(posicao.DocumentosPendentes, Has.Count.EqualTo(2));
        });

        ExecuteTransactionally(sessionFactory, connection, session =>
        {
            var posicao = session.Get<PosicaoDebito>(1);
            Assert.That(posicao.EventosFuturos, Has.Count.EqualTo(0));
        });
    }

    private static void ExecuteTransactionally(
        ISessionFactory sessionFactory,
        DbConnection connection,
        Action<ISession> action)
    {
        using var session = sessionFactory
            .WithOptions()
            .Connection(connection)
            .OpenSession();
        using var transaction = session.BeginTransaction();
        action(session);
        transaction.Commit();
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
        Map(x => x.Situacao).CustomType<SituacaoDocumento>();
        HasMany(x => x.Eventos)
            .KeyColumn("DocumentoId")
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

public class PosicaoDebitoMap : ClassMap<PosicaoDebito>
{
    public PosicaoDebitoMap()
    {
        Not.LazyLoad();

        Id(x => x.Id);

        HasMany(x => x.EventosFuturos)
            .KeyColumn("UCId")
            .Cascade.AllDeleteOrphan();

        HasMany(x => x.DocumentosPendentes)
            .KeyColumn("UCId")
            .Where("Situacao < 4")
            .Cascade.AllDeleteOrphan();

        HasMany(x => x.DocumentosBaixados)
            .KeyColumn("UCId")
            .Where("Situacao = 4")
            .Cascade.AllDeleteOrphan();
    }
}