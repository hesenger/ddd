namespace Domain;

public class Documento : IEntity
{
    public int Id { get; protected set; }
    public int UCId { get; protected set; }
    public DateTime DataVencimento { get; protected set; }
    public SituacaoDocumento Situacao { get; protected set; }
    public IList<Evento> Eventos { get; protected set; } = new List<Evento>();

    protected Documento() { }

    public Documento(
        int ucId,
        DateTime dataVencimento,
        SituacaoDocumento situacao)
    {
        UCId = ucId;
        DataVencimento = dataVencimento;
        Situacao = situacao;
    }
}

public enum SituacaoDocumento
{
    AguardandoFaturamento = 1,
    Pendente = 2,
    Contestado = 3,
    Baixado = 4
}

public class Evento : IEntity
{
    public int Id { get; protected set; }
    public int UCId { get; protected set; }
    public int? DocumentoId { get; protected set; }
    public string Historico { get; protected set; } = null!;
    public decimal Valor { get; protected set; }

    protected Evento() { }

    public Evento(
        string historico,
        decimal valor)
    {
        Historico = historico;
        Valor = valor;
    }
}

public class PosicaoDebito : AggregateRoot
{
    public int Id { get; protected set; }
    public IList<Evento> EventosFuturos { get; protected set; } = new List<Evento>();
    public IList<Documento> DocumentosPendentes { get; protected set; } = new List<Documento>();
    public IList<Documento> DocumentosBaixados { get; protected set; } = new List<Documento>();

    protected PosicaoDebito() { }

    public PosicaoDebito(
        int ucId)
    {
        Id = ucId;
    }
}

public interface IEntity
{
    int Id { get; }
}

public interface AggregateRoot : IEntity
{
}
