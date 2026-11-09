# I Principi SOLID in C# (.NET)

I principi **SOLID** rappresentano cinque linee guida fondamentali della programmazione orientata agli oggetti (OOP) e del design architetturale, introdotte da Robert C. Martin (*Uncle Bob*). In **C# e .NET**, costituiscono la base su cui poggiano il sistema di Dependency Injection nativo, il pattern middleware di ASP.NET Core e la scrittura di codice testabile e manutenibile.

---

## 1. S — Single Responsibility Principle (SRP)

> **Principio:** *Una classe dovrebbe avere una, e una sola, ragione per cambiare.*

Spesso viene frainteso come "una classe deve fare una sola cosa", ma la definizione corretta riguarda l'**attore o il contesto di business** da cui dipende. Se una classe gestisce contemporaneamente la logica di dominio, il salvataggio su database (es. tramite EF Core) e l'invio di notifiche, un cambiamento nel provider email rischia di rompere la logica di creazione dell'ordine.

### ❌ Violazione (Accoppiamento di responsabilità)

    public class OrderProcessor
    {
        public void ProcessOrder(Order order)
        {
            // 1. Validazione di business
            if (order.Items.Count == 0) 
                throw new InvalidOperationException("Ordine vuoto.");

            // 2. Persistenza dati (Infrastruttura DB)
            using var db = new AppDbContext();
            db.Orders.Add(order);
            db.SaveChanges();

            // 3. Notifica (Infrastruttura Email)
            using var smtp = new SmtpClient("smtp.server.com");
            smtp.Send("noreply@shop.com", order.CustomerEmail, "Conferma", "Ordine ricevuto.");
        }
    }

### ✅ Refactoring SRP

Ogni classe ha una responsabilità chiara e isolata; `OrderService` funge solo da orchestratore:

    public class OrderValidator
    {
        public void Validate(Order order)
        {
            if (order.Items.Count == 0) 
                throw new InvalidOperationException("Ordine vuoto.");
        }
    }

    public class OrderService(
        OrderValidator validator, 
        IOrderRepository repository, 
        INotificationService notifier)
    {
        public async Task ProcessOrderAsync(Order order, CancellationToken ct)
        {
            validator.Validate(order);
            await repository.SaveAsync(order, ct);
            await notifier.SendConfirmationAsync(order.CustomerEmail, ct);
        }
    }

---

## 2. O — Open/Closed Principle (OCP)

> **Principio:** *Le entità software dovrebbero essere aperte alle estensioni, ma chiuse alle modifiche.*

Quando arriva un nuovo requisito (es. un nuovo metodo di pagamento o una nuova regola di sconto), dovresti poter aggiungere il nuovo comportamento scrivendo **nuovo codice** (nuove classi), senza dover modificare e ritestare le classi esistenti piene di `if/else` o `switch`.

### ❌ Violazione

Ogni volta che aggiungi un tipo di cliente, devi modificare questa classe:

    public class DiscountCalculator
    {
        public decimal Calculate(Order order, string customerType) => customerType switch
        {
            "Standard" => order.Total * 0.05m,
            "VIP" => order.Total * 0.20m,
            // Se aggiungo "Employee", devo toccare e ricompilare questa classe
            _ => 0m
        };
    }

### ✅ Refactoring OCP (Strategy Pattern)

In C#, l'OCP si realizza naturalmente con le interfacce e l'iniezione di collezioni (`IEnumerable<T>`) nel container DI:

    public interface IDiscountStrategy
    {
        string CustomerType { get; }
        decimal CalculateDiscount(Order order);
    }

    public class StandardDiscount : IDiscountStrategy
    {
        public string CustomerType => "Standard";
        public decimal CalculateDiscount(Order order) => order.Total * 0.05m;
    }

    public class VipDiscount : IDiscountStrategy
    {
        public string CustomerType => "VIP";
        public decimal CalculateDiscount(Order order) => order.Total * 0.20m;
    }

    // Questa classe è CHIUSA alle modifiche: per aggiungere nuovi sconti 
    // basta creare una nuova classe che implementa IDiscountStrategy e registrarla in DI.
    public class DiscountService(IEnumerable<IDiscountStrategy> strategies)
    {
        public decimal ApplyDiscount(Order order, string customerType)
        {
            var strategy = strategies.FirstOrDefault(s => s.CustomerType == customerType);
            return strategy?.CalculateDiscount(order) ?? 0m;
        }
    }

---

## 3. L — Liskov Substitution Principle (LSP)

> **Principio:** *Gli oggetti di una superclasse devono poter essere sostituiti con oggetti delle sue sottoclassi senza alterare la correttezza del programma.*

Se una classe figlia eredita da una classe base o implementa un'interfaccia, ma lancia una `NotImplementedException` o una `NotSupportedException` in uno dei suoi metodi, sta violando il contratto promesso dal tipo base. Chi consuma l'astrazione non deve mai essere costretto a fare type-checking (`if (file is ReadOnlyFile)`) per evitare crash a runtime.

### ❌ Violazione

    public abstract class FileStorage
    {
        public abstract byte[] Read(string path);
        public abstract void Save(string path, byte[] data);
    }

    public class ReadOnlyArchiveStorage : FileStorage
    {
        public override byte[] Read(string path) => [];

        // VIOLAZIONE LSP: il chiamante si aspetta che qualsiasi FileStorage possa salvare
        public override void Save(string path, byte[] data) => 
            throw new NotSupportedException("L'archivio è in sola lettura.");
    }

### ✅ Refactoring LSP

Separa le capacità usando contratti distinti, in modo che la gerarchia rispetti sempre le precondizioni e postcondizioni attese:

    public interface IReadableStorage
    {
        byte[] Read(string path);
    }

    public interface IWritableStorage : IReadableStorage
    {
        void Save(string path, byte[] data);
    }

    public class ReadOnlyArchiveStorage : IReadableStorage
    {
        public byte[] Read(string path) => [];
    }

    public class LocalDiskStorage : IWritableStorage
    {
        public byte[] Read(string path) => [];
        public void Save(string path, byte[] data) { /* salva su disco */ }
    }

---

## 4. I — Interface Segregation Principle (ISP)

> **Principio:** *I client non dovrebbero essere forzati a dipendere da metodi che non usano.*

Invece di creare "fat interfaces" (interfacce monolitiche con decine di metodi), conviene creare interfacce piccole e focalizzate sul ruolo (*role interfaces*). Un esempio classico nel mondo .NET è la separazione tra lettura e scrittura (alla base anche del pattern CQRS).

### ❌ Violazione

    public interface IRepository<T>
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
        void GenerateMonthlyReport(); // Metodo specifico che non c'entra con il CRUD base
    }

### ✅ Refactoring ISP

    public interface IReadRepository<T>
    {
        T? GetById(int id);
        IEnumerable<T> GetAll();
    }

    public interface IWriteRepository<T>
    {
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }

    // Una classe concreta può comunque implementarle entrambe
    public class OrderRepository : IReadRepository<Order>, IWriteRepository<Order>
    {
        // Implementazione completa...
    }

---

## 5. D — Dependency Inversion Principle (DIP)

> **Principio:**
> 1. *I moduli di alto livello non devono dipendere da quelli di basso livello. Entrambi devono dipendere da astrazioni.*
> 2. *Le astrazioni non devono dipendere dai dettagli. I dettagli devono dipendere dalle astrazioni.*

Differenza chiave:
* **DIP** è il *principio di design*: il dominio di business (alto livello) definisce le interfacce di cui ha bisogno; il layer infrastrutturale (basso livello, es. PostgreSQL, Redis, client HTTP) implementa quelle interfacce.
* **IoC (Inversion of Control)** è il *pattern* con cui il controllo del flusso o della creazione degli oggetti viene delegato a un componente esterno.
* **DI (Dependency Injection)** è la *tecnica pratica* (in C# tramite `IServiceCollection` e costruttori) con cui passiamo l'istanza concreta a chi richiede l'astrazione.

### ❌ Violazione

Il modulo di alto livello istanzia direttamente il dettaglio di basso livello con la keyword `new`:

    public class TelemetryService
    {
        // Dipendenza rigida da un dettaglio infrastrutturale specifico
        private readonly SqlDatabaseLogger _logger = new SqlDatabaseLogger();

        public void ProcessMetric(double value)
        {
            if (value > 100)
                _logger.LogWarning($"Valore fuori soglia: {value}");
        }
    }

### ✅ Refactoring DIP

    // Astrazione posseduta dal modulo di alto livello
    public interface ITelemetryLogger
    {
        void LogWarning(string message);
    }

    // Modulo di alto livello: dipende SOLO dall'astrazione
    public class TelemetryService(ITelemetryLogger logger)
    {
        public void ProcessMetric(double value)
        {
            if (value > 100)
                logger.LogWarning($"Valore fuori soglia: {value}");
        }
    }

    // Dettaglio di basso livello: dipende dall'astrazione
    public class SqlDatabaseLogger : ITelemetryLogger
    {
        public void LogWarning(string message) { /* scrive su DB */ }
    }

    // Registrazione nel Composition Root (Program.cs):
    // builder.Services.AddScoped<ITelemetryLogger, SqlDatabaseLogger>();
    // builder.Services.AddScoped<TelemetryService>();

---

## Compromessi pratici nel mondo reale (.NET)

Applicare SOLID in modo dogmatico porta facilmente all'**over-engineering**. Regole pratiche per bilanciare i principi in C#:

* **Evita le interfacce "Header" 1:1 senza motivo:** Creare `IUserService` per `UserService` ha senso se devi mockare il servizio nei test, se hai più implementazioni (OCP) o se stai separando i contratti tra assembly diversi (Clean Architecture). Per classi helper interne e pure (senza I/O), un'interfaccia spesso aggiunge solo rumore.
* **`DbContext` di Entity Framework Core:** Creare un `IRepository` generico sopra `DbContext` spesso duplica ciò che il framework offre già nativamente (Unit of Work + Repository). Valuta se iniettare un'astrazione sul caso d'uso specifico o usare direttamente `DbContext` incapsulato dietro handler verticali (*Vertical Slice Architecture*).
* **Pragmatismo su OCP:** Non astrarre preventivamente tutto al primo requisito. Scrivi codice semplice; alla prima o seconda modifica reale dello stesso punto (*Rule of Three*), applica il refactoring verso Strategy o Decorator.
