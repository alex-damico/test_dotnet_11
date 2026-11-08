using System.Diagnostics;

namespace test_task;

class Program
{
    // Per poter usare la parola chiave 'await' dentro il Main,
    // la firma deve restituire 'async Task' invece di 'void'.
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 1. TASK SENZA VALORE DI RITORNO (Task) ===");
        // 'await' mette in pausa il flusso del metodo Main finché InviaNotificaAsync non termina,
        // senza bloccare il thread principale.
        await InviaNotificaAsync("INC-001", "Il ticket è stato preso in carico.");

        Console.WriteLine("\n=== 2. TASK CHE RESTITUISCE UN VALORE (Task<T>) ===");
        // Quando un metodo restituisce Task<string>, usando 'await' otteniamo direttamente la 'string'.
        string statoTicket = await RecuperaStatoTicketDaDbAsync("INC-001");
        Console.WriteLine($"Risultato ricevuto: {statoTicket}");

        Console.WriteLine("\n=== 3. DUE TASK IN PARALLELO (Task.WhenAll) ===");
        Stopwatch cronometro = Stopwatch.StartNew();

        // ATTENZIONE: NON mettiamo 'await' subito qui!
        // Avviamo entrambi i Task contemporaneamente e salviamo i riferimenti alle operazioni in corso.
        Task<string> chiamataUtenteTask = RecuperaUtenteDaApiAsync(userId: 42);
        Task<int> chiamataTicketApertiTask = ContaTicketApertiDaDbAsync();

        // Ora attendiamo che ENTRAMBI i task abbiano finito in parallelo.
        // Poiché il primo dura 1500ms e il secondo 1000ms, insieme impiegheranno ~1500ms (e non 2500ms!).
        await Task.WhenAll(chiamataUtenteTask, chiamataTicketApertiTask);

        cronometro.Stop();

        // A questo punto entrambi i Task sono completati, possiamo leggere i risultati
        string nomeUtente = await chiamataUtenteTask;
        int numeroTicket = await chiamataTicketApertiTask;

        Console.WriteLine($"Utente: {nomeUtente} | Ticket aperti: {numeroTicket}");
        Console.WriteLine($"Tempo totale per le 2 chiamate in parallelo: {cronometro.ElapsedMilliseconds} ms (invece di ~2500 ms)");
    }

    // -------------------------------------------------------------------------
    // ESEMPIO 1: Task che NON restituisce nulla (equivalente asincrono di 'void')
    // Nota: in C# si usa sempre 'async Task' e quasi mai 'async void' (usato solo per gli event handler).
    // -------------------------------------------------------------------------
    private static async Task InviaNotificaAsync(string codiceTicket, string messaggio)
    {
        Console.WriteLine($"[Inizio] Invio email per {codiceTicket}...");

        // Task.Delay simula un'operazione lenta di I/O (es. chiamata a un server SMTP) di 1 secondo
        await Task.Delay(1000);

        Console.WriteLine($"[Fine] Email inviata per {codiceTicket}: '{messaggio}'");
    }

    // -------------------------------------------------------------------------
    // ESEMPIO 2: Task che RESTITUISCE un valore (Task<string>)
    // Il tipo restituito dentro Task<T> corrisponde al tipo del 'return' finale.
    // -------------------------------------------------------------------------
    private static async Task<string> RecuperaStatoTicketDaDbAsync(string codiceTicket)
    {
        Console.WriteLine($"[Inizio] Ricerca stato sul database per {codiceTicket}...");

        // Simula una query al database di 800 millisecondi
        await Task.Delay(800);

        return $"Ticket {codiceTicket}: IN LAVORAZIONE (Priorità Alta)";
    }

    // -------------------------------------------------------------------------
    // ESEMPIO 3A: Prima chiamata per il test in parallelo (dura 1.5 secondi)
    // -------------------------------------------------------------------------
    private static async Task<string> RecuperaUtenteDaApiAsync(int userId)
    {
        Console.WriteLine(" -> [API Utenti] Chiamata avviata (1500 ms)...");
        await Task.Delay(1500);
        Console.WriteLine(" <- [API Utenti] Risposta ricevuta!");

        return $"Mario Rossi (ID: {userId})";
    }

    // -------------------------------------------------------------------------
    // ESEMPIO 3B: Seconda chiamata per il test in parallelo (dura 1 secondo)
    // -------------------------------------------------------------------------
    private static async Task<int> ContaTicketApertiDaDbAsync()
    {
        Console.WriteLine(" -> [DB Ticket] Conteggio avviato (1000 ms)...");
        await Task.Delay(1000);
        Console.WriteLine(" <- [DB Ticket] Conteggio completato!");

        return 7;
    }
}
