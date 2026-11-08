using Microsoft.Extensions.DependencyInjection;

namespace test_DI;

// =========================================================================
// TEORIA BASE SULLA DEPENDENCY INJECTION (DI) E INVERSION OF CONTROL (IoC)
// =========================================================================
// Senza DI: una classe crea da sola le sue dipendenze con 'new Servizio()' -> forte accoppiamento!
// Con DI:   la classe dichiara nel costruttore SOLO l'interfaccia di cui ha bisogno (es. IServizio)
//           ed è il Container IoC di .NET (IServiceProvider) a creare e passare l'istanza corretta,
//           gestendone anche la distruzione (Dispose) a fine ciclo di vita.
//
// I 3 CICLI DI VITA (LIFETIME) IN .NET:
// 1. TRANSIENT (AddTransient): Nuova istanza OGNI VOLTA che viene richiesta.
// 2. SCOPED    (AddScoped):    Un'unica istanza per ogni SCOPE (in Web API = 1 istanza per Richiesta HTTP).
// 3. SINGLETON (AddSingleton): Un'unica istanza per TUTTA la vita dell'applicazione.
// =========================================================================

class Program
{
    static void Main(string[] args)
    {
        // 1. Creiamo la collezione dei servizi (in ASP.NET Core equivale a 'builder.Services')
        ServiceCollection services = new ServiceCollection();

        // 2. Registriamo i servizi associando Interfaccia -> Implementazione concreta con il relativo ciclo di vita
        services.AddTransient<ITransientService, OperationService>();
        services.AddScoped<IScopedService, OperationService>();
        services.AddSingleton<ISingletonService, OperationService>();

        // Registriamo anche un servizio applicativo che riceve gli altri servizi nel costruttore
        services.AddTransient<TicketProcessor>();

        // 3. Costruiamo il ServiceProvider (il Container IoC vero e proprio)
        // Nota: validateScopes: true attiva il controllo anti-"Captive Dependency" (come fa ASP.NET Core in Development)
        using ServiceProvider rootProvider = services.BuildServiceProvider(validateScopes: true);

        // =====================================================================
        // ESEMPIO 1: TRANSIENT (AddTransient)
        // Ogni volta che chiediamo ITransientService al container, viene creato
        // un oggetto NUOVO con un Guid diverso, anche dentro lo stesso Scope.
        // Quando usarlo: servizi leggeri, senza stato (stateless), es. validatori o calcolatori.
        // =====================================================================
        Console.WriteLine("=== ESEMPIO 1: TRANSIENT (Sempre nuovo ad ogni richiesta) ===");
        using (IServiceScope scope = rootProvider.CreateScope())
        {
            var t1 = scope.ServiceProvider.GetRequiredService<ITransientService>();
            var t2 = scope.ServiceProvider.GetRequiredService<ITransientService>();

            Console.WriteLine($"1° chiamata Transient -> ID: {t1.OperationId}");
            Console.WriteLine($"2° chiamata Transient -> ID: {t2.OperationId}");
            Console.WriteLine($"Sono la stessa istanza? {ReferenceEquals(t1, t2)}\n"); // False!
        }

        // =====================================================================
        // ESEMPIO 2: SCOPED (AddScoped)
        // Dentro lo STESSO Scope (es. la stessa chiamata HTTP) restituisce SEMPRE la stessa istanza.
        // Se apriamo un NUOVO Scope (es. arriva una seconda chiamata HTTP), crea una nuova istanza.
        // Quando usarlo: per il DbContext di Entity Framework Core o servizi legati alla singola richiesta HTTP.
        // =====================================================================
        Console.WriteLine("=== ESEMPIO 2: SCOPED (Uguale nello stesso Scope, diverso tra Scope diversi) ===");
        using (IServiceScope httpRequest1 = rootProvider.CreateScope())
        {
            var s1 = httpRequest1.ServiceProvider.GetRequiredService<IScopedService>();
            var s2 = httpRequest1.ServiceProvider.GetRequiredService<IScopedService>();

            Console.WriteLine($"[Richiesta HTTP 1 - Prima chiamata]   ID: {s1.OperationId}");
            Console.WriteLine($"[Richiesta HTTP 1 - Seconda chiamata] ID: {s2.OperationId}");
            Console.WriteLine($"Stessa istanza dentro Richiesta HTTP 1? {ReferenceEquals(s1, s2)}"); // True!
        }

        using (IServiceScope httpRequest2 = rootProvider.CreateScope())
        {
            var s3 = httpRequest2.ServiceProvider.GetRequiredService<IScopedService>();
            Console.WriteLine($"[Richiesta HTTP 2 - Nuova richiesta]  ID: {s3.OperationId} (Diverso dalla Richiesta 1!)\n");
        }

        // =====================================================================
        // ESEMPIO 3: SINGLETON (AddSingleton)
        // Viene istanziato UNA SOLA VOLTA alla prima richiesta e rimane identico
        // per tutta l'esecuzione del programma, attraverso qualunque Scope.
        // Quando usarlo: cache in memoria, configurazioni globali, client condivisi thread-safe.
        // ATTENZIONE: un Singleton deve essere Thread-Safe perché più richieste lo usano in parallelo!
        // =====================================================================
        Console.WriteLine("=== ESEMPIO 3: SINGLETON (Unica istanza per tutta la vita dell'app) ===");
        using (IServiceScope scopeA = rootProvider.CreateScope())
        using (IServiceScope scopeB = rootProvider.CreateScope())
        {
            var single1 = scopeA.ServiceProvider.GetRequiredService<ISingletonService>();
            var single2 = scopeB.ServiceProvider.GetRequiredService<ISingletonService>();

            Console.WriteLine($"[Scope A] Singleton ID: {single1.OperationId}");
            Console.WriteLine($"[Scope B] Singleton ID: {single2.OperationId}");
            Console.WriteLine($"Sono la stessa istanza tra Scope diversi? {ReferenceEquals(single1, single2)}\n"); // True!
        }

        // =====================================================================
        // ESEMPIO 4: CONSTRUCTOR INJECTION & CONFRONTO COMPLETO ( + CAPTIVE DEPENDENCY )
        // Simuliamo 2 richieste HTTP reali dove una classe 'TicketProcessor' riceve
        // i 3 servizi direttamente nel suo costruttore (Constructor Injection).
        //
        // DOMANDA DA COLLOQUIO - "Cos'è la Captive Dependency?"
        // È l'errore di iniettare un servizio SCOPED (es. DbContext) dentro un servizio SINGLETON.
        // Poiché il Singleton non muore mai, terrebbe "prigioniero" (captive) lo Scoped per sempre,
        // trasformandolo di fatto in un Singleton e causando bug gravissimi di concorrenza!
        // Con 'validateScopes: true', .NET lancia subito una InvalidOperationException se provi a farlo.
        // =====================================================================
        Console.WriteLine("=== ESEMPIO 4: CONSTRUCTOR INJECTION IN 2 RICHIESTE HTTP SIMULATE ===");

        Console.WriteLine("--- Simulazione Richiesta HTTP #1 ---");
        using (IServiceScope requestScope1 = rootProvider.CreateScope())
        {
            // Nella stessa richiesta HTTP risolviamo due volte TicketProcessor:
            // - Transient cambia anche dentro la stessa richiesta
            // - Scoped resta uguale dentro la Richiesta #1
            // - Singleton resta uguale sempre
            var processor1A = requestScope1.ServiceProvider.GetRequiredService<TicketProcessor>();
            processor1A.StampaIdServizi(" Chiamata 1A");

            var processor1B = requestScope1.ServiceProvider.GetRequiredService<TicketProcessor>();
            processor1B.StampaIdServizi(" Chiamata 1B");
        }

        Console.WriteLine("\n--- Simulazione Richiesta HTTP #2 ---");
        using (IServiceScope requestScope2 = rootProvider.CreateScope())
        {
            // Nuova richiesta HTTP:
            // - Transient cambia ancora
            // - Scoped ora ha un NUOVO ID (nuova richiesta HTTP)
            // - Singleton mantiene SEMPRE lo stesso ID dall'inizio del programma!
            var processor2 = requestScope2.ServiceProvider.GetRequiredService<TicketProcessor>();
            processor2.StampaIdServizi(" Chiamata 2A");
        }
    }
}

// =========================================================================
// INTERFACCE E CLASSI DI SUPPORTO PER GLI ESEMPI
// =========================================================================

public interface IOperation
{
    Guid OperationId { get; }
}

// Creiamo 3 interfacce distinte per registrare la stessa classe con i 3 cicli di vita diversi
public interface ITransientService : IOperation { }
public interface IScopedService : IOperation { }
public interface ISingletonService : IOperation { }

// Implementazione concreta: ogni volta che viene fatto 'new OperationService()' dal container DI,
// genera un Guid univoco così possiamo vedere subito quando l'istanza cambia o rimane la stessa.
public class OperationService : ITransientService, IScopedService, ISingletonService
{
    public Guid OperationId { get; } = Guid.NewGuid();
}

// Servizio applicativo che dimostra la Constructor Injection:
// Non usa mai 'new OperationService()', ma riceve le interfacce dal Container DI.
public class TicketProcessor
{
    private readonly ITransientService _transient;
    private readonly IScopedService _scoped;
    private readonly ISingletonService _singleton;

    public TicketProcessor(
        ITransientService transient,
        IScopedService scoped,
        ISingletonService singleton)
    {
        _transient = transient;
        _scoped = scoped;
        _singleton = singleton;
    }

    public void StampaIdServizi(string etichetta)
    {
        Console.WriteLine($"{etichetta} -> Transient: {_transient.OperationId.ToString()[..8]} | Scoped: {_scoped.OperationId.ToString()[..8]} | Singleton: {_singleton.OperationId.ToString()[..8]}");
    }
}
