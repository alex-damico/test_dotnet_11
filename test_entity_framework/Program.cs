using Microsoft.EntityFrameworkCore;

namespace test_entity_framework;

// =========================================================================
// TEORIA BASE SU ENTITY FRAMEWORK CORE (ORM di .NET)
// =========================================================================
// Entity Framework Core (EF Core) è un ORM (Object-Relational Mapper):
// permette di lavorare con le tabelle del database (es. SQL Server) usando
// normali classi C# (Entità) e query LINQ invece di scrivere stringhe SQL a mano.
//
// CONCETTI CHIAVE DA COLLOQUIO:
// 1. DbContext: rappresenta la sessione con il database (Unit of Work) e tiene traccia
//    delle modifiche alle entità (Change Tracker). Va registrato in DI come SCOPED!
// 2. DbSet<T>: rappresenta una tabella del database su cui fare query LINQ.
// 3. IQueryable<T> vs IEnumerable<T>:
//    - IQueryable<T> (usato da DbSet<T>): costruisce un Expression Tree che EF Core
//      traduce in una vera query SQL (SELECT ... WHERE ... ORDER BY) eseguita sul DB!
//      La query parte davvero solo quando chiami un metodo terminale (es. ToListAsync(), FirstOrDefaultAsync(), CountAsync()).
//    - IEnumerable<T>: filtra i dati IN MEMORIA RAM dopo averli già scaricati.
// 4. Change Tracking vs AsNoTracking():
//    - Di default EF Core "osserva" ogni entità letta per capire se modifichi qualche proprietà.
//    - Se devi solo LEGGERE i dati (es. una chiamata GET in un'API), usa .AsNoTracking()
//      per risparmiare memoria e velocizzare la query!
// =========================================================================

class Program
{
    static async Task Main(string[] args)
    {
        // Configuriamo il DbContext con un database InMemory per poter provare subito le query
        // (Su SQL Server reale useresti: .UseSqlServer("Server=...;Database=...;"))
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TicketsDb")
            .Options;

        await using var db = new AppDbContext(options);

        // =====================================================================
        // ESEMPIO 1: INSERIMENTO DATI (Add / AddRange + SaveChangesAsync)
        // Quando aggiungi oggetti al DbSet, lo stato dell'entità nel Change Tracker
        // diventa 'Added'. I dati vengono scritti sul DB solo con SaveChangesAsync().
        // =====================================================================
        Console.WriteLine("=== 1. POPOLAMENTO DATABASE (AddRange + SaveChangesAsync) ===");

        var catInfrastruttura = new Category { Name = "Infrastruttura" };
        var catSviluppo = new Category { Name = "Sviluppo" };

        var ticketsIniziali = new List<TicketEntity>
        {
            new() { Code = "INC-001", Title = "Server SQL lento", Priority = 1, IsClosed = false, Category = catInfrastruttura },
            new() { Code = "INC-002", Title = "Container Docker non parte", Priority = 1, IsClosed = false, Category = catInfrastruttura },
            new() { Code = "REQ-001", Title = "Nuovo endpoint API REST", Priority = 2, IsClosed = true, Category = catSviluppo },
            new() { Code = "REQ-002", Title = "Aggiornamento pacchetti NuGet", Priority = 3, IsClosed = false, Category = catSviluppo }
        };

        await db.Tickets.AddRangeAsync(ticketsIniziali);
        int righeSalvate = await db.SaveChangesAsync();
        Console.WriteLine($"Record salvati nel database: {righeSalvate}\n");

        // =====================================================================
        // ESEMPIO 2: QUERY DI SOLA LETTURA CON AsNoTracking() + FILTRI E ORDINAMENTO
        // Poiché db.Tickets è un IQueryable<TicketEntity>, EF Core combina Where e OrderBy
        // in un'unica query prima di eseguirla al momento del ToListAsync().
        // Usiamo .AsNoTracking() perché non dobbiamo modificare questi oggetti.
        // =====================================================================
        Console.WriteLine("=== 2. QUERY CON AsNoTracking(), Where() E OrderBy() ===");

        List<TicketEntity> ticketAperti = await db.Tickets
            .AsNoTracking()
            .Where(t => !t.IsClosed && t.Priority <= 2)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Code)
            .ToListAsync();

        foreach (var t in ticketAperti)
        {
            Console.WriteLine($" -> [{t.Code}] {t.Title} (Priorità: {t.Priority})");
        }

        // =====================================================================
        // ESEMPIO 3: RELAZIONI TRA TABELLE ED EAGER LOADING CON .Include()
        // DOMANDA DA COLLOQUIO - "Cos'è il problema N+1 e come si risolve?"
        // Di default EF Core NON carica le tabelle collegate (es. Category è null se usi un nuovo DbContext).
        // Se carichi N ticket e poi in un foreach fai una query separata per ogni categoria,
        // esegui 1 + N query al DB!
        // SOLUZIONE: usare .Include(t => t.Category) (che in SQL diventa una INNER/LEFT JOIN).
        // =====================================================================
        Console.WriteLine("\n=== 3. EAGER LOADING CON .Include() (Equivalente di JOIN in SQL) ===");

        var categorieConTicket = await db.Categories
            .AsNoTracking()
            .Include(c => c.Tickets) // Carica in un colpo solo anche la lista dei Ticket figli!
            .ToListAsync();

        foreach (var categoria in categorieConTicket)
        {
            Console.WriteLine($"Categoria: {categoria.Name} (Totale ticket: {categoria.Tickets.Count})");
            foreach (var t in categoria.Tickets)
            {
                Console.WriteLine($"   * {t.Code} - {t.Title}");
            }
        }

        // =====================================================================
        // ESEMPIO 4: PROIEZIONE VERSO UN DTO CON .Select() (Best Practice per Web API!)
        // Invece di tirare su tutte le colonne della tabella con .Include(),
        // usiamo .Select() per proiettare direttamente i dati in un record DTO leggero.
        // In SQL questo genera: SELECT t.Code, t.Title, c.Name FROM Tickets t JOIN Categories c ...
        // Nota: quando usi .Select() verso un DTO, EF Core non fa tracking automaticamente!
        // =====================================================================
        Console.WriteLine("\n=== 4. PROIEZIONE IN UN DTO CON .Select() ===");

        List<TicketDto> ticketDtos = await db.Tickets
            .Where(t => !t.IsClosed)
            .Select(t => new TicketDto(
                t.Code,
                t.Title,
                t.Category != null ? t.Category.Name : "Nessuna"))
            .ToListAsync();

        foreach (var dto in ticketDtos)
        {
            Console.WriteLine($" -> DTO: Codice={dto.Code} | Titolo={dto.Title} | Categoria={dto.CategoryName}");
        }

        // =====================================================================
        // ESEMPIO 5: AGGIORNAMENTO (UPDATE) ED ELIMINAZIONE (DELETE) TRAMITE CHANGE TRACKER
        // ATTENZIONE: qui NON usiamo AsNoTracking(), perché vogliamo che il Change Tracker
        // di EF Core si accorga delle modifiche alle proprietà e generi l'UPDATE SQL!
        // =====================================================================
        Console.WriteLine("\n=== 5. UPDATE E DELETE CON IL CHANGE TRACKER ===");

        // 5A. UPDATE: recuperiamo un singolo ticket tracciato e ne modifichiamo le proprietà
        TicketEntity? ticketDaChiudere = await db.Tickets
            .FirstOrDefaultAsync(t => t.Code == "INC-001");

        if (ticketDaChiudere is not null)
        {
            ticketDaChiudere.IsClosed = true; // Il Change Tracker segna automaticamente lo stato come 'Modified'
            Console.WriteLine($"Stato nel Change Tracker per INC-001 prima del Save: {db.Entry(ticketDaChiudere).State}");

            await db.SaveChangesAsync(); // Esegue l'UPDATE sul database
            Console.WriteLine($"INC-001 aggiornato! IsClosed = {ticketDaChiudere.IsClosed}");
        }

        // 5B. DELETE: rimuoviamo un ticket dal DbSet
        TicketEntity? ticketDaEliminare = await db.Tickets
            .FirstOrDefaultAsync(t => t.Code == "REQ-002");

        if (ticketDaEliminare is not null)
        {
            db.Tickets.Remove(ticketDaEliminare); // Stato nel Change Tracker -> 'Deleted'
            await db.SaveChangesAsync();          // Esegue il DELETE sul database
            Console.WriteLine("Ticket REQ-002 eliminato dal database.");
        }

        // Conteggio finale direttamente lato DB con CountAsync() (in SQL: SELECT COUNT(*) FROM Tickets)
        int totaleRimasti = await db.Tickets.CountAsync();
        Console.WriteLine($"Totale ticket rimasti a database: {totaleRimasti}");
    }
}

// =========================================================================
// DEFINIZIONE DEL DBCONTEXT, DELLE ENTITÀ E DEL DTO
// =========================================================================

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Ogni DbSet<T> rappresenta una tabella nel database
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();
    public DbSet<Category> Categories => Set<Category>();
}

// Entità Categoria (lato "1" della relazione 1-a-Molti: una Categoria ha molti Ticket)
public class Category
{
    public int Id { get; set; } // Per convenzione 'Id' diventa automaticamente Primary Key (PK)
    public string Name { get; set; } = string.Empty;

    // Navigation Property: collezione dei ticket associati a questa categoria
    public List<TicketEntity> Tickets { get; set; } = [];
}

// Entità Ticket (lato "Molti" della relazione: ogni Ticket appartiene a una Categoria)
public class TicketEntity
{
    public int Id { get; set; } // Primary Key (PK)
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsClosed { get; set; }

    // Foreign Key (FK) + Navigation Property verso Category
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}

// DTO (Data Transfer Object) definito come 'record' immutabile:
// Usato nelle API per restituire al client solo i campi desiderati senza esporre l'entità del DB.
public record TicketDto(string Code, string Title, string CategoryName);
