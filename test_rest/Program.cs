using Scalar.AspNetCore;

// =========================================================================
// STRUTTURA DI UN'APPLICAZIONE WEB API IN ASP.NET CORE (Program.cs)
// =========================================================================
// Il file Program.cs è diviso in 2 fasi principali:
// 1. REGISTRAZIONE DEI SERVIZI (builder.Services): qui configuri il Container DI
//    (Controller, DbContext, Autenticazione, OpenAPI/Scalar, Servizi custom).
// 2. CONFIGURAZIONE DELLA MIDDLEWARE PIPELINE (app.Use... / app.Map...):
//    definisce l'ordine esatto con cui ogni richiesta HTTP viene elaborata
//    (es. UseHttpsRedirection -> UseAuthentication -> UseAuthorization -> MapControllers).
//    ATTENZIONE (Domanda da colloquio!): L'ordine dei Middleware è fondamentale!
//    UseAuthentication() DEVE sempre venire PRIMA di UseAuthorization().
// =========================================================================

var builder = WebApplication.CreateBuilder(args);

// 1. Aggiungiamo il supporto ai Controller API ([ApiController]) e alla generazione del documento OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 2. Espone il documento JSON OpenAPI (/openapi/v1.json) e l'interfaccia interattiva Scalar (/scalar/v1)
// Nota: da .NET 9+ Microsoft ha sostituito Swashbuckle (Swagger) di default con Microsoft.AspNetCore.OpenApi,
// e Scalar è l'interfaccia grafica moderna usata per esplorare e testare le API dal browser.
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Test REST API - Gestione Ticket");
});

// Redirect comodo: aprendo http://localhost:5080 nel browser ti porta direttamente su Scalar!
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

// 3. Configuriamo la Pipeline dei Middleware HTTP
app.UseAuthorization();

// Mappa automaticamente le rotte definite nei nostri Controller (es. TicketsController -> /api/tickets)
app.MapControllers();

Console.WriteLine("=================================================================");
Console.WriteLine(" Web API CRUD avviata con SCALAR UI!");
Console.WriteLine(" -> Interfaccia Scalar: http://localhost:5080/scalar/v1");
Console.WriteLine(" -> Documento OpenAPI:  http://localhost:5080/openapi/v1.json");
Console.WriteLine("=================================================================");

app.Run("http://localhost:5080");
