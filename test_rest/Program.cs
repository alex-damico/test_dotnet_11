// =========================================================================
// STRUTTURA DI UN'APPLICAZIONE WEB API IN ASP.NET CORE (Program.cs)
// =========================================================================
// Il file Program.cs è diviso in 2 fasi principali:
// 1. REGISTRAZIONE DEI SERVIZI (builder.Services): qui configuri il Container DI
//    (Controller, DbContext, Autenticazione, Swagger/OpenAPI, Servizi custom).
// 2. CONFIGURAZIONE DELLA MIDDLEWARE PIPELINE (app.Use... / app.Map...):
//    definisce l'ordine esatto con cui ogni richiesta HTTP viene elaborata
//    (es. UseHttpsRedirection -> UseAuthentication -> UseAuthorization -> MapControllers).
//    ATTENZIONE (Domanda da colloquio!): L'ordine dei Middleware è fondamentale!
//    UseAuthentication() DEVE sempre venire PRIMA di UseAuthorization().
// =========================================================================

var builder = WebApplication.CreateBuilder(args);

// 1. Aggiungiamo il supporto ai Controller API ([ApiController]) nel Container DI
builder.Services.AddControllers();

var app = builder.Build();

// 2. Configuriamo la Pipeline dei Middleware HTTP
app.UseAuthorization();

// Mappa automaticamente le rotte definite nei nostri Controller (es. TicketsController -> /api/tickets)
app.MapControllers();

Console.WriteLine("=================================================================");
Console.WriteLine(" Web API CRUD avviata!");
Console.WriteLine(" Endpoint disponibili:");
Console.WriteLine("  - GET    /api/tickets                  (Lista tutti i ticket, supporta ?isClosed=false&priority=1)");
Console.WriteLine("  - GET    /api/tickets/{id}             (Dettaglio singolo ticket)");
Console.WriteLine("  - POST   /api/tickets                  (Crea un nuovo ticket -> 201 Created)");
Console.WriteLine("  - PUT    /api/tickets/{id}             (Aggiorna intero ticket -> 200 OK)");
Console.WriteLine("  - PATCH  /api/tickets/{id}/close       (Chiude il ticket -> 200 OK)");
Console.WriteLine("  - DELETE /api/tickets/{id}             (Elimina il ticket -> 204 No Content)");
Console.WriteLine(" Apri il file 'test_rest.http' in Rider per testare tutte le chiamate con un click!");
Console.WriteLine("=================================================================");

app.Run("http://localhost:5080");
