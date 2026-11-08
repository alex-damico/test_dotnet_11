namespace test_linq;

class Program
{
    static void Main(string[] args)
    {
        Ticket firstTicket = new Ticket(
            "INC-001",
            "Portale non disponibile",
            "Gli utenti ricevono un errore durante l'accesso.",
            priority: 1);

        Ticket secondTicket = new Ticket(
            "REQ-001",
            "Attivazione ambiente di test",
            "Serve un nuovo ambiente per il team di sviluppo.",
            priority: 2);

        Ticket thirdTicket = new Ticket(
            "INC-002",
            "Esportazione lenta",
            "La generazione del report richiede troppo tempo.",
            priority: 3);

        Ticket fourthTicket = new Ticket(
            "REQ-002",
            "Creazione nuovo utente",
            "Creare l'utenza per una nuova collega.",
            priority: 1);

        List<Ticket> tickets = new List<Ticket>()
        {
            firstTicket,
            secondTicket,
            thirdTicket,
            fourthTicket
        };
        TicketsShow(tickets);

        secondTicket.Close();
        fourthTicket.Close();

        // Approccio imperativo classico (senza LINQ): ciclo foreach + if manuale
        List<Ticket> ticketsClosed = [];
        foreach (var ticket in tickets)
        {
            if (ticket.IsClosed)
            {
                ticketsClosed.Add(ticket);
            }
        }

        TicketsShow(ticketsClosed);

        // Approccio dichiarativo con LINQ:
        // - Where: filtra gli elementi che soddisfano il predicato (esecuzione differita / deferred execution)
        // - ToList: materializza subito la query in una List<Ticket> (esecuzione immediata)
        List<Ticket> ticketsClosedLQ = tickets.Where(t => t.IsClosed).ToList();
        TicketsShow(ticketsClosedLQ);

        // First: restituisce il primo elemento che soddisfa la condizione.
        // ATTENZIONE: lancia InvalidOperationException se nessun elemento soddisfa la condizione!
        Ticket firstClosed = tickets.First(t => t.IsClosed);
        TicketShow(firstClosed);

        // FirstOrDefault: come First, ma se la sequenza è vuota o nessun elemento soddisfa il filtro
        // restituisce il valore di default del tipo (null per i reference type come Ticket) senza lanciare eccezioni.
        List<Ticket> ticketsEmpty = [];
        Console.WriteLine(ticketsEmpty.FirstOrDefault(t => t.IsClosed));

        // Pipeline LINQ concatenata (esecuzione differita fino al foreach):
        // 1. Where: filtra solo i ticket chiusi
        // 2. OrderBy: ordinamento primario crescente per Priority
        // 3. ThenBy: ordinamento secondario crescente per Code (a parità di Priority)
        // 4. Select: proiezione/trasformazione di ogni Ticket in una stringa formattata
        var enumerable = tickets.Where(t => t.IsClosed)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Code)
            .Select(t => $"{t.Code} - {t.Priority}");
        foreach (var ticket in enumerable)
        {
            Console.WriteLine(ticket);
        }

        // 1 Livello
        Console.WriteLine("----1 Livello----------");
        // 1.1 - Filtro combinato: ticket ancora aperti (!IsClosed) il cui codice inizia con "INC-"
        var list = tickets.Where(t => !t.IsClosed && t.Code.StartsWith("INC-"))
            .ToList();
        TicketsShow(list);

        // 1.2 - Count con predicato: conta quanti ticket hanno priorità 1 (esecuzione immediata)
        var count = tickets.Count(t => t.Priority == 1);
        Console.WriteLine(count);
        
        // 1.3 - Quantificatori booleani (Any e All):
        // Any: restituisce true se ALMENO UN elemento soddisfa la condizione (si ferma al primo trovato)
        bool existsOpenHighPriority = tickets.Any(t => !t.IsClosed && t.Priority == 1);
        Console.WriteLine(existsOpenHighPriority);

        // All: restituisce true solo se TUTTI gli elementi della sequenza filtrata soddisfano la condizione
        bool allReqClosed = tickets                                                                                                                                                                                                                                                                                                                                    
            .Where(t => t.Code.StartsWith("REQ-"))                                                                                                                                                                                                                                                                                                                     
            .All(t => t.IsClosed); 
        Console.WriteLine(allReqClosed);
        
        // 2 Livello
        Console.WriteLine("----2 Livello----------");
        
        // 2.1 - SingleOrDefault: cerca l'UNICO elemento che soddisfa la condizione (o null se non esiste).
        // ATTENZIONE: se trova più di un elemento corrispondente, lancia InvalidOperationException!
        // Ideale per ricerche tramite chiave univoca (es. Code).
        var singleOrDefault = tickets.SingleOrDefault(t => t.Code.Equals("INC-002"));
        Console.WriteLine(singleOrDefault);
        
        // 2.2 - Ordinamento decrescente (OrderByDescending) + secondario (ThenBy) + Take(2):
        // Take(n) prende solo i primi n elementi della sequenza ordinata (es. "Top 2").
        var take = tickets.OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Title.Length)
            .Take(2).ToList();
        TicketsShow(take);
        
        // 2.3 - Paginazione con Skip e Take:
        // Skip(2) salta i primi 2 elementi, Take(2) prende i 2 successivi (equivale alla "pagina 2" con pageSize = 2).
        var tickets1 = tickets.OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Title.Length)
            .Skip(2) .Take(2)
            .ToList();
        TicketsShow(tickets1);
        
        // 3 Livello
        Console.WriteLine("----3 Livello----------");
        
        // 3.1 - Proiezione (Select) verso un Tipo Anonimo (new { ... }):
        // Utile per estrarre o calcolare solo le proprietà che servono senza dover creare una classe DTO dedicata.
        var select = tickets.Select(t => new
            { Codice = t.Code, EtichettaPriorita = t.PriorityLabel, Stato = t.IsClosed ? "Chiuso" : "Aperto" });
        foreach (var x1 in select)
        {
            Console.WriteLine($"{x1.Codice}: {x1.EtichettaPriorita} {x1.Stato}");
        }
        // 3.2 - Raggruppamento con GroupBy:
        // Raggruppa i ticket in base alla chiave specificata (PriorityLabel).
        // Ogni elemento iterato (grouping) è un IGrouping<string, Ticket> che espone:
        // - grouping.Key: il valore della chiave (es. "Alta", "Media", "Bassa")
        // - gli elementi del gruppo su cui usare metodi aggregati come grouping.Count()
        var groupBy = tickets.GroupBy(t => t.PriorityLabel);
        foreach (var grouping in groupBy)
        {
            Console.WriteLine($"{grouping.Key}: {grouping.Count()}");
        }

        // 4 Livello
        Console.WriteLine("----4 Livello----------");

        // 4.1 - Rimozione duplicati per proprietà con DistinctBy:
        // Restituisce un solo elemento per ciascun valore distinto della chiave scelta (Priority).
        // Mantiene il PRIMO elemento incontrato per ogni valore di Priority (es. tra INC-001 e REQ-002,
        // entrambi con Priority 1, mantiene solo INC-001).
        var distinctByPriority = tickets.DistinctBy(t => t.Priority).ToList();
        TicketsShow(distinctByPriority);

        // 4.2 - Conversione in dizionario con ToDictionary:
        // Trasforma la sequenza in un Dictionary<TKey, TValue> (esecuzione immediata).
        // Permette ricerche istantanee O(1) tramite chiave invece di scorrere tutta la lista con Where/FirstOrDefault.
        // ATTENZIONE: la chiave deve essere univoca per ogni elemento, altrimenti lancia ArgumentException!
        // Qui costruiamo una chiave composta concatenando Code + Priority (es. "INC-001-1"):
        Dictionary<string, Ticket> ticketsByKey = tickets.ToDictionary(t => $"{t.Code}-{t.Priority}");
        foreach (KeyValuePair<string, Ticket> item in ticketsByKey)
        {
            Console.WriteLine($"Chiave: {item.Key} -> Oggetto: {item.Value.GetSummary()}");
            Console.WriteLine("--------------");
        }
    }

    private static void TicketsShow(List<Ticket> tickets)
    {
        Console.WriteLine("--------------");
        foreach (var ticket in tickets)
        {
            Console.WriteLine(ticket.GetSummary());
        }

        Console.WriteLine("--------------");
    }

    private static void TicketShow(Ticket ticket)
    {
        Console.WriteLine("--------------");
        Console.WriteLine(ticket.ToString());
        Console.WriteLine("--------------");
    }
}