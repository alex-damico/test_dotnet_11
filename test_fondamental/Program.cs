namespace test_fondamental;

// =========================================================================
// RIEPILOGO PER COLLOQUIO: OOP, TIPI C# (class/record/struct) E PRINCIPI SOLID
// =========================================================================
// PARTE 1 - I 4 PILASTRI DELL'OOP:
//   1. Incapsulamento: nascondere e proteggere lo stato interno (campi 'private', proprietà 'private set').
//   2. Astrazione:     esporre solo i concetti essenziali tramite 'abstract class' e 'interface'.
//   3. Ereditarietà:   riusare ed estendere una classe base ('BaseTicket' -> 'IncidentTicket').
//   4. Polimorfismo:   uno stesso metodo ('CalcolaSlaOre()') si comporta in modo diverso a seconda
//                      del tipo concreto dell'oggetto a runtime ('virtual' / 'override' / 'abstract').
//
// PARTE 2 - DIFFERENZA TRA class, record E struct (Domanda frequentissima!):
//   - class  (Reference Type - Heap): copiando la variabile copi il RIFERIMENTO; '==' confronta gli indirizzi di memoria.
//   - record (Reference Type - Heap): pensato per dati immutabili (DTO); '==' confronta i VALORI delle proprietà!
//   - struct (Value Type - Stack):    per piccoli dati leggeri (es. Coordinate, DateTime); copiando la variabile copi il VALORE.
//
// PARTE 3 - I 5 PRINCIPI S.O.L.I.D.:
//   - [S] Single Responsibility: una classe deve avere un'unica responsabilità (un solo motivo per cambiare).
//   - [O] Open/Closed:           aperta alle estensioni (nuove classi), chiusa alle modifiche (senza toccare gli if/switch esistenti).
//   - [L] Liskov Substitution:   una classe figlia deve poter sostituire la classe padre senza alterare la correttezza del programma.
//   - [I] Interface Segregation: meglio più interfacce piccole e mirate che un'unica interfaccia gigante.
//   - [D] Dependency Inversion:  dipendere sempre da astrazioni (interfacce) e mai da classi concrete.
// =========================================================================

class Program
{
    static void Main(string[] args)
    {
        // =====================================================================
        // SEZIONE 1: I 4 PILASTRI DELL'OOP IN AZIONE
        // =====================================================================
        Console.WriteLine("=== 1. OOP: INCAPSULAMENTO, EREDITARIETÀ, ASTRAZIONE E POLIMORFISMO ===");

        // Creiamo due sottoclassi diverse ma le inseriamo in una lista del tipo astratto comune (BaseTicket)
        List<BaseTicket> tickets =
        [
            new IncidentTicket("INC-100", "Blocco display stazione centrale", gravitaCritica: true),
            new IncidentTicket("INC-101", "Led lampeggiante corsia 2", gravitaCritica: false),
            new ServiceRequestTicket("REQ-200", "Installazione nuovo pannello informativo")
        ];

        // POLIMORFISMO: scorriamo la lista come 'BaseTicket', ma a runtime ogni oggetto esegue
        // la SUA versione di CalcolaSlaOre() e StampaDettaglio()!
        foreach (BaseTicket ticket in tickets)
        {
            Console.WriteLine($" -> {ticket.StampaDettaglio()} | SLA massima: {ticket.CalcolaSlaOre()} ore");
        }

        // INCAPSULAMENTO: non possiamo fare 'ticket.IsClosed = true' dall'esterno (è private set!),
        // dobbiamo passare dal metodo pubblico Chiudi() che protegge le regole di business.
        tickets[0].Chiudi();
        Console.WriteLine($"Stato di {tickets[0].Code} dopo Chiudi(): Chiuso={tickets[0].IsClosed}\n");

        // =====================================================================
        // SEZIONE 2: class vs record vs struct (CONFRONTO PRATICO)
        // =====================================================================
        Console.WriteLine("=== 2. CONFRONTO TRA class, record E struct ===");

        // A) CLASS: due istanze con gli stessi identici dati hanno indirizzi diversi in memoria -> '==' è FALSE
        var class1 = new ClienteClass { Nome = "Mario", Ruolo = "Admin" };
        var class2 = new ClienteClass { Nome = "Mario", Ruolo = "Admin" };
        Console.WriteLine($"[class]  class1 == class2 (stessi dati): {class1 == class2} (confronta l'indirizzo in memoria!)");

        // B) RECORD: confronta automaticamente il VALORE di tutte le proprietà -> '==' è TRUE
        var record1 = new ClienteRecord("Mario", "Admin");
        var record2 = new ClienteRecord("Mario", "Admin");
        Console.WriteLine($"[record] record1 == record2 (stessi dati): {record1 == record2} (confronta i valori!)");

        // I record supportano anche la non-destructive mutation con la keyword 'with' (crea una copia modificata):
        var recordPromosso = record1 with { Ruolo = "SuperAdmin" };
        Console.WriteLine($"[record con 'with'] Originale: {record1.Ruolo} | Nuovo: {recordPromosso.Ruolo}");

        // C) STRUCT (Value Type): copiando la variabile si crea una copia indipendente sullo Stack
        var punto1 = new CoordinateStruct(10, 20);
        var punto2 = punto1; // Copia per VALORE!
        punto2.X = 99;       // Modificare punto2 NON modifica punto1!
        Console.WriteLine($"[struct] punto1.X = {punto1.X} | punto2.X = {punto2.X} (copia per valore indipendente!)\n");

        // =====================================================================
        // SEZIONE 3: I PRINCIPI SOLID APPLICATI
        // =====================================================================
        Console.WriteLine("=== 3. PRINCIPI S.O.L.I.D. IN PRATICA ===");

        // Usiamo la Dependency Inversion [D] e Open/Closed [O]:
        // Possiamo passare a TicketAlertManager qualsiasi implementazione di INotifier
        // (EmailNotifier, SmsNotifier, ecc.) senza mai modificare la classe TicketAlertManager!
        List<INotifier> canaliNotifica =
        [
            new EmailNotifier(),
            new SmsNotifier()
        ];

        var alertManager = new TicketAlertManager(canaliNotifica);
        alertManager.InviaAvvisoUrgente("INC-100: Blocco display stazione centrale");
    }
}

// =========================================================================
// PARTE 1: CLASSI PER ESEMPIO OOP (abstract class vs interface)
// =========================================================================
// DOMANDA DA COLLOQUIO: "Qual è la differenza tra Classe Astratta e Interfaccia?"
// - ABSTRACT CLASS (BaseTicket):
//   * Definisce cosa un oggetto "È" (relazione di identità: IncidentTicket È UN BaseTicket).
//   * Può avere campi, stato (proprietà valorizzate), costruttori e codice già condiviso.
//   * Una classe in C# può ereditare da UNA SOLA classe astratta (ereditarietà singola).
// - INTERFACE (IAuditable):
//   * Definisce cosa un oggetto "SA FARE" (contratto di comportamento).
//   * Non ha stato (campi istanza) né costruttori.
//   * Una classe può implementare INFINITE interfacce! Fondamentale per Dependency Injection e Unit Test.
// =========================================================================

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
}

public abstract class BaseTicket : IAuditable
{
    public string Code { get; }
    public string Title { get; }

    // INCAPSULAMENTO: 'private set' impedisce modifiche arbitrarie dall'esterno
    public bool IsClosed { get; private set; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    protected BaseTicket(string code, string title)
    {
        Code = code;
        Title = title;
    }

    public void Chiudi()
    {
        IsClosed = true;
    }

    // METODO ASTRATTO: non ha corpo qui; OBBLIGA ogni sottoclasse a scrivere la propria implementazione (override)
    public abstract int CalcolaSlaOre();

    // METODO VIRTUALE: ha già un comportamento di base, ma le sottoclassi POSSONO ridefinirlo con 'override'
    public virtual string StampaDettaglio() => $"[{Code}] {Title}";
}

// EREDITARIETÀ: IncidentTicket eredita da BaseTicket
public class IncidentTicket : BaseTicket
{
    public bool GravitaCritica { get; }

    public IncidentTicket(string code, string title, bool gravitaCritica)
        : base(code, title) // Chiama il costruttore della classe padre BaseTicket
    {
        GravitaCritica = gravitaCritica;
    }

    // POLIMORFISMO (override del metodo abstract): un incidente critico ha SLA di 2 ore, altrimenti 8 ore
    public override int CalcolaSlaOre() => GravitaCritica ? 2 : 8;

    // POLIMORFISMO (override del metodo virtual): aggiunge l'indicazione di criticità
    public override string StampaDettaglio() =>
        $"{base.StampaDettaglio()} (Tipo: INCIDENTE - Critico: {GravitaCritica})";
}

// EREDITARIETÀ: ServiceRequestTicket eredita da BaseTicket
public class ServiceRequestTicket : BaseTicket
{
    public ServiceRequestTicket(string code, string title)
        : base(code, title)
    {
    }

    // POLIMORFISMO: una richiesta di servizio standard ha SLA di 24 ore
    public override int CalcolaSlaOre() => 24;

    public override string StampaDettaglio() =>
        $"{base.StampaDettaglio()} (Tipo: RICHIESTA SERVIZIO)";
}

// =========================================================================
// PARTE 2: TIPI PER CONFRONTO class vs record vs struct
// =========================================================================

public class ClienteClass
{
    public string Nome { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
}

public record ClienteRecord(string Nome, string Ruolo);

public struct CoordinateStruct(int x, int y)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
}

// =========================================================================
// PARTE 3: ESEMPI PRATICI DEI 5 PRINCIPI S.O.L.I.D.
// =========================================================================

// [I] INTERFACE SEGREGATION PRINCIPLE (ISP):
// Invece di un'unica interfaccia enorme IRepository con 20 metodi (anche per chi deve solo leggere),
// separiamo il contratto di sola lettura da quello di scrittura.
public interface IReadOnlyTicketRepository
{
    BaseTicket? GetByCode(string code);
}

public interface IWriteTicketRepository
{
    void Save(BaseTicket ticket);
}

// [O] OPEN/CLOSED PRINCIPLE (OCP) + [L] LISKOV SUBSTITUTION PRINCIPLE (LSP):
// Creiamo l'astrazione INotifier. Se domani l'azienda vuole inviare notifiche anche su Microsoft Teams
// o Telegram, ci basterà creare una nuova classe 'TeamsNotifier : INotifier' SENZA toccare il codice esistente!
public interface INotifier
{
    void Send(string message);
}

// [S] SINGLE RESPONSIBILITY PRINCIPLE (SRP):
// EmailNotifier ha UNA SOLA responsabilità: inviare notifiche via Email.
public class EmailNotifier : INotifier
{
    public void Send(string message) =>
        Console.WriteLine($" [EmailNotifier] Invio email: '{message}'");
}

// [S] SINGLE RESPONSIBILITY PRINCIPLE (SRP):
// SmsNotifier ha UNA SOLA responsabilità: inviare notifiche via SMS.
public class SmsNotifier : INotifier
{
    public void Send(string message) =>
        Console.WriteLine($" [SmsNotifier]   Invio SMS:   '{message}'");
}

// [D] DEPENDENCY INVERSION PRINCIPLE (DIP):
// TicketAlertManager (modulo di alto livello) NON dipende dalle classi concrete EmailNotifier o SmsNotifier
// (non fa mai 'new EmailNotifier()'), ma dipende solo dall'astrazione IEnumerable<INotifier> ricevuta nel costruttore!
public class TicketAlertManager
{
    private readonly IEnumerable<INotifier> _notifiers;

    public TicketAlertManager(IEnumerable<INotifier> notifiers)
    {
        _notifiers = notifiers;
    }

    public void InviaAvvisoUrgente(string testoAvviso)
    {
        foreach (INotifier notifier in _notifiers)
        {
            notifier.Send(testoAvviso);
        }
    }
}
