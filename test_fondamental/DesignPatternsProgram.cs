namespace test_fondamental;

// =========================================================================
// RIEPILOGO PER COLLOQUIO: I DESIGN PATTERN PIÙ USATI IN .NET
// =========================================================================
// Si dividono in 3 famiglie (Gang of Four - GoF) + Pattern Architetturali:
// 1. CREAZIONALI (come creo gli oggetti?):
//    - Factory Pattern:   centralizza la logica di creazione di sottoclassi diverse ('TicketFactory.Crea(...)').
//    - Builder Pattern:   costruisce oggetti complessi passo-passo con metodi concatenati (Fluent API, es. WebApplication.CreateBuilder).
//    - Singleton:         un'unica istanza globale (in .NET lo gestisce direttamente la DI con services.AddSingleton!).
// 2. STRUTTURALI (come compongo le classi?):
//    - Decorator Pattern: "avvolge" un oggetto per aggiungere funzionalità (es. Logging, Cache) senza modificare la classe originale.
//    - Adapter / Facade:  adatta un'interfaccia esterna incompatibile o semplifica un sottosistema complesso.
// 3. COMPORTAMENTALI E ARCHITETTURALI (come comunicano gli oggetti?):
//    - Strategy Pattern:  rende intercambiabile un algoritmo a runtime tramite interfaccia (evita lunghi switch/if).
//    - Observer Pattern:  notifica automaticamente più ascoltatori quando succede un evento (event / EventHandler).
//    - Repository Pattern: disaccoppia la logica di business dal database (EF Core / SQL) tramite un'interfaccia.
//
// NOTA: In JetBrains Rider puoi cliccare direttamente l'icona Play verde accanto a 'static void Main'
//       qui sotto per avviare solo questo file!
// =========================================================================

public class DesignPatternsProgram
{
    public static void Main(string[] args)
    {
        if (args.Contains("--oop"))
        {
            Program.Main(args);
            return;
        }

        Console.WriteLine("=== DESIGN PATTERN PIÙ USATI IN .NET ===");

        // -----------------------------------------------------------------
        // 1. FACTORY PATTERN (Creazionale)
        // Il chiamante chiede un ticket passando il codice ("INC-..." o "REQ-...")
        // senza dover sapere quale classe concreta istanziare con 'new'.
        // -----------------------------------------------------------------
        Console.WriteLine("\n--- 1. Factory Pattern ---");
        BaseTicket ticketCreatoDaFactory = TicketFactory.CreaDaCodice("INC-500", "Guasto telecamera galleria");
        Console.WriteLine($"Creato tramite Factory: {ticketCreatoDaFactory.StampaDettaglio()}");

        // -----------------------------------------------------------------
        // 2. BUILDER PATTERN (Creazionale - Fluent Interface)
        // Costruiamo un oggetto complesso passo dopo passo (es. messaggio per un pannello LED stradale),
        // proprio come fa .NET con WebApplication.CreateBuilder() o StringBuilder.
        // -----------------------------------------------------------------
        Console.WriteLine("\n--- 2. Builder Pattern ---");
        DisplayMessage messaggioLed = new DisplayMessageBuilder()
            .ConTesto("CODA TRA USCITA A4 E SERIATE")
            .ConColore("Ambra")
            .ConLampeggio(true)
            .ConDurataSecondi(30)
            .Build();
        Console.WriteLine($"Messaggio costruito col Builder: {messaggioLed}");

        // -----------------------------------------------------------------
        // 3. STRATEGY PATTERN (Comportamentale)
        // Cambiamo a runtime l'algoritmo di assegnazione del tecnico senza usare if/switch nel codice chiamante!
        // -----------------------------------------------------------------
        Console.WriteLine("\n--- 3. Strategy Pattern ---");
        var dispatcherDiurno = new TicketDispatcher(new AssegnazioneRepartoLocaleStrategy());
        dispatcherDiurno.AssegnaTicket(ticketCreatoDaFactory);

        var dispatcherNotturno = new TicketDispatcher(new AssegnazioneReperibilitaNotturnaStrategy());
        dispatcherNotturno.AssegnaTicket(ticketCreatoDaFactory);

        // -----------------------------------------------------------------
        // 4. DECORATOR PATTERN (Strutturale)
        // "Avvolgiamo" EmailNotifier dentro LoggingNotifierDecorator per aggiungere i log prima e dopo l'invio,
        // senza toccare una singola riga di codice di EmailNotifier!
        // -----------------------------------------------------------------
        Console.WriteLine("\n--- 4. Decorator Pattern ---");
        INotifier emailConLog = new LoggingNotifierDecorator(new EmailNotifier());
        emailConLog.Send("Manutenzione programmata server ore 23:00");

        // -----------------------------------------------------------------
        // 5. REPOSITORY PATTERN (Architetturale / Accesso Dati)
        // Il codice di business salva e cerca ticket usando l'interfaccia ITicketRepository,
        // restando completamente indipendente dal fatto che sotto ci sia SQL Server, EF Core o una lista In-Memory.
        // -----------------------------------------------------------------
        Console.WriteLine("\n--- 5. Repository Pattern ---");
        ITicketRepository repository = new InMemoryTicketRepository();
        repository.Save(ticketCreatoDaFactory);
        BaseTicket? trovato = repository.GetByCode("INC-500");
        Console.WriteLine($"Letto dal Repository astratto: {trovato?.StampaDettaglio()}");
    }
}

// =========================================================================
// IMPLEMENTAZIONE DEI DESIGN PATTERN
// =========================================================================

// -------------------------------------------------------------------------
// 1. FACTORY PATTERN (Creazionale)
// Quando usarlo: quando la scelta di quale sottoclasse istanziare dipende da parametri/configurazioni
// e vuoi evitare di duplicare blocchi switch/if con 'new' sparsi per tutta l'applicazione.
// -------------------------------------------------------------------------
public static class TicketFactory
{
    public static BaseTicket CreaDaCodice(string code, string title)
    {
        if (code.StartsWith("INC-", StringComparison.OrdinalIgnoreCase))
        {
            return new IncidentTicket(code, title, gravitaCritica: true);
        }

        if (code.StartsWith("REQ-", StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceRequestTicket(code, title);
        }

        throw new ArgumentException($"Prefisso codice '{code}' non riconosciuto dalla Factory.");
    }
}

// -------------------------------------------------------------------------
// 2. BUILDER PATTERN (Creazionale - Fluent API)
// Quando usarlo: quando un oggetto ha tanti parametri (spesso opzionali) e un costruttore con
// 8-10 parametri ("Telescoping Constructor") sarebbe illeggibile e soggetto a errori.
// Esempi reali in .NET: WebApplication.CreateBuilder(), DbContextOptionsBuilder, StringBuilder, UriBuilder.
// -------------------------------------------------------------------------
public record DisplayMessage(string Testo, string Colore, bool Lampeggiante, int DurataSecondi);

public class DisplayMessageBuilder
{
    private string _testo = "MESSAGGIO VUOTO";
    private string _colore = "Bianco";
    private bool _lampeggiante = false;
    private int _durataSecondi = 10;

    // Ogni metodo restituisce 'this' (lo stesso Builder) per permettere il Method Chaining (.ConTesto().ConColore()...)
    public DisplayMessageBuilder ConTesto(string testo)
    {
        _testo = testo;
        return this;
    }

    public DisplayMessageBuilder ConColore(string colore)
    {
        _colore = colore;
        return this;
    }

    public DisplayMessageBuilder ConLampeggio(bool lampeggiante)
    {
        _lampeggiante = lampeggiante;
        return this;
    }

    public DisplayMessageBuilder ConDurataSecondi(int secondi)
    {
        _durataSecondi = secondi;
        return this;
    }

    // Il metodo finale Build() valida e restituisce l'oggetto immutabile pronto all'uso
    public DisplayMessage Build() => new(_testo, _colore, _lampeggiante, _durataSecondi);
}

// -------------------------------------------------------------------------
// 3. STRATEGY PATTERN (Comportamentale)
// Quando usarlo: quando hai più modi (algoritmi) per compiere la stessa operazione (es. calcolo sconto,
// esportazione PDF/Excel, assegnazione ticket) e vuoi poter cambiare algoritmo a runtime senza if/else.
// -------------------------------------------------------------------------
public interface IAssignmentStrategy
{
    string DeterminaDestinatario(BaseTicket ticket);
}

public class AssegnazioneRepartoLocaleStrategy : IAssignmentStrategy
{
    public string DeterminaDestinatario(BaseTicket ticket) =>
        $"Team Presidio Diurno Sede (per ticket {ticket.Code})";
}

public class AssegnazioneReperibilitaNotturnaStrategy : IAssignmentStrategy
{
    public string DeterminaDestinatario(BaseTicket ticket) =>
        $"Tecnico Reperibile H24 via Telefono (per ticket urgente {ticket.Code})";
}

public class TicketDispatcher
{
    private readonly IAssignmentStrategy _strategy;

    public TicketDispatcher(IAssignmentStrategy strategy)
    {
        _strategy = strategy;
    }

    public void AssegnaTicket(BaseTicket ticket)
    {
        string destinatario = _strategy.DeterminaDestinatario(ticket);
        Console.WriteLine($" [Strategy] Ticket {ticket.Code} assegnato a -> {destinatario}");
    }
}

// -------------------------------------------------------------------------
// 4. DECORATOR PATTERN (Strutturale)
// Quando usarlo: quando vuoi aggiungere funzionalità trasversali (Logging, Caching, Retry, Telemetria)
// a una classe esistente rispettando l'Open/Closed Principle (senza modificare la classe originale).
// Come funziona: il Decorator implementa LA STESSA interfaccia (INotifier) e contiene al suo interno
// un'istanza dell'oggetto reale ("decorato"), eseguendo codice prima/dopo la chiamata.
// -------------------------------------------------------------------------
public class LoggingNotifierDecorator : INotifier
{
    private readonly INotifier _innerNotifier;

    public LoggingNotifierDecorator(INotifier innerNotifier)
    {
        _innerNotifier = innerNotifier;
    }

    public void Send(string message)
    {
        Console.WriteLine($" [Decorator LOG - INIZIO] Invio notifica alle {DateTimeOffset.Now:HH:mm:ss}...");
        _innerNotifier.Send(message); // Delega il lavoro vero al servizio interno!
        Console.WriteLine(" [Decorator LOG - FINE]   Notifica completata con successo.");
    }
}

// -------------------------------------------------------------------------
// 5. REPOSITORY PATTERN (Architetturale / Data Access)
// DOMANDA DA COLLOQUIO: "Perché usare il Repository Pattern se abbiamo già Entity Framework?"
// 1. Disaccoppiamento: i Controller o i Service applicativi parlano con ITicketRepository e non dipendono
//    direttamente da SQL Server o da dettagli specifici del database.
// 2. Testabilità (Unit Test): nei test puoi passare un Fake/Mock di ITicketRepository in memoria
//    senza dover toccare un database vero.
// 3. Centralizzazione query: eviti di duplicare le stesse query LINQ (.Where(...).Include(...)) in 10 Controller diversi.
// -------------------------------------------------------------------------
public interface ITicketRepository : IReadOnlyTicketRepository, IWriteTicketRepository
{
}

public class InMemoryTicketRepository : ITicketRepository
{
    private readonly Dictionary<string, BaseTicket> _storage = new(StringComparer.OrdinalIgnoreCase);

    public void Save(BaseTicket ticket)
    {
        _storage[ticket.Code] = ticket;
        Console.WriteLine($" [Repository] Salvato ticket {ticket.Code} nello storage.");
    }

    public BaseTicket? GetByCode(string code)
    {
        _storage.TryGetValue(code, out BaseTicket? ticket);
        return ticket;
    }
}
